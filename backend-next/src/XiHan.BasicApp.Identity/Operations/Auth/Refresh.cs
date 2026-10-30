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
using XiHan.Framework.Domain.Specifications;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.MultiTenancy.Abstractions.Tenants;
using XiHan.Framework.Persistence.Abstractions.Queries;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Identity.Operations.Auth;

/// <summary>
/// 刷新令牌：轮换刷新令牌并签发新的访问令牌。
/// </summary>
/// <remarks>
/// 匿名操作。按散列找到有效会话，轮换刷新令牌、顺延会话有效期，签发新的访问令牌。会话已吊销或过期即 <c>IDENTITY_DENIED_001</c>，客户端重新登录；会话以成员身份所在的租户里，成员关系被移除或到期即 <c>IDENTITY_DENIED_006</c>。
/// </remarks>
/// <param name="accounts">账号域服务。</param>
/// <param name="users">用户仓储。</param>
/// <param name="sessions">会话仓储。</param>
/// <param name="issuer">令牌签发器。</param>
/// <param name="options">身份配置。</param>
/// <param name="clock">时间源。</param>
/// <param name="tenants">框架租户目录：会话所属租户已停用或已删即不再续签。</param>
/// <param name="activators">会话角色激活（令牌只带激活的角色）。</param>
/// <param name="memberships">成员关系（Tenancy 实现）：会话在成员租户里时，成员关系被移除或到期即不再续签。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http)]
public sealed class Refresh(
    UserAccountService accounts,
    IRepository<SysUser, Guid> users,
    IRepository<SysUserSession, Guid> sessions,
    AccessTokenIssuer issuer,
    IdentityOptions options,
    TimeProvider clock,
    ITenantStore tenants,
    IEnumerable<ISessionRoleActivator> activators,
    IEnumerable<ITenantMembershipResolver> memberships) : IOperation<RefreshRequest, TokenResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "auth.refresh";

    /// <inheritdoc />
    public async ValueTask<Result<TokenResponse>> HandleAsync(RefreshRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var now = clock.GetUtcNow();
        var hash = RefreshTokens.Hash(request.RefreshToken);
        var matches = await sessions.ListAsync(EntityQuery.All<SysUserSession>().Where(new Specification<SysUserSession>(s => s.RefreshTokenHash == hash)).Page(0, 1), context.CancellationToken).ConfigureAwait(false);
        if (matches.Count == 0 || !matches[0].IsActive(now))
        {
            return Error.Denied(IdentityErrors.SessionInvalid, IdentityMessages.SessionInvalid);
        }

        var session = matches[0];
        // 认出会话之后才记登录日志（拿着无效令牌的调用不记）；落在会话当前所在的一侧
        var log = new LoginLogEntry(LoginEvent.Refresh) { TenantId = session.ActiveTenantId ?? TenantIds.Platform, UserId = session.UserId, SessionId = session.Id, ImpersonatorUserId = session.ImpersonatorUserId };
        context.AddAuditLog(log);
        string? tenantName = null;
        if (session.ActiveTenantId is { } tenantId)
        {
            var directory = await tenants.FindAsync(tenantId, context.CancellationToken).ConfigureAwait(false);
            if (!directory.TryGetValue(out var info) || !info.IsEnabled)
            {
                return Error.Denied(IdentityErrors.TenantUnavailable, IdentityMessages.TenantUnavailable, MessageArguments.Of(("tenantId", tenantId)));
            }

            tenantName = info.DisplayName;
        }

        // 模仿会话随发起人的原会话一起失效，续签前再核一次
        string? impersonator = null;
        if (session.ImpersonatorSessionId is { } originId)
        {
            if (!(await sessions.FindAsync(originId, context.CancellationToken).ConfigureAwait(false)).TryGetValue(out var origin) || !origin.IsActive(now))
            {
                return Error.Denied(IdentityErrors.SessionInvalid, IdentityMessages.SessionInvalid);
            }

            if (session.ImpersonatorUserId is { } actorId && (await users.FindAsync(actorId, context.CancellationToken).ConfigureAwait(false)).TryGetValue(out var actor))
            {
                impersonator = actor.UserName;
            }
        }

        if (!(await users.FindAsync(session.UserId, context.CancellationToken).ConfigureAwait(false)).TryGetValue(out var user) || user.Status != UserStatus.Active)
        {
            return Error.Denied(IdentityErrors.AccountUnavailable, IdentityMessages.AccountUnavailable, MessageArguments.Of(("status", user?.Status.ToString() ?? "Deleted")));
        }

        log.UserName = user.UserName;
        log.AccountTenantId = user.HomeTenantId;
        // 以成员身份所在的租户（非注册地、非模仿）：成员关系移除或到期即不再续签
        if (session.ActiveTenantId is { } memberTenant && session.ImpersonatorUserId is null && !string.Equals(user.HomeTenantId, memberTenant, StringComparison.Ordinal) && !await CanEnterAsync(memberTenant, user.Id, context.CancellationToken).ConfigureAwait(false))
        {
            return Error.Denied(IdentityErrors.TenantNotAccessible, IdentityMessages.TenantNotAccessible, MessageArguments.Of(("tenantId", memberTenant)));
        }

        var securityResult = await accounts.SecurityOfAsync(user.Id, context.CancellationToken).ConfigureAwait(false);
        if (!securityResult.TryGetValue(out var security))
        {
            return securityResult.Error!;
        }

        // 轮换：旧刷新令牌立即失效；会话有效期按滑动口径顺延
        var refreshToken = RefreshTokens.Generate();
        session.RefreshTokenHash = RefreshTokens.Hash(refreshToken);
        session.LastSeenAt = now;
        session.RefreshExpiresAt = now + options.RefreshTokenLifetime;
        var updated = await sessions.UpdateAsync(session, context.CancellationToken).ConfigureAwait(false);
        if (updated.IsFailure)
        {
            return updated.Error!;
        }

        // 角色在每次刷新时重算：角色指派的变化最迟在下一次刷新生效；激活集沿用并按当前的动态职责分离规则重新核对
        var roles = await accounts.RolesOfAsync(user.Id, session.ActiveTenantId, context.CancellationToken).ConfigureAwait(false);
        var active = await activators.RefreshAsync(session.Id, user.Id, session.ActiveTenantId, roles, context).ConfigureAwait(false);
        var access = issuer.Issue(user, session.Id, session.ActiveTenantId, active, session.ImpersonatorUserId);
        return new TokenResponse(access.Token, access.ExpiresAt, refreshToken, session.RefreshExpiresAt, session.Id, session.ActiveTenantId, security.MustChangePassword, UserResponse.From(user), tenantName, impersonator);
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
