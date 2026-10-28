// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Runtime.Security;
using XiHan.Framework.Caching.Abstractions.Caches;
using XiHan.Framework.Caching.Abstractions.Stores;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Identity.Infrastructure;

/// <summary>
/// 认证器绑定中的密钥（单例）：<c>profile.setup-totp</c> 生成后先放共享缓存（加密，十分钟），<c>profile.enable-two-factor</c> 用认证器算出的码验证通过才落库。
/// 同一用户再生成即覆盖上一个；没确认的密钥过期即丢，账号不受影响。
/// </summary>
/// <param name="cache">共享缓存。</param>
/// <param name="protector">密钥加密。</param>
public sealed class TotpEnrollments(ISharedCache cache, ISecretProtector protector)
{
    /// <summary>
    /// 绑定窗口。
    /// </summary>
    public static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(10);

    /// <summary>
    /// 暂存用户的新密钥。
    /// </summary>
    public async Task StashAsync(Guid userId, string secret, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(secret);
        await cache.SetAsync(KeyOf(userId), protector.Protect(Operations.Auth.VerifyTwoFactor.SecretPurpose, secret), CacheEntryOptions.Expires(Lifetime), cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// 取用户暂存的密钥；没有或已过期为无值。
    /// </summary>
    public async Task<Maybe<string>> FindAsync(Guid userId, CancellationToken cancellationToken)
    {
        var stored = await cache.GetAsync<string>(KeyOf(userId), cancellationToken).ConfigureAwait(false);
        return stored.TryGetValue(out var protectedSecret) && protector.TryUnprotect(Operations.Auth.VerifyTwoFactor.SecretPurpose, protectedSecret, out var secret)
            ? Maybe.Some(secret)
            : default;
    }

    /// <summary>
    /// 丢弃暂存的密钥。
    /// </summary>
    public async Task DiscardAsync(Guid userId, CancellationToken cancellationToken)
        => await cache.RemoveAsync(KeyOf(userId), cancellationToken).ConfigureAwait(false);

    private static CacheKey KeyOf(Guid userId) => new($"identity:totp-enrollment:{userId:N}");
}
