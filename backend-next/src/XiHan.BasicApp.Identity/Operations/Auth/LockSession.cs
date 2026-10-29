// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.Contracts.Security;
using XiHan.BasicApp.Identity.Domain.Entities;
using XiHan.BasicApp.Identity.Domain.Services;
using XiHan.BasicApp.Identity.Infrastructure;
using XiHan.BasicApp.Runtime.Persistence;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Kernel.Security;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Persistence.SqlSugar;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Identity.Operations.Auth;

/// <summary>
/// <c>auth.lock</c> 的请求。
/// </summary>
/// <param name="Password">锁屏密码（本会话一次性；不能为空，否则谁拿到令牌调一次解锁就开了，4.x 同）。</param>
public sealed record LockRequest([StringLength(128)] string? Password);

/// <summary>
/// <c>auth.lock</c> 的结果。
/// </summary>
/// <param name="Reason">锁定原因（<c>ScreenLock</c>）。</param>
/// <param name="LockedAt">锁定时间。</param>
public sealed record LockResponse(string Reason, DateTimeOffset LockedAt);

/// <summary>
/// <c>auth.unlock</c> 的请求。
/// </summary>
/// <param name="Password">锁屏密码。</param>
public sealed record UnlockRequest([StringLength(128)] string? Password);

/// <summary>
/// <c>auth.unlock</c> 的结果。
/// </summary>
/// <param name="Unlocked">是否解开了（本来没锁为 <see langword="false"/>）。</param>
public sealed record UnlockResponse(bool Unlocked);

/// <summary>
/// 锁屏当前会话。
/// </summary>
/// <remarks>
/// 服务端强制，4.x 同。给当前令牌的 <c>sid</c> 会话记下锁定原因与锁屏密码的哈希，之后这个会话的请求除解锁与注销外都以 <c>IDENTITY_DENIED_002</c> 拒绝，
/// 只作用于这一个会话、不影响同一用户的其他端。只要求已认证。
/// </remarks>
/// <param name="sessions">会话仓储。</param>
/// <param name="gate">会话门禁。</param>
/// <param name="clock">时间源。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Policy = Policies.Authenticated)]
public sealed class LockSession(IRepository<SysUserSession, Guid> sessions, SessionGate gate, TimeProvider clock) : IOperation<LockRequest, LockResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "auth.lock";

    /// <inheritdoc />
    public async ValueTask<Result<LockResponse>> HandleAsync(LockRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        if (string.IsNullOrWhiteSpace(request.Password))
        {
            return Error.Validation(IdentityErrors.LockPasswordRequired, IdentityMessages.LockPasswordRequired);
        }

        var found = await CurrentSessions.FindAsync(sessions, context).ConfigureAwait(false);
        if (!found.TryGetValue(out var session))
        {
            return found.Error!;
        }

        var now = clock.GetUtcNow();
        session.LockReason = SessionLockReasons.ScreenLock;
        session.LockedAt = now;
        session.LockPasswordHash = PasswordHasher.Hash(request.Password);
        session.UnlockFailedAttempts = 0;
        var updated = await sessions.UpdateAsync(session, context.CancellationToken).ConfigureAwait(false);
        if (updated.IsFailure)
        {
            return updated.Error!;
        }

        await gate.InvalidateAsync(session.Id, context.CancellationToken).ConfigureAwait(false);
        return new LockResponse(SessionLockReasons.ScreenLock, now);
    }
}

