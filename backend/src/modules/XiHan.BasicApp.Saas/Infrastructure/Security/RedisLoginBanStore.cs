// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Globalization;
using StackExchange.Redis;
using XiHan.BasicApp.Saas.Application.AccessPolicies;
using XiHan.BasicApp.Saas.Application.Caching;

namespace XiHan.BasicApp.Saas.Infrastructure.Security;

/// <summary>
/// Redis 上的登录封禁存储：失败计数用原子 INCR 并在首次计数时设过期，封禁名单用有序集合（分值是解封时刻的 Unix 秒）
/// </summary>
public sealed class RedisLoginBanStore : ILoginBanStore
{
    private const string IncrementScript =
        "local count = redis.call('INCR', KEYS[1]) " +
        "if count == 1 then redis.call('PEXPIRE', KEYS[1], ARGV[1]) end " +
        "return count";

    private static readonly TimeSpan HistoryLifetime = TimeSpan.FromHours(24);

    private readonly IConnectionMultiplexer _redis;

    /// <summary>
    /// 构造函数
    /// </summary>
    public RedisLoginBanStore(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    /// <inheritdoc />
    public async Task<long> IncrementFailureAsync(string ip, TimeSpan window, CancellationToken cancellationToken = default)
    {
        var result = await _redis.GetDatabase().ScriptEvaluateAsync(
            IncrementScript,
            [FailureKey(ip)],
            [(long)window.TotalMilliseconds]);
        return (long)result;
    }

    /// <inheritdoc />
    public async Task<TimeSpan?> GetLastBanDurationAsync(string ip, CancellationToken cancellationToken = default)
    {
        var value = await _redis.GetDatabase().StringGetAsync(HistoryKey(ip));
        return value.HasValue && long.TryParse(value.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var seconds)
            ? TimeSpan.FromSeconds(seconds)
            : null;
    }

    /// <inheritdoc />
    public async Task BanAsync(string ip, DateTimeOffset until, TimeSpan duration, CancellationToken cancellationToken = default)
    {
        var transaction = _redis.GetDatabase().CreateTransaction();
        _ = transaction.SortedSetAddAsync(SaasCacheNames.LoginBans, ip, until.ToUnixTimeSeconds());
        _ = transaction.StringSetAsync(HistoryKey(ip), ((long)duration.TotalSeconds).ToString(CultureInfo.InvariantCulture), HistoryLifetime);
        _ = transaction.KeyDeleteAsync(FailureKey(ip));
        _ = await transaction.ExecuteAsync();
    }

    /// <inheritdoc />
    public async Task<DateTimeOffset?> GetBanExpirationAsync(string ip, DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        var score = await _redis.GetDatabase().SortedSetScoreAsync(SaasCacheNames.LoginBans, ip);
        return score is { } seconds && seconds > now.ToUnixTimeSeconds()
            ? DateTimeOffset.FromUnixTimeSeconds((long)seconds)
            : null;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<LoginBanEntry>> ListAsync(DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        var database = _redis.GetDatabase();
        var nowSeconds = now.ToUnixTimeSeconds();
        _ = await database.SortedSetRemoveRangeByScoreAsync(SaasCacheNames.LoginBans, double.NegativeInfinity, nowSeconds);
        var entries = await database.SortedSetRangeByScoreWithScoresAsync(SaasCacheNames.LoginBans, nowSeconds, double.PositiveInfinity, Exclude.Start);
        return [.. entries.Select(entry => new LoginBanEntry(entry.Element.ToString(), DateTimeOffset.FromUnixTimeSeconds((long)entry.Score)))];
    }

    /// <inheritdoc />
    public async Task<bool> UnbanAsync(string ip, DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        var banned = await GetBanExpirationAsync(ip, now, cancellationToken) is not null;
        var transaction = _redis.GetDatabase().CreateTransaction();
        _ = transaction.SortedSetRemoveAsync(SaasCacheNames.LoginBans, ip);
        _ = transaction.KeyDeleteAsync(FailureKey(ip));
        _ = transaction.KeyDeleteAsync(HistoryKey(ip));
        _ = await transaction.ExecuteAsync();
        return banned;
    }

    private static RedisKey FailureKey(string ip)
    {
        return $"{SaasCacheNames.LoginFailureCount}:{ip}";
    }

    private static RedisKey HistoryKey(string ip)
    {
        return $"{SaasCacheNames.LoginBanHistory}:{ip}";
    }
}
