// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts;
using XiHan.BasicApp.Contracts.Auditing;
using XiHan.BasicApp.Contracts.MultiTenancy;
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
/// 把当前会话切换到目标租户或平台。
/// </summary>
/// <remarks>
/// 操作 <c>auth.switch-tenant</c>（自助操作，只要求已认证）。当前会话换到目标上下文——注册地租户、生效中的成员租户（含运维支持），或平台（平台账号）。
/// 会话续用（同一 <c>sid</c>），刷新令牌轮换，访问令牌按新租户重算角色；客户端换令牌后整页重载。模仿态的会话不能切换。换上下文之前先过登录守卫（目标一侧的登录时机访问策略）。
/// </remarks>
/// <param name="users">用户仓储。</param>
/// <param name="sessions">会话仓储。</param>
/// <param name="accounts">账号域服务。</param>
/// <param name="memberships">成员关系（Tenancy 实现；没有即只有注册地可进）。</param>
/// <param name="issuer">令牌签发器。</param>
/// <param name="guards">登录守卫。</param>
/// <param name="activators">会话角色激活（令牌只带激活的角色）。</param>
/// <param name="options">身份配置。</param>
/// <param name="clock">时间源。</param>
/// <param name="tenants">框架租户目录。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Policy = Policies.Authenticated)]
public sealed class SwitchTenant(
    IRepository<SysUser, Guid> users,
    IRepository<SysUserSession, Guid> sessions,
    UserAccountService accounts,
    IEnumerable<ITenantMembershipResolver> memberships,
    AccessTokenIssuer issuer,
    IEnumerable<ILoginGuard> guards,
    IEnumerable<ISessionRoleActivator> activators,
    IdentityOptions options,
    TimeProvider clock,
    ITenantStore tenants) : IOperation<SwitchTenantRequest, TokenResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "auth.switch-tenant";

    /// <inheritdoc />
    public async ValueTask<Result<TokenResponse>> HandleAsync(SwitchTenantRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var now = clock.GetUtcNow();
        // 切换成功即记在目标一侧（目标租户能看到谁进来了），没切成记在原来一侧；说明里是另一端
        var log = new LoginLogEntry(LoginEvent.SwitchTenant) { Detail = request.TenantId?.Trim() };
        context.AddAuditLog(log);
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

        var target = string.IsNullOrWhiteSpace(request.TenantId) || string.Equals(request.TenantId.Trim(), TenantIds.Platform, StringComparison.Ordinal) ? null : request.TenantId.Trim();
        log.AccountTenantId = user.HomeTenantId;
        log.Detail = target ?? TenantIds.Platform;
        var isPlatformAccount = string.Equals(user.HomeTenantId, TenantIds.Platform, StringComparison.Ordinal);
        string? tenantName = null;
        if (target is null)
        {
            if (!isPlatformAccount)
            {
                return Error.Denied(IdentityErrors.TenantNotAccessible, IdentityMessages.TenantNotAccessible, MessageArguments.Of(("tenantId", TenantIds.Platform)));
            }
        }
        else
        {
            var directory = await tenants.FindAsync(target, context.CancellationToken).ConfigureAwait(false);
            if (!directory.TryGetValue(out var info) || !info.IsEnabled)
            {
                return Error.Denied(IdentityErrors.TenantUnavailable, IdentityMessages.TenantUnavailable, MessageArguments.Of(("tenantId", target)));
            }

            tenantName = info.DisplayName;
            if (!string.Equals(user.HomeTenantId, target, StringComparison.Ordinal) && !await CanEnterAsync(target, user.Id, context.CancellationToken).ConfigureAwait(false))
            {
                return Error.Denied(IdentityErrors.TenantNotAccessible, IdentityMessages.TenantNotAccessible, MessageArguments.Of(("tenantId", target)));
            }
        }

        var roles = await accounts.RolesOfAsync(user.Id, target, context.CancellationToken).ConfigureAwait(false);
        var guarded = await guards.CheckAllAsync(new LoginAttempt(target ?? TenantIds.Platform, user.Id, user.UserName, roles), context).ConfigureAwait(false);
        if (guarded.IsFailure)
        {
            return guarded.Error!;
        }

        log.TenantId = target ?? TenantIds.Platform;
        log.Detail = session.ActiveTenantId ?? TenantIds.Platform;

        // 续用会话：换上下文、轮换刷新令牌（旧的立即失效）、顺延有效期
        var refreshToken = RefreshTokens.Generate();
        session.ActiveTenantId = target;
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

        var active = await activators.StartAsync(session.Id, user.Id, target, roles, context).ConfigureAwait(false);
        var access = issuer.Issue(user, session.Id, target, active);
        return new TokenResponse(access.Token, access.ExpiresAt, refreshToken, session.RefreshExpiresAt, session.Id, target, security.MustChangePassword, UserResponse.From(user), tenantName);
    }

    private async Task<bool> CanEnterAsync(string tenantId, Guid userId, CancellationToken cancellationToken)
    {
        foreach (var resolver in memberships)
        {
            if (await resolver.CanEnterAsync(tenantId, userId, cancellationToken).ConfigureAwait(false))
            {
                return true;
            }
        }

        return false;
    }
}
