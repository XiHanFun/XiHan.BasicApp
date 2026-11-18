// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Auditing;
using XiHan.BasicApp.Contracts.Realtime;
using XiHan.BasicApp.Contracts.Security;
using XiHan.BasicApp.Identity.Domain.Entities;
using XiHan.BasicApp.Identity.Domain.Services;
using XiHan.BasicApp.Identity.Infrastructure;
using XiHan.Framework.Domain.Specifications;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Kernel.Security;
using XiHan.Framework.Persistence.Abstractions.Queries;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Identity.Operations.Auth;

/// <summary>
/// 注销当前会话
/// </summary>
/// <remarks>
/// 自助操作，只要求已认证。吊销当前令牌的 <c>sid</c> 会话并让门禁缓存失效；之后同一会话的访问令牌与刷新令牌都不再可用
/// 由这个会话发起的模仿会话一并吊销（发起人走了，被模仿身份的令牌不能继续用）
/// 提交后断开这个会话的实时连接；账号本人的会话退出时，给退出的这一侧与账号别的设备所在的各侧发一条「其他设备退出登录」的安全通知（断开排在提醒之前，退出的这台设备收不到它）
/// </remarks>
/// <param name="sessions">会话仓储</param>
/// <param name="accounts">账号域服务（账号别的设备）</param>
/// <param name="notices">发给本人的安全通知</param>
/// <param name="pushes">实时推送（没装 Realtime 即没有）</param>
/// <param name="gate">会话门禁</param>
/// <param name="clock">时间源</param>
[Operation(Id, Expose = Expose.Http, Policy = Policies.Authenticated)]
public sealed class Logout(IRepository<SysUserSession, Guid> sessions, UserAccountService accounts, SecurityNotices notices, IEnumerable<IRealtimePush> pushes, SessionGate gate, TimeProvider clock) : IOperation<LogoutRequest, LogoutResponse>
{
    /// <summary>
    /// 操作 ID
    /// </summary>
    public const string Id = "auth.logout";

    /// <inheritdoc />
    public async ValueTask<Result<LogoutResponse>> HandleAsync(LogoutRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        context.AddAuditLog(new LoginLogEntry(LoginEvent.Logout));
        var sid = context.GetSecurity().Principal.ClaimValues(IdentityClaims.Session).FirstOrDefault();
        if (sid is null || !Guid.TryParse(sid, out var sessionId))
        {
            return Error.Validation(IdentityErrors.NoSession, IdentityMessages.NoSession);
        }

        if (!(await sessions.FindAsync(sessionId, context.CancellationToken).ConfigureAwait(false)).TryGetValue(out var session))
        {
            return Error.Denied(IdentityErrors.SessionInvalid, IdentityMessages.SessionInvalid);
        }

        var now = clock.GetUtcNow();
        if (session.RevokedAt is null)
        {
            foreach (var push in pushes)
            {
                await push.DisconnectAsync([sessionId], context.CancellationToken).ConfigureAwait(false);
            }

            // 提醒在第一次写入之前发出（跨侧的投递独立提交）；模仿会话不是账号本人的设备
            if (!session.ImpersonatorUserId.HasValue)
            {
                var others = (await accounts.DevicesOfAsync(session.UserId, context.CancellationToken).ConfigureAwait(false)).Where(s => s.Id != sessionId).ToList();
                await notices.LogoutAsync(session, others, context).ConfigureAwait(false);
            }

            session.RevokedAt = now;
            session.RevokeReason = "logout";
            var updated = await sessions.UpdateAsync(session, context.CancellationToken).ConfigureAwait(false);
            if (updated.IsFailure)
            {
                return updated.Error!;
            }
        }

        await gate.InvalidateAsync(sessionId, context.CancellationToken).ConfigureAwait(false);
        var impersonations = await sessions.ListAsync(EntityQuery.All<SysUserSession>().Where(new Specification<SysUserSession>(s => s.ImpersonatorSessionId == sessionId && !s.RevokedAt.HasValue)), context.CancellationToken).ConfigureAwait(false);
        foreach (var impersonation in impersonations)
        {
            impersonation.RevokedAt = now;
            impersonation.RevokeReason = "impersonator-logout";
        }

        foreach (var outcome in await sessions.UpdateRangeAsync(impersonations, context.CancellationToken).ConfigureAwait(false))
        {
            if (outcome.IsFailure)
            {
                return outcome.Error!;
            }
        }

        foreach (var impersonation in impersonations)
        {
            await gate.InvalidateAsync(impersonation.Id, context.CancellationToken).ConfigureAwait(false);
        }

        return new LogoutResponse(sessionId, session.RevokedAt.Value);
    }
}
