// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Auditing;
using XiHan.BasicApp.Contracts.Security;
using XiHan.BasicApp.Identity.Domain.Entities;
using XiHan.BasicApp.Identity.Domain.Services;
using XiHan.BasicApp.Identity.Infrastructure;
using XiHan.BasicApp.Identity.Operations.Auth;
using XiHan.BasicApp.Identity.Operations.Users;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Kernel.Security;
using XiHan.Framework.MultiTenancy.Abstractions.Tenants;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Identity.Operations.Impersonation;

/// <summary>
/// 结束模仿登录 <c>impersonation.stop</c>（自助操作：模仿态的主体是被模仿者，不持有模仿权限；准入靠令牌 <c>act</c> 与会话行上的发起人相等）。
/// 吊销模仿会话，在发起人原会话上轮换刷新令牌并签发其本人身份的令牌；原会话已失效即要求重新登录。
/// </summary>
/// <param name="users">用户仓储。</param>
/// <param name="sessions">会话仓储。</param>
/// <param name="accounts">账号域服务。</param>
/// <param name="gate">会话门禁。</param>
/// <param name="issuer">令牌签发器。</param>
/// <param name="options">身份配置。</param>
/// <param name="clock">时间源。</param>
/// <param name="tenants">框架租户目录。</param>
/// <param name="activators">会话角色激活（令牌只带激活的角色）。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Policy = Policies.Authenticated)]
public sealed class StopImpersonation(
    IRepository<SysUser, Guid> users,
    IRepository<SysUserSession, Guid> sessions,
    UserAccountService accounts,
    SessionGate gate,
    AccessTokenIssuer issuer,
    IdentityOptions options,
    TimeProvider clock,
    ITenantStore tenants,
    IEnumerable<ISessionRoleActivator> activators) : IOperation<StopImpersonationRequest, TokenResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "impersonation.stop";

    /// <inheritdoc />
    public async ValueTask<Result<TokenResponse>> HandleAsync(StopImpersonationRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var now = clock.GetUtcNow();
        var principal = context.GetSecurity().Principal;
        // 调用方是被模仿的人（用户与会话取调用方），发起人取令牌的模仿者声明
        context.AddAuditLog(new LoginLogEntry(LoginEvent.ImpersonationStop) { ImpersonatorUserId = Guid.TryParse(principal.ClaimValues(IdentityClaims.Actor).FirstOrDefault(), out var impersonator) ? impersonator : null });
        var sid = principal.ClaimValues(IdentityClaims.Session).FirstOrDefault();
        if (sid is null || !Guid.TryParse(sid, out var sessionId))
        {
            return Error.Validation(IdentityErrors.NoSession, IdentityMessages.NoSession);
        }

        if (!(await sessions.FindAsync(sessionId, context.CancellationToken).ConfigureAwait(false)).TryGetValue(out var session) || !session.IsActive(now))
        {
            return Error.Denied(IdentityErrors.SessionInvalid, IdentityMessages.SessionInvalid);
        }

        var actorClaim = principal.ClaimValues(IdentityClaims.Actor).FirstOrDefault();
        if (session.ImpersonatorUserId is not { } actorId || session.ImpersonatorSessionId is not { } originId || actorClaim is null || !Guid.TryParse(actorClaim, out var claimedActor) || claimedActor != actorId)
        {
            return Error.Validation(IdentityErrors.NotImpersonating, IdentityMessages.NotImpersonating);
        }

        // 先关掉模仿会话：之后同一令牌再来就被门禁拒绝
        session.RevokedAt = now;
        session.RevokeReason = "impersonation-stopped";
        var revoked = await sessions.UpdateAsync(session, context.CancellationToken).ConfigureAwait(false);
        if (revoked.IsFailure)
        {
            return revoked.Error!;
        }

        await gate.InvalidateAsync(session.Id, context.CancellationToken).ConfigureAwait(false);
        if (!(await sessions.FindAsync(originId, context.CancellationToken).ConfigureAwait(false)).TryGetValue(out var origin) || !origin.IsActive(now) || origin.UserId != actorId)
        {
            return Error.Denied(IdentityErrors.SessionInvalid, IdentityMessages.SessionInvalid);
        }

        if (!(await users.FindAsync(actorId, context.CancellationToken).ConfigureAwait(false)).TryGetValue(out var actor) || actor.Status != UserStatus.Active)
        {
            return Error.Denied(IdentityErrors.AccountUnavailable, IdentityMessages.AccountUnavailable, MessageArguments.Of(("status", actor?.Status.ToString() ?? "Deleted")));
        }

        var refreshToken = RefreshTokens.Generate();
        origin.RefreshTokenHash = RefreshTokens.Hash(refreshToken);
        origin.LastSeenAt = now;
        origin.RefreshExpiresAt = now + options.RefreshTokenLifetime;
        var updated = await sessions.UpdateAsync(origin, context.CancellationToken).ConfigureAwait(false);
        if (updated.IsFailure)
        {
            return updated.Error!;
        }

        var securityResult = await accounts.SecurityOfAsync(actor.Id, context.CancellationToken).ConfigureAwait(false);
        if (!securityResult.TryGetValue(out var security))
        {
            return securityResult.Error!;
        }

        var roles = await accounts.RolesOfAsync(actor.Id, origin.ActiveTenantId, context.CancellationToken).ConfigureAwait(false);
        var active = await activators.RefreshAsync(origin.Id, actor.Id, origin.ActiveTenantId, roles, context).ConfigureAwait(false);
        var access = issuer.Issue(actor, origin.Id, origin.ActiveTenantId, active);
        var tenantName = await TenantNames.ResolveAsync(tenants, origin.ActiveTenantId, context.CancellationToken).ConfigureAwait(false);
        return new TokenResponse(access.Token, access.ExpiresAt, refreshToken, origin.RefreshExpiresAt, origin.Id, origin.ActiveTenantId, security.MustChangePassword, UserResponse.From(actor), tenantName);
    }
}
