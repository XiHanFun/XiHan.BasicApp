// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Globalization;
using System.Text.Json;
using XiHan.Framework.Caching.Abstractions.Caches;
using XiHan.Framework.Caching.Abstractions.Stores;

namespace XiHan.BasicApp.Access.Infrastructure;

/// <summary>
/// 登录失败自动封禁的参数。
/// </summary>
/// <remarks>
/// 系统参数 <see cref="ConfigKey"/> 的 JSON 值，只认平台的；没建即缺省值，缺省关，4.x 同。
/// </remarks>
/// <param name="IsEnabled">是否启用。</param>
/// <param name="WindowMinutes">计数的固定窗口（分钟，1–1440）。</param>
/// <param name="MaxFailures">一个窗口里同一地址的失败上限（1–10000），达到即封禁。</param>
/// <param name="BanMinutes">首次封禁时长（分钟，1–43200）。</param>
/// <param name="MaxBanMinutes">封禁时长上限（分钟，不小于首次时长、不超过 43200）；24 小时内再犯时长翻倍到它为止。</param>
public sealed record LoginAutoBanSettings(bool IsEnabled = false, int WindowMinutes = 10, int MaxFailures = 20, int BanMinutes = 30, int MaxBanMinutes = 1440)
{
    /// <summary>
    /// 系统参数键。
    /// </summary>
    public const string ConfigKey = "access.security.login-auto-ban";

    /// <summary>
    /// 缺省值（关）。
    /// </summary>
    public static LoginAutoBanSettings Default { get; } = new();

    /// <summary>
    /// 各项是否在范围内。
    /// </summary>
    public bool IsValid => WindowMinutes is >= 1 and <= 1440 && MaxFailures is >= 1 and <= 10000 && BanMinutes is >= 1 and <= 43200 && MaxBanMinutes >= BanMinutes && MaxBanMinutes <= 43200;
}

/// <summary>
/// 一条封禁。
/// </summary>
/// <param name="Ip">地址。</param>
/// <param name="Until">解封时刻。</param>
public sealed record LoginBanEntry(string Ip, DateTimeOffset Until);

/// <summary>
/// 登录失败自动封禁的存储（单例，共享缓存）。
/// </summary>
/// <remarks>
/// 同一地址跨账号、按固定窗口数失败，达到上限即封；24 小时内再犯，时长在上次的基础上翻倍到上限（4.x 同）。
/// 封禁带到期时间，到点自动解封；另存一份封禁名单供查看（先读后写，不是原子的，与登录节流一样只当减速，不当安全边界）。
/// </remarks>
/// <param name="cache">共享缓存。</param>
/// <param name="clock">时间源。</param>
public sealed class LoginBans(ISharedCache cache, TimeProvider clock)
{
    private static readonly TimeSpan _historyLifetime = TimeSpan.FromHours(24);
    private static readonly CacheKey _indexKey = new("access:ban:index");

