// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Security;
using XiHan.BasicApp.Identity.Domain.Entities;
using XiHan.BasicApp.Identity.Infrastructure;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Kernel.Security;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Identity.Operations.Auth;

/// <summary>
/// 注销（自助操作，只要求已认证）：吊销当前令牌的 <c>sid</c> 会话并让门禁缓存失效；之后同一会话的访问令牌与刷新令牌都不再可用。
/// </summary>
/// <param name="sessions">会话仓储。</param>
/// <param name="gate">会话门禁。</param>
/// <param name="clock">时间源。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Policy = Policies.Authenticated)]
public sealed class Logout(IRepository<SysUserSession, Guid> sessions, SessionGate gate, TimeProvider clock) : IOperation<LogoutRequest, LogoutResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "auth.logout";

    /// <inheritdoc />
    public async ValueTask<Result<LogoutResponse>> HandleAsync(LogoutRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var sid = context.GetSecurity().Principal.ClaimValues(IdentityClaims.Session).FirstOrDefault();
        if (sid is null || !Guid.TryParse(sid, out var sessionId))
        {
            return Error.Validation(IdentityErrors.NoSession, IdentityMessages.NoSession);
        }

        if (!(await sessions.FindAsync(sessionId, context.CancellationToken).ConfigureAwait(false)).TryGetValue(out var session))
        {
            return Error.Denied(IdentityErrors.SessionInvalid, IdentityMessages.SessionInvalid);
        }

        if (session.RevokedAt is null)
        {
            session.RevokedAt = clock.GetUtcNow();
            session.RevokeReason = "logout";
            var updated = await sessions.UpdateAsync(session, context.CancellationToken).ConfigureAwait(false);
            if (updated.IsFailure)
            {
                return updated.Error!;
            }
        }

        await gate.InvalidateAsync(sessionId, context.CancellationToken).ConfigureAwait(false);
        return new LogoutResponse(sessionId, session.RevokedAt.Value);
    }
}
