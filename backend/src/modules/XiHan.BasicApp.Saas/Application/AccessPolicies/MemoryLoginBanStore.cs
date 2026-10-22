// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Saas.Application.AccessPolicies;

/// <summary>
/// 进程内的登录封禁存储，只适用于单实例部署
/// </summary>
/// <remarks>
/// 计数、历史与封禁各自最多 <see cref="Capacity"/> 项；满了先清过期项，仍满则淘汰最早到期的一项。
/// </remarks>
public sealed class MemoryLoginBanStore : ILoginBanStore
{
    /// <summary>
    /// 每类记录的容量上限
    /// </summary>
    public const int Capacity = 100_000;

    private static readonly TimeSpan HistoryLifetime = TimeSpan.FromHours(24);

    private readonly TimeProvider _timeProvider;
    private readonly Lock _lock = new();
    private readonly Dictionary<string, (long Count, DateTimeOffset ExpiresAt)> _failures = new(StringComparer.Ordinal);
    private readonly Dictionary<string, (TimeSpan Duration, DateTimeOffset ExpiresAt)> _history = new(StringComparer.Ordinal);
    private readonly Dictionary<string, DateTimeOffset> _bans = new(StringComparer.Ordinal);

    /// <summary>
    /// 构造函数
    /// </summary>
    public MemoryLoginBanStore(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
    }

    /// <inheritdoc />
    public Task<long> IncrementFailureAsync(string ip, TimeSpan window, CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow();
        lock (_lock)
        {
            var (count, expiresAt) = _failures.TryGetValue(ip, out var current) && current.ExpiresAt > now
                ? (current.Count + 1, current.ExpiresAt)
                : (1L, now + window);
            EnsureRoom(_failures, now, static item => item.ExpiresAt);
            _failures[ip] = (count, expiresAt);
            return Task.FromResult(count);
        }
    }

    /// <inheritdoc />
    public Task<TimeSpan?> GetLastBanDurationAsync(string ip, CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow();
        lock (_lock)
        {
            return Task.FromResult(_history.TryGetValue(ip, out var item) && item.ExpiresAt > now ? item.Duration : (TimeSpan?)null);
        }
    }

    /// <inheritdoc />
    public Task BanAsync(string ip, DateTimeOffset until, TimeSpan duration, CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow();
        lock (_lock)
        {
            EnsureRoom(_bans, now, static item => item);
            _bans[ip] = until;
            EnsureRoom(_history, now, static item => item.ExpiresAt);
            _history[ip] = (duration, now + HistoryLifetime);
            _ = _failures.Remove(ip);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<DateTimeOffset?> GetBanExpirationAsync(string ip, DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            return Task.FromResult(_bans.TryGetValue(ip, out var until) && until > now ? until : (DateTimeOffset?)null);
        }
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<LoginBanEntry>> ListAsync(DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            IReadOnlyList<LoginBanEntry> bans = [.. _bans
                .Where(pair => pair.Value > now)
                .OrderBy(pair => pair.Value)
                .Select(pair => new LoginBanEntry(pair.Key, pair.Value))];
            return Task.FromResult(bans);
        }
    }

    /// <inheritdoc />
    public Task<bool> UnbanAsync(string ip, DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var banned = _bans.Remove(ip, out var until) && until > now;
            _ = _failures.Remove(ip);
            _ = _history.Remove(ip);
            return Task.FromResult(banned);
        }
    }

    private static void EnsureRoom<T>(Dictionary<string, T> items, DateTimeOffset now, Func<T, DateTimeOffset> expiresAt)
    {
        if (items.Count < Capacity)
        {
            return;
        }

        foreach (var key in items.Where(pair => expiresAt(pair.Value) <= now).Select(pair => pair.Key).ToList())
        {
            _ = items.Remove(key);
        }

        if (items.Count >= Capacity)
        {
            _ = items.Remove(items.MinBy(pair => expiresAt(pair.Value)).Key);
        }
    }
}