/// <summary>
/// 用锁屏密码解锁。
/// </summary>
/// <remarks>
/// 4.x 同。本来没锁即无事；锁定原因不是锁屏即 <c>IDENTITY_CONFLICT_006</c>（别的锁定不能用锁屏密码开）；
/// 密码不对计一次失败（<c>IDENTITY_DENIED_013</c>，带剩余次数），连错 5 次吊销会话（<c>IDENTITY_DENIED_014</c>）；锁屏密码的哈希缺了也直接吊销。
/// 失败的调用会回滚，所以计数与吊销在调用之外的独立事务里写。锁定中的会话也能调用（门禁放行）。只要求已认证。
/// </remarks>
/// <param name="sessions">会话仓储。</param>
/// <param name="gate">会话门禁。</param>
/// <param name="scopes">作用域工厂（调用之外写计数与吊销）。</param>
/// <param name="clock">时间源。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Policy = Policies.Authenticated)]
public sealed class Unlock(IRepository<SysUserSession, Guid> sessions, SessionGate gate, IServiceScopeFactory scopes, TimeProvider clock) : IOperation<UnlockRequest, UnlockResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "auth.unlock";

    /// <inheritdoc />
    public async ValueTask<Result<UnlockResponse>> HandleAsync(UnlockRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var found = await CurrentSessions.FindAsync(sessions, context).ConfigureAwait(false);
        if (!found.TryGetValue(out var session))
        {
            return found.Error!;
        }

        if (session.LockReason is not { } reason)
        {
            return new UnlockResponse(false);
        }

        if (!string.Equals(reason, SessionLockReasons.ScreenLock, StringComparison.Ordinal))
        {
            return Error.Conflict(IdentityErrors.LockReasonUnsupported, IdentityMessages.LockReasonUnsupported, MessageArguments.Of(("reason", reason)));
        }

        var sessionId = session.Id;
        if (string.IsNullOrEmpty(session.LockPasswordHash))
        {
            // 哈希缺了（脏数据）绝不当作「没有密码」放行
            await RevokeAsync(sessionId, "lock-password-missing", context.CancellationToken).ConfigureAwait(false);
            return Error.Denied(IdentityErrors.SessionInvalid, IdentityMessages.SessionInvalid);
        }

        if (string.IsNullOrEmpty(request.Password) || !PasswordHasher.Verify(request.Password, session.LockPasswordHash))
        {
            var attempts = session.UnlockFailedAttempts + 1;
            if (attempts >= SessionLockReasons.MaxUnlockAttempts)
            {
                await RevokeAsync(sessionId, "unlock-exhausted", context.CancellationToken).ConfigureAwait(false);
                return Error.Denied(IdentityErrors.UnlockExhausted, IdentityMessages.UnlockExhausted);
            }

            await StoreScope.WriteAsync(scopes, async (services, ct) =>
            {
                var unitOfWork = services.GetRequiredService<SqlSugarUnitOfWork>();
                await unitOfWork.EnsureTransactionAsync(ct).ConfigureAwait(false);
                await unitOfWork.ConnectionFor(typeof(SysUserSession)).Updateable<SysUserSession>()
                    .SetColumns(s => new SysUserSession { UnlockFailedAttempts = attempts })
                    .Where(s => s.Id == sessionId).ExecuteCommandAsync(ct).ConfigureAwait(false);
                return Result.Success();
            }, context.CancellationToken).ConfigureAwait(false);
            return Error.Denied(IdentityErrors.UnlockPasswordWrong, IdentityMessages.UnlockPasswordWrong, MessageArguments.Of(("remaining", SessionLockReasons.MaxUnlockAttempts - attempts)));
        }

        session.LockReason = null;
        session.LockedAt = null;
        session.LockPasswordHash = null;
        session.UnlockFailedAttempts = 0;
        var updated = await sessions.UpdateAsync(session, context.CancellationToken).ConfigureAwait(false);
        if (updated.IsFailure)
        {
            return updated.Error!;
        }

        await gate.InvalidateAsync(sessionId, context.CancellationToken).ConfigureAwait(false);
        return new UnlockResponse(true);
    }

    /// <summary>
    /// 吊销并清掉锁屏态（否则留下一个既锁屏又失效的会话），在调用之外提交。
    /// </summary>
    private async Task RevokeAsync(Guid sessionId, string reason, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        await StoreScope.WriteAsync(scopes, async (services, ct) =>
        {
            var unitOfWork = services.GetRequiredService<SqlSugarUnitOfWork>();
            await unitOfWork.EnsureTransactionAsync(ct).ConfigureAwait(false);
            await unitOfWork.ConnectionFor(typeof(SysUserSession)).Updateable<SysUserSession>()
                .SetColumns(s => new SysUserSession { RevokedAt = now, RevokeReason = reason, LockReason = null, LockedAt = null, LockPasswordHash = null, UnlockFailedAttempts = 0 })
                .Where(s => s.Id == sessionId).ExecuteCommandAsync(ct).ConfigureAwait(false);
            return Result.Success();
        }, cancellationToken).ConfigureAwait(false);
        await gate.InvalidateAsync(sessionId, cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>
/// 当前令牌的会话（锁屏、解锁只作用于这一个会话）。
/// </summary>
internal static class CurrentSessions
{
    /// <summary>
    /// 取当前令牌 <c>sid</c> 的会话。
    /// </summary>
    /// <remarks>
    /// 令牌不带会话即 <c>IDENTITY_VALIDATION</c> 的无会话，会话不在或已失效即 <c>IDENTITY_DENIED_001</c>。
    /// </remarks>
    public static async ValueTask<Result<SysUserSession>> FindAsync(IRepository<SysUserSession, Guid> sessions, InvocationContext context)
    {
        var sid = context.GetSecurity().Principal.ClaimValues(IdentityClaims.Session).FirstOrDefault();
        if (sid is null || !Guid.TryParse(sid, out var sessionId))
        {
            return Error.Validation(IdentityErrors.NoSession, IdentityMessages.NoSession);
        }

        var found = await sessions.FindAsync(sessionId, context.CancellationToken).ConfigureAwait(false);
        return found.TryGetValue(out var session) && session.RevokedAt is null
            ? session
            : Error.Denied(IdentityErrors.SessionInvalid, IdentityMessages.SessionInvalid);
    }
}