    /// <summary>
    /// 取地址此刻的封禁到期时刻。
    /// </summary>
    /// <returns>封禁到期时刻；没封为空。</returns>
    public async Task<DateTimeOffset?> BannedUntilAsync(string ip, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ip);
        var until = await ReadSecondsAsync(BanKey(ip), cancellationToken).ConfigureAwait(false);
        return until is { } seconds && seconds > clock.GetUtcNow().ToUnixTimeSeconds() ? DateTimeOffset.FromUnixTimeSeconds(seconds) : null;
    }

    /// <summary>
    /// 记一次登录失败。
    /// </summary>
    /// <remarks>
    /// 达到上限即封禁。
    /// </remarks>
    /// <returns>达到上限时返回解封时刻。</returns>
    public async Task<DateTimeOffset?> RecordFailureAsync(string ip, LoginAutoBanSettings settings, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ip);
        ArgumentNullException.ThrowIfNull(settings);
        var now = clock.GetUtcNow();
        var windowSeconds = settings.WindowMinutes * 60L;
        var window = now.ToUnixTimeSeconds() / windowSeconds;
        var counter = CounterKey(settings.WindowMinutes, window, ip);
        var count = (await ReadSecondsAsync(counter, cancellationToken).ConfigureAwait(false) ?? 0) + 1;
        if (count < settings.MaxFailures)
        {
            var remaining = ((window + 1) * windowSeconds) - now.ToUnixTimeSeconds();
            await cache.SetAsync(counter, count.ToString(CultureInfo.InvariantCulture), CacheEntryOptions.Expires(TimeSpan.FromSeconds(remaining + 1)), cancellationToken).ConfigureAwait(false);
            return null;
        }

        await cache.RemoveAsync(counter, cancellationToken).ConfigureAwait(false);
        var previous = await ReadSecondsAsync(HistoryKey(ip), cancellationToken).ConfigureAwait(false);
        var minutes = previous is { } last ? (int)Math.Min(last * 2L, settings.MaxBanMinutes) : settings.BanMinutes;
        var until = now.AddMinutes(minutes);
        await cache.SetAsync(BanKey(ip), until.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture), CacheEntryOptions.Expires(TimeSpan.FromMinutes(minutes)), cancellationToken).ConfigureAwait(false);
        await cache.SetAsync(HistoryKey(ip), minutes.ToString(CultureInfo.InvariantCulture), CacheEntryOptions.Expires(_historyLifetime), cancellationToken).ConfigureAwait(false);
        var index = (await ReadIndexAsync(cancellationToken).ConfigureAwait(false)).Where(e => e.Until > now && !string.Equals(e.Ip, ip, StringComparison.Ordinal)).Append(new LoginBanEntry(ip, until)).ToList();
        await WriteIndexAsync(index, cancellationToken).ConfigureAwait(false);
        return until;
    }

    /// <summary>
    /// 此刻的封禁，按解封时刻排列。
    /// </summary>
    public async Task<IReadOnlyList<LoginBanEntry>> ListAsync(CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var current = new List<LoginBanEntry>();
        foreach (var entry in await ReadIndexAsync(cancellationToken).ConfigureAwait(false))
        {
            if (entry.Until > now && await BannedUntilAsync(entry.Ip, cancellationToken).ConfigureAwait(false) is { } until)
            {
                current.Add(entry with { Until = until });
            }
        }

        return [.. current.OrderBy(static e => e.Until)];
    }

    /// <summary>
    /// 解封一个地址，连同它这一窗口的失败计数。
    /// </summary>
    /// <remarks>
    /// 4.x 同；封禁历史保留，24 小时内再犯照样翻倍。
    /// </remarks>
    /// <returns>没封着返回 <see langword="false"/>。</returns>
    public async Task<bool> LiftAsync(string ip, LoginAutoBanSettings settings, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ip);
        ArgumentNullException.ThrowIfNull(settings);
        var banned = await BannedUntilAsync(ip, cancellationToken).ConfigureAwait(false) is not null;
        await cache.RemoveAsync(BanKey(ip), cancellationToken).ConfigureAwait(false);
        var now = clock.GetUtcNow();
        await cache.RemoveAsync(CounterKey(settings.WindowMinutes, now.ToUnixTimeSeconds() / (settings.WindowMinutes * 60L), ip), cancellationToken).ConfigureAwait(false);
        await WriteIndexAsync([.. (await ReadIndexAsync(cancellationToken).ConfigureAwait(false)).Where(e => e.Until > now && !string.Equals(e.Ip, ip, StringComparison.Ordinal))], cancellationToken).ConfigureAwait(false);
        return banned;
    }

    private static CacheKey BanKey(string ip) => new($"access:ban:until:{ip}");

    private static CacheKey HistoryKey(string ip) => new($"access:ban:history:{ip}");

    private static CacheKey CounterKey(int windowMinutes, long window, string ip) => new(string.Create(CultureInfo.InvariantCulture, $"access:ban:fail:{windowMinutes}:{window}:{ip}"));

    private async Task<long?> ReadSecondsAsync(CacheKey key, CancellationToken cancellationToken)
        => (await cache.GetAsync<string>(key, cancellationToken).ConfigureAwait(false)).TryGetValue(out var raw) && long.TryParse(raw, NumberStyles.None, CultureInfo.InvariantCulture, out var value) ? value : null;

    private async Task<List<LoginBanEntry>> ReadIndexAsync(CancellationToken cancellationToken)
        => (await cache.GetAsync<string>(_indexKey, cancellationToken).ConfigureAwait(false)).TryGetValue(out var raw) && raw.Length > 0
            ? JsonSerializer.Deserialize(raw, AccessJsonContext.Default.ListLoginBanEntry) ?? []
            : [];

    // 名单活到最晚的一条到期为止
    private async Task WriteIndexAsync(List<LoginBanEntry> index, CancellationToken cancellationToken)
    {
        if (index.Count == 0)
        {
            await cache.RemoveAsync(_indexKey, cancellationToken).ConfigureAwait(false);
            return;
        }

        var lifetime = index.Max(static e => e.Until) - clock.GetUtcNow();
        await cache.SetAsync(_indexKey, JsonSerializer.Serialize(index, AccessJsonContext.Default.ListLoginBanEntry), CacheEntryOptions.Expires(lifetime > TimeSpan.Zero ? lifetime : TimeSpan.FromSeconds(1)), cancellationToken).ConfigureAwait(false);
    }
}
