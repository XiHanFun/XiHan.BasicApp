// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using XiHan.BasicApp.Identity.Domain.Entities;
using XiHan.BasicApp.Identity.Domain.Services;
using XiHan.Framework.Caching.Abstractions.Caches;
using XiHan.Framework.Caching.Abstractions.Stores;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Identity.Infrastructure;

/// <summary>
/// 两步验证票据的状态。
/// </summary>
/// <remarks>
/// 哪个用户、登录哪个上下文、第二步能用哪些方式、已输错几次、何时过期。
/// </remarks>
/// <param name="UserId">第一步已通过的用户。</param>
/// <param name="TenantId">登录的上下文；平台为 <see langword="null"/>。</param>
/// <param name="Methods">第二步能用的方式（签发时可用的，验证码登录去掉了第一步用过的那种）。</param>
/// <param name="Failures">已输错的次数。</param>
/// <param name="ExpiresAt">过期时间（签发起算，不顺延）。</param>
public sealed record TwoFactorTicket(Guid UserId, string? TenantId, TwoFactorMethods Methods, int Failures, DateTimeOffset ExpiresAt)
{
    /// <summary>
    /// 这张票据能不能用这种方式。
    /// </summary>
    public bool Allows(TwoFactorMethod method) => Methods.HasFlag(TwoFactorSettings.FlagOf(method));
}

/// <summary>
/// 两步验证票据。
/// </summary>
/// <remarks>
/// 单例服务。密码通过后签发，第二步凭它免密码，登录完成即作废。
/// 票据是 32 字节随机数的 base64url，共享缓存里按它的 SHA-256 存状态；输错达到 <see cref="IdentityOptions.TwoFactorTicketMaxFailures"/> 次即作废，须重新输密码。
/// </remarks>
/// <param name="cache">共享缓存。</param>
/// <param name="options">身份配置。</param>
/// <param name="clock">时间源。</param>
public sealed class TwoFactorTickets(ISharedCache cache, IdentityOptions options, TimeProvider clock)
{
    /// <summary>
    /// 签发票据。
    /// </summary>
    public async Task<(string Ticket, DateTimeOffset ExpiresAt)> IssueAsync(Guid userId, string? tenantId, IReadOnlyList<TwoFactorMethod> methods, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(methods);
        var ticket = RefreshTokens.Generate();
        var state = new TwoFactorTicket(userId, tenantId, TwoFactorSettings.FlagsOf(methods), 0, clock.GetUtcNow() + options.TwoFactorTicketLifetime);
        await cache.SetAsync(KeyOf(ticket), Encode(state), CacheEntryOptions.Expires(options.TwoFactorTicketLifetime), cancellationToken).ConfigureAwait(false);
        return (ticket, state.ExpiresAt);
    }

    /// <summary>
    /// 取票据状态。
    /// </summary>
    /// <returns>票据状态；不存在、已过期或已作废为无值。</returns>
    public async Task<Maybe<TwoFactorTicket>> FindAsync(string? ticket, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(ticket))
        {
            return default;
        }

        var stored = await cache.GetAsync<string>(KeyOf(ticket), cancellationToken).ConfigureAwait(false);
        return stored.TryGetValue(out var raw) && Decode(raw) is { } state && state.ExpiresAt > clock.GetUtcNow() ? Maybe.Some(state) : default;
    }

    /// <summary>
    /// 记一次输错。
    /// </summary>
    /// <remarks>
    /// 用完次数即作废。
    /// </remarks>
    /// <returns>票据是否已作废。</returns>
    public async Task<bool> RecordFailureAsync(string ticket, TwoFactorTicket state, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ticket);
        ArgumentNullException.ThrowIfNull(state);
        var failures = state.Failures + 1;
        var remaining = state.ExpiresAt - clock.GetUtcNow();
        if (failures >= options.TwoFactorTicketMaxFailures || remaining <= TimeSpan.Zero)
        {
            await RevokeAsync(ticket, cancellationToken).ConfigureAwait(false);
            return true;
        }

        await cache.SetAsync(KeyOf(ticket), Encode(state with { Failures = failures }), CacheEntryOptions.Expires(remaining), cancellationToken).ConfigureAwait(false);
        return false;
    }

    /// <summary>
    /// 作废票据。
    /// </summary>
    public async Task RevokeAsync(string ticket, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ticket);
        await cache.RemoveAsync(KeyOf(ticket), cancellationToken).ConfigureAwait(false);
    }

    private static CacheKey KeyOf(string ticket) => new($"identity:two-factor:{Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(ticket)))}");

    // 存成一行文本，不依赖缓存的对象序列化：用户 | 租户 | 次数 | 过期（Unix 毫秒）| 方式（按位）
    private static string Encode(TwoFactorTicket state)
        => string.Create(CultureInfo.InvariantCulture, $"{state.UserId:N}|{state.TenantId}|{state.Failures}|{state.ExpiresAt.ToUnixTimeMilliseconds()}|{(int)state.Methods}");

    private static TwoFactorTicket? Decode(string raw)
    {
        var parts = raw.Split('|');
        return parts.Length == 5
            && Guid.TryParseExact(parts[0], "N", out var userId)
            && int.TryParse(parts[2], NumberStyles.None, CultureInfo.InvariantCulture, out var failures)
            && long.TryParse(parts[3], NumberStyles.None, CultureInfo.InvariantCulture, out var expires)
            && int.TryParse(parts[4], NumberStyles.None, CultureInfo.InvariantCulture, out var methods)
            ? new TwoFactorTicket(userId, parts[1].Length == 0 ? null : parts[1], (TwoFactorMethods)methods, failures, DateTimeOffset.FromUnixTimeMilliseconds(expires))
            : null;
    }
}
