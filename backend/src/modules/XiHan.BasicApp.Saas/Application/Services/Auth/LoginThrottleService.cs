// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Caching.Distributed;
using XiHan.Framework.Core.Exceptions;

namespace XiHan.BasicApp.Saas.Application.Services;

/// <summary>
/// 登录节流服务实现：账号 + IP 与纯 IP 两个维度的固定窗口计数
/// </summary>
/// <remarks>
/// 账号维度防单账号撞库（与框架账号锁定互补：锁定按账号计数，本服务防分布式多源尝试）；
/// IP 维度防同源多账号扫号。窗口与阈值对正常使用足够宽松（每分钟 5 次账号级尝试）。
/// </remarks>
public sealed class LoginThrottleService : ILoginThrottleService
{
    private const int AccountWindowSeconds = 60;

    private const int AccountMaxAttempts = 5;

    private const int IpWindowSeconds = 60;

    private const int IpMaxAttempts = 30;

    private readonly IDistributedCache _distributedCache;

    /// <summary>
    /// 构造函数
    /// </summary>
    public LoginThrottleService(IDistributedCache distributedCache)
    {
        _distributedCache = distributedCache;
    }

    /// <summary>
    /// 校验密码登录尝试是否在限流窗口内
    /// </summary>
    public async Task EnsureLoginAllowedAsync(string account, string? ipAddress, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(account);
        cancellationToken.ThrowIfCancellationRequested();

        var normalizedIp = string.IsNullOrWhiteSpace(ipAddress) ? "unknown" : ipAddress;
        var accountKey = $"auth:throttle:login:{account.ToLowerInvariant()}:{normalizedIp}";
        var ipKey = $"auth:throttle:login-ip:{normalizedIp}";

        await EnsureWindowAllowedAsync(accountKey, AccountMaxAttempts, AccountWindowSeconds, cancellationToken);
        await EnsureWindowAllowedAsync(ipKey, IpMaxAttempts, IpWindowSeconds, cancellationToken);
    }

    /// <summary>
    /// 单窗口计数：超限即拒绝，否则计数 +1 并重置窗口
    /// </summary>
    private async Task EnsureWindowAllowedAsync(string key, int maxAttempts, int windowSeconds, CancellationToken cancellationToken)
    {
        var hit = await _distributedCache.GetStringAsync(key, cancellationToken);
        _ = int.TryParse(hit, out var count);
        if (count >= maxAttempts)
        {
            throw new UserFriendlyException("登录尝试过于频繁，请稍后再试。");
        }

        await _distributedCache.SetStringAsync(
            key,
            (count + 1).ToString(),
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(windowSeconds)
            },
            cancellationToken);
    }
}
