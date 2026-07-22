// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using System.Globalization;
using XiHan.Framework.Core.Exceptions;

namespace XiHan.BasicApp.Saas.Application.Services;

/// <summary>
/// 验证码防刷限流服务实现（分布式缓存标记/计数）
/// </summary>
/// <remarks>
/// 计数采用「读-改-写」而非原子自增（与 <c>AuthAppService</c> 既有邮箱登录码频控同型机制）：
/// 并发竞态仅导致轻微少计，不影响防刷有效性；接入 Redis 后天然多实例共享。
/// IP 取自 <see cref="IHttpContextAccessor"/>（照登录频控取法，取不到记 unknown）。
/// </remarks>
public sealed class VerificationThrottleService
    : IVerificationThrottleService
{
    private readonly IDistributedCache _distributedCache;
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>
    /// 构造函数
    /// </summary>
    public VerificationThrottleService(
        IDistributedCache distributedCache,
        IHttpContextAccessor httpContextAccessor)
    {
        _distributedCache = distributedCache;
        _httpContextAccessor = httpContextAccessor;
    }

    /// <inheritdoc />
    public async Task EnsureSendAllowedAsync(long userId, ProfileVerificationPurpose purpose, string target, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(target);
        cancellationToken.ThrowIfCancellationRequested();

        // 封禁期内发送一并拒绝
        await EnsureVerifyAllowedAsync(userId, purpose, cancellationToken);

        var targetKey = NormalizeTarget(target);
        var intervalKey = BuildIntervalKey(purpose, targetKey);
        if (!string.IsNullOrEmpty(await _distributedCache.GetStringAsync(intervalKey, cancellationToken)))
        {
            throw new UserFriendlyException("验证码发送过于频繁，请稍后再试。");
        }

        var today = DateTimeOffset.UtcNow.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        var targetQuotaKey = $"{VerificationThrottleConsts.KeyPrefix}:daily:target:{targetKey}:{today}";
        var targetCount = await GetCounterAsync(targetQuotaKey, cancellationToken);
        if (targetCount >= VerificationThrottleConsts.DailyTargetQuota)
        {
            throw new UserFriendlyException("该联系方式今日验证码发送次数已达上限，请明日再试。");
        }

        var ip = ResolveClientIp();
        var ipQuotaKey = $"{VerificationThrottleConsts.KeyPrefix}:daily:ip:{ip}:{today}";
        var ipCount = await GetCounterAsync(ipQuotaKey, cancellationToken);
        if (ipCount >= VerificationThrottleConsts.DailyIpQuota)
        {
            throw new UserFriendlyException("当前网络今日验证码发送次数已达上限，请明日再试。");
        }

        // 全部检查通过后记账：占用间隔位 + 双日配额计数（TTL 到次日零点，UTC）
        var dayTtl = UtcEndOfDayTtl();
        await _distributedCache.SetStringAsync(
            intervalKey,
            "1",
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(VerificationThrottleConsts.SendIntervalSeconds) },
            cancellationToken);
        await SetCounterAsync(targetQuotaKey, targetCount + 1, dayTtl, cancellationToken);
        await SetCounterAsync(ipQuotaKey, ipCount + 1, dayTtl, cancellationToken);
    }

    /// <inheritdoc />
    public async Task EnsureVerifyAllowedAsync(long userId, ProfileVerificationPurpose purpose, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var banKey = BuildBanKey(userId, purpose);
        if (!string.IsNullOrEmpty(await _distributedCache.GetStringAsync(banKey, cancellationToken)))
        {
            throw new UserFriendlyException("验证码错误次数过多，已临时锁定，请稍后再试。");
        }
    }

    /// <inheritdoc />
    public async Task<bool> OnVerifyFailedAsync(long userId, ProfileVerificationPurpose purpose, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var failKey = BuildFailKey(userId, purpose);
        var failCount = await GetCounterAsync(failKey, cancellationToken) + 1;
        if (failCount < VerificationThrottleConsts.MaxVerifyFailures)
        {
            // 失败计数滚动窗口：每次失败重置 TTL（封禁时长同窗）
            await SetCounterAsync(failKey, failCount, TimeSpan.FromMinutes(VerificationThrottleConsts.BanMinutes), cancellationToken);
            return false;
        }

        // 达到阈值：落封禁标记并清计数（封禁期发送/校验均拒绝）
        await _distributedCache.SetStringAsync(
            BuildBanKey(userId, purpose),
            "1",
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(VerificationThrottleConsts.BanMinutes) },
            cancellationToken);
        await _distributedCache.RemoveAsync(failKey, cancellationToken);
        return true;
    }

    /// <inheritdoc />
    public async Task OnVerifySucceededAsync(long userId, ProfileVerificationPurpose purpose, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _distributedCache.RemoveAsync(BuildFailKey(userId, purpose), cancellationToken);
    }

    private static string BuildIntervalKey(ProfileVerificationPurpose purpose, string targetKey)
    {
        return $"{VerificationThrottleConsts.KeyPrefix}:interval:{purpose}:{targetKey}";
    }

    private static string BuildFailKey(long userId, ProfileVerificationPurpose purpose)
    {
        return $"{VerificationThrottleConsts.KeyPrefix}:fail:{userId}:{purpose}";
    }

    private static string BuildBanKey(long userId, ProfileVerificationPurpose purpose)
    {
        return $"{VerificationThrottleConsts.KeyPrefix}:ban:{userId}:{purpose}";
    }

    private static string NormalizeTarget(string target)
    {
        return target.Trim().ToLowerInvariant();
    }

    /// <summary>
    /// 距 UTC 次日零点的剩余时长（日配额计数器 TTL）
    /// </summary>
    private static TimeSpan UtcEndOfDayTtl()
    {
        var now = DateTimeOffset.UtcNow;
        var ttl = now.UtcDateTime.Date.AddDays(1) - now.UtcDateTime;
        return ttl > TimeSpan.Zero ? ttl : TimeSpan.FromMinutes(1);
    }

    private string ResolveClientIp()
    {
        return _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    private async Task<int> GetCounterAsync(string key, CancellationToken cancellationToken)
    {
        var raw = await _distributedCache.GetStringAsync(key, cancellationToken);
        return int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var count) ? count : 0;
    }

    private async Task SetCounterAsync(string key, int count, TimeSpan ttl, CancellationToken cancellationToken)
    {
        await _distributedCache.SetStringAsync(
            key,
            count.ToString(CultureInfo.InvariantCulture),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = ttl },
            cancellationToken);
    }
}
