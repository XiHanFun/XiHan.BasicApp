// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Security.Cryptography;
using Microsoft.Extensions.Caching.Distributed;

namespace XiHan.BasicApp.Saas.Application.Services;

/// <summary>
/// 两步验证票据服务实现
/// </summary>
/// <remarks>
/// 票据是 32 字节加密安全随机数的 Base64Url 形态，以分布式缓存键存储、值为绑定的用户主键；
/// 有效期与两步验证码同量级。与图形验证码（一次性码、读取即销毁）不同，票据在有效期内可重复出示，
/// 直到登录完成显式作废或过期。
/// </remarks>
public sealed class TwoFactorTicketService : ITwoFactorTicketService
{
    private const int TicketBytes = 32;

    private const int TicketLifetimeSeconds = 600;

    private readonly IDistributedCache _distributedCache;

    /// <summary>
    /// 构造函数
    /// </summary>
    public TwoFactorTicketService(IDistributedCache distributedCache)
    {
        _distributedCache = distributedCache;
    }

    /// <summary>
    /// 票据有效秒数
    /// </summary>
    public int ExpiresInSeconds => TicketLifetimeSeconds;

    /// <summary>
    /// 票据缓存键
    /// </summary>
    /// <param name="ticket">票据</param>
    public static string CacheKey(string ticket)
    {
        return $"auth:two-factor-ticket:{ticket}";
    }

    /// <summary>
    /// 签发票据
    /// </summary>
    public async Task<string> IssueAsync(long userId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var ticket = Convert.ToBase64String(RandomNumberGenerator.GetBytes(TicketBytes))
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
        await _distributedCache.SetStringAsync(
            CacheKey(ticket),
            userId.ToString(),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(TicketLifetimeSeconds) },
            cancellationToken);
        return ticket;
    }

    /// <summary>
    /// 解析票据绑定的用户主键
    /// </summary>
    public async Task<long?> ResolveUserIdAsync(string? ticket, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(ticket))
        {
            return null;
        }

        var value = await _distributedCache.GetStringAsync(CacheKey(ticket.Trim()), cancellationToken);
        return long.TryParse(value, out var userId) ? userId : null;
    }

    /// <summary>
    /// 作废票据
    /// </summary>
    public Task RevokeAsync(string ticket, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ticket);
        return _distributedCache.RemoveAsync(CacheKey(ticket.Trim()), cancellationToken);
    }
}
