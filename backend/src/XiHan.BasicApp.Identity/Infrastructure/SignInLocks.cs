// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.Framework.Caching.Abstractions.Locking;
using XiHan.Framework.Caching.Abstractions.Stores;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Identity.Infrastructure;

/// <summary>
/// 同一账号的凭据核对排成一队
/// </summary>
/// <remarks>
/// 失败计数与锁定（<see cref="LoginAttempts"/>）、两步验证票据与验证码的错误次数都是先读后写：并发的猜测读到同一个次数，多次只记一次，锁定形同虚设
/// 核对密码或码之前先拿这把锁，拿不到即按节流拒绝、不核对；拿到的那一次记完失败才放开，下一次读到的就是记过的次数
/// 租约只兜底进程意外退出；正常路径在核对结束时释放
/// </remarks>
public static class SignInLocks
{
    /// <summary>
    /// 租约时长
    /// </summary>
    public static readonly TimeSpan Lease = TimeSpan.FromSeconds(30);

    /// <summary>
    /// 拿一个账号的核对锁
    /// </summary>
    /// <param name="locks">分布式锁</param>
    /// <param name="userId">账号</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>锁；同一账号正在核对时返回 <see cref="IdentityErrors.LoginThrottled"/></returns>
    public static Task<Result<LockHandle>> AcquireAsync(IDistributedLock locks, Guid userId, CancellationToken cancellationToken)
        => AcquireAsync(locks, new CacheKey($"identity:sign-in:{userId:N}"), cancellationToken);

    /// <summary>
    /// 拿一把核对锁
    /// </summary>
    /// <param name="locks">分布式锁</param>
    /// <param name="resource">锁的资源键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>锁；正在核对时返回 <see cref="IdentityErrors.LoginThrottled"/></returns>
    public static async Task<Result<LockHandle>> AcquireAsync(IDistributedLock locks, CacheKey resource, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(locks);
        var acquired = await locks.TryAcquireAsync(resource, Lease, cancellationToken).ConfigureAwait(false);
        return acquired.TryGetValue(out var handle)
            ? handle
            : Error.Unavailable(IdentityErrors.LoginThrottled, IdentityMessages.LoginThrottled, MessageArguments.Of(("retryAfter", 1)));
    }
}
