// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.AccessPolicies;

/// <summary>
/// 内存里按分钟合并的命中记录器
/// </summary>
/// <remarks>
/// 同一（层、来源、规则、动作、时机、是否拦下、IP、方法、路径、用户）在同一分钟里只占一项、累加次数；
/// 不同的键最多 <see cref="AccessPolicyOptions.HitBufferCapacity"/> 个，满了以后新键的命中只计数，每分钟最多告警一次。
/// </remarks>
public sealed class AccessPolicyHitRecorder : IAccessPolicyHitRecorder
{
    private readonly IOptionsMonitor<AccessPolicyOptions> _options;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<AccessPolicyHitRecorder> _logger;
    private readonly Lock _lock = new();
    private readonly Dictionary<HitKey, Bucket> _buckets = [];
    private long _dropped;
    private DateTimeOffset _lastDropWarning = DateTimeOffset.MinValue;

    /// <summary>
    /// 构造函数
    /// </summary>
    public AccessPolicyHitRecorder(IOptionsMonitor<AccessPolicyOptions> options, TimeProvider timeProvider, ILogger<AccessPolicyHitRecorder> logger)
    {
        _options = options;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    /// <summary>
    /// 自启动以来因合并键已满而没记下的命中次数
    /// </summary>
    public long Dropped => Interlocked.Read(ref _dropped);

    /// <inheritdoc />
    public void Record(AccessPolicyHitEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        var key = HitKey.From(entry);
        var capacity = Math.Max(1, _options.CurrentValue.HitBufferCapacity);
        bool warn;
        lock (_lock)
        {
            if (_buckets.TryGetValue(key, out var bucket))
            {
                bucket.Count++;
                return;
            }

            if (_buckets.Count < capacity)
            {
                _buckets[key] = new Bucket(entry);
                return;
            }

            _dropped++;
            var now = _timeProvider.GetUtcNow();
            warn = now - _lastDropWarning >= TimeSpan.FromMinutes(1);
            if (warn)
            {
                _lastDropWarning = now;
            }
        }

        if (warn)
        {
            _logger.LogWarning("命中记录缓冲已满（{Capacity} 个合并键），新的命中只计数不落库；累计 {Dropped} 次", capacity, Dropped);
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<AccessPolicyHitBatchItem> Drain(DateTimeOffset now, bool includeCurrentMinute = false)
    {
        var currentMinute = TruncateToMinute(now);
        lock (_lock)
        {
            var closed = _buckets
                .Where(pair => includeCurrentMinute || pair.Key.Minute < currentMinute)
                .ToList();
            foreach (var pair in closed)
            {
                _ = _buckets.Remove(pair.Key);
            }

            return [.. closed.Select(pair => new AccessPolicyHitBatchItem(pair.Value.First, pair.Value.Count))];
        }
    }

    private static DateTimeOffset TruncateToMinute(DateTimeOffset time)
    {
        var utc = time.ToUniversalTime();
        return new DateTimeOffset(utc.Year, utc.Month, utc.Day, utc.Hour, utc.Minute, 0, TimeSpan.Zero);
    }

    private sealed class Bucket(AccessPolicyHitEntry first)
    {
        public AccessPolicyHitEntry First { get; } = first;

        public int Count { get; set; } = 1;
    }

    private readonly record struct HitKey(
        long TenantId,
        AccessPolicyHitSource Source,
        long? PolicyId,
        string PolicyCode,
        AccessPolicyAction Action,
        AccessPolicyPhase Phase,
        bool IsBlocked,
        string? Ip,
        string? Method,
        string? Path,
        long? UserId,
        DateTimeOffset Minute)
    {
        public static HitKey From(AccessPolicyHitEntry entry)
        {
            return new HitKey(
                entry.TenantId,
                entry.Source,
                entry.PolicyId,
                entry.PolicyCode,
                entry.Action,
                entry.Phase,
                entry.IsBlocked,
                entry.Request.Ip,
                entry.Request.Method,
                entry.Request.Path,
                entry.Request.UserId,
                TruncateToMinute(entry.Time));
        }
    }
}
