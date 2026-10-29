// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using XiHan.BasicApp.Contracts;
using XiHan.BasicApp.Contracts.Auditing;
using XiHan.BasicApp.Contracts.Security;
using XiHan.BasicApp.Identity.Domain.Entities;
using XiHan.BasicApp.Identity.Domain.Services;
using XiHan.BasicApp.Identity.Infrastructure;
using XiHan.BasicApp.Identity.Operations.Users;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Kernel.Security;
using XiHan.Framework.MultiTenancy.Abstractions.Tenants;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Identity.Operations.Auth;

/// <summary>
/// <c>auth.activate-roles</c> 的请求：本会话要激活的角色。
/// </summary>
/// <param name="RoleIds">要激活的角色 ID，1 到 100 个；须都此刻持有，且合起来不违反动态职责分离。</param>
public sealed record ActivateRolesRequest([Required, MinLength(1), MaxLength(100)] IReadOnlyList<Guid> RoleIds);

/// <summary>
/// 切换角色 <c>auth.activate-roles</c>（自助操作，只要求已认证）：换本会话激活的角色并换签令牌（同 <c>auth.switch-tenant</c> 的续接方式：会话续用、刷新令牌轮换）。
/// 要激活的须都此刻持有、且不违反动态职责分离（拦截类违规即 <c>ACCESS_CONFLICT_012</c>）；其余持有的记停用。模仿态的会话不能切换。登录日志记为 <see cref="LoginEvent.RoleActivation"/>。
/// </summary>
/// <param name="users">用户仓储。</param>
/// <param name="sessions">会话仓储。</param>
/// <param name="accounts">账号域服务。</param>
/// <param name="activators">会话角色激活。</param>
/// <param name="issuer">令牌签发器。</param>
/// <param name="options">身份配置。</param>
/// <param name="clock">时间源。</param>
/// <param name="tenants">框架租户目录。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Policy = Policies.Authenticated)]
public sealed class ActivateRoles(
    IRepository<SysUser, Guid> users,
    IRepository<SysUserSession, Guid> sessions,
    UserAccountService accounts,
    IEnumerable<ISessionRoleActivator> activators,
    AccessTokenIssuer issuer,
    IdentityOptions options,
    TimeProvider clock,
    ITenantStore tenants) : IOperation<ActivateRolesRequest, TokenResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "auth.activate-roles";

    /// <inheritdoc />
    public async ValueTask<Result<TokenResponse>> HandleAsync(ActivateRolesRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var now = clock.GetUtcNow();
        var sid = context.GetSecurity().Principal.ClaimValues(IdentityClaims.Session).FirstOrDefault();
        if (sid is null || !Guid.TryParse(sid, out var sessionId))
        {
            return Error.Validation(IdentityErrors.NoSession, IdentityMessages.NoSession);
        }

        if (!(await sessions.FindAsync(sessionId, context.CancellationToken).ConfigureAwait(false)).TryGetValue(out var session) || !session.IsActive(now))
        {
            return Error.Denied(IdentityErrors.SessionInvalid, IdentityMessages.SessionInvalid);
        }

        if (session.ImpersonatorUserId is not null)
        {
            return Error.Conflict(IdentityErrors.ImpersonationActive, IdentityMessages.ImpersonationActive);
        }

        if (!(await users.FindAsync(session.UserId, context.CancellationToken).ConfigureAwait(false)).TryGetValue(out var user) || user.Status != UserStatus.Active)
        {
            return Error.Denied(IdentityErrors.AccountUnavailable, IdentityMessages.AccountUnavailable, MessageArguments.Of(("status", user?.Status.ToString() ?? "Deleted")));
        }

        var log = new LoginLogEntry(LoginEvent.RoleActivation)
        {
            TenantId = session.ActiveTenantId ?? TenantIds.Platform,
            AccountTenantId = user.HomeTenantId,
            UserId = user.Id,
            UserName = user.UserName,
            SessionId = session.Id,
        };
        context.AddAuditLog(log);
        var held = await accounts.RolesOfAsync(user.Id, session.ActiveTenantId, context.CancellationToken).ConfigureAwait(false);
        var activated = await activators.ActivateAsync(session.Id, user.Id, session.ActiveTenantId, held, [.. request.RoleIds.Select(static r => r.ToString("D"))], context).ConfigureAwait(false);
        if (!activated.TryGetValue(out var active))
        {
            return activated.Error!;
        }

        log.Detail = string.Join(',', active);

        // 续用会话：轮换刷新令牌（旧的立即失效）、顺延有效期
        var refreshToken = RefreshTokens.Generate();
        session.RefreshTokenHash = RefreshTokens.Hash(refreshToken);
        session.LastSeenAt = now;
        session.RefreshExpiresAt = now + options.RefreshTokenLifetime;
        var updated = await sessions.UpdateAsync(session, context.CancellationToken).ConfigureAwait(false);
        if (updated.IsFailure)
        {
            return updated.Error!;
        }

        var securityResult = await accounts.SecurityOfAsync(user.Id, context.CancellationToken).ConfigureAwait(false);
        if (!securityResult.TryGetValue(out var security))
        {
            return securityResult.Error!;
        }

        var access = issuer.Issue(user, session.Id, session.ActiveTenantId, active);
        var tenantName = await TenantNames.ResolveAsync(tenants, session.ActiveTenantId, context.CancellationToken).ConfigureAwait(false);
        return new TokenResponse(access.Token, access.ExpiresAt, refreshToken, session.RefreshExpiresAt, session.Id, session.ActiveTenantId, security.MustChangePassword, UserResponse.From(user), tenantName);
    }
}
