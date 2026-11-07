// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts;
using XiHan.BasicApp.Contracts.Auditing;
using XiHan.BasicApp.Contracts.MultiTenancy;
using XiHan.BasicApp.Contracts.Security;
using XiHan.BasicApp.Identity.Domain.Entities;
using XiHan.BasicApp.Identity.Domain.Services;
using XiHan.BasicApp.Identity.Infrastructure;
using XiHan.BasicApp.Identity.Operations.Auth;
using XiHan.BasicApp.Identity.Operations.Users;
using XiHan.Framework.Domain.Specifications;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Kernel.Security;
using XiHan.Framework.MultiTenancy.Abstractions.Tenants;
using XiHan.Framework.Persistence.Abstractions.Identifiers;
using XiHan.Framework.Persistence.Abstractions.Queries;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Identity.Operations.Impersonation;

/// <summary>
/// 发起模仿登录
/// </summary>
/// <remarks>
/// 操作 <c>impersonation.start</c>。以目标用户的身份另开一个会话并签发令牌（<c>sub</c> 是目标、<c>act</c> 是发起人），客户端换令牌后整页重载
/// 不是目标用户的一次登录：不写登录痕迹。发起人的会话保留，<c>impersonation.stop</c> 时回到它
/// 范围：不填租户即发起人当前所在的上下文；租户主体只能在自己的租户里模仿；平台主体可指定租户（目标须是该租户的注册地用户或成员）或平台（目标须是平台账号）
/// 不能模仿自己，不能在模仿态里再模仿，不能模仿超级管理员，也不能模仿同级或更高权限的账号（<see cref="IImpersonationGuard"/>）
/// 模仿会话的时长与要不要通知被模仿者看系统参数 <see cref="ImpersonationSettings.ConfigKey"/>（缺省 30 分钟、通知）。时长是从发起算起的硬期限，<c>auth.refresh</c> 只换令牌不顺延，到期即结束，要接着用就重新发起
/// </remarks>
/// <param name="users">用户仓储</param>
/// <param name="sessions">会话仓储</param>
/// <param name="accounts">账号域服务</param>
/// <param name="memberships">成员关系（Tenancy 实现）</param>
/// <param name="issuer">令牌签发器</param>
/// <param name="ids">标识生成器</param>
/// <param name="clock">时间源</param>
/// <param name="tenants">框架租户目录</param>
/// <param name="activators">会话角色激活（令牌只带激活的角色）</param>
/// <param name="notices">通知被模仿者</param>
/// <param name="guards">模仿的权限上限（Access 实现）</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = IdentityPermissions.ImpersonationStart)]
public sealed class StartImpersonation(
    IRepository<SysUser, Guid> users,
    IRepository<SysUserSession, Guid> sessions,
    UserAccountService accounts,
    IEnumerable<ITenantMembershipResolver> memberships,
    AccessTokenIssuer issuer,
    IIdGenerator<Guid> ids,
    TimeProvider clock,
    ITenantStore tenants,
    IEnumerable<ISessionRoleActivator> activators,
    SecurityNotices notices,
    IEnumerable<IImpersonationGuard> guards) : IOperation<StartImpersonationRequest, TokenResponse>
{
    /// <summary>
    /// 操作 ID
    /// </summary>
    public const string Id = "impersonation.start";

    /// <inheritdoc />
    public async ValueTask<Result<TokenResponse>> HandleAsync(StartImpersonationRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var now = clock.GetUtcNow();
        var security = context.GetSecurity();
        // 发起人记在模仿者一栏；目标认出之后用户换成被模仿的人，成功即记在目标上下文那一侧
        var log = new LoginLogEntry(LoginEvent.ImpersonationStart) { ImpersonatorUserId = Guid.TryParse(security.Principal.Id, out var actorId) ? actorId : null, Detail = request.TargetUserId.ToString("D") };
        context.AddAuditLog(log);
        var sid = security.Principal.ClaimValues(IdentityClaims.Session).FirstOrDefault();
        if (sid is null || !Guid.TryParse(sid, out var originSessionId))
        {
            return Error.Validation(IdentityErrors.NoSession, IdentityMessages.NoSession);
        }

        if (!(await sessions.FindAsync(originSessionId, context.CancellationToken).ConfigureAwait(false)).TryGetValue(out var origin) || !origin.IsActive(now))
        {
            return Error.Denied(IdentityErrors.SessionInvalid, IdentityMessages.SessionInvalid);
        }

        if (origin.ImpersonatorUserId is not null)
        {
            return Error.Conflict(IdentityErrors.ImpersonationActive, IdentityMessages.ImpersonationActive);
        }

        // 一条原会话同时只挂一条模仿会话，要先结束
        var running = await sessions.CountAsync(EntityQuery.All<SysUserSession>().Where(new Specification<SysUserSession>(s => s.ImpersonatorSessionId == originSessionId && !s.RevokedAt.HasValue && s.RefreshExpiresAt > now)), context.CancellationToken).ConfigureAwait(false);
        if (running > 0)
        {
            return Error.Conflict(IdentityErrors.ImpersonationRunning, IdentityMessages.ImpersonationRunning);
        }

        if (!(await users.FindAsync(origin.UserId, context.CancellationToken).ConfigureAwait(false)).TryGetValue(out var actor) || actor.Status != UserStatus.Active)
        {
            return Error.Denied(IdentityErrors.AccountUnavailable, IdentityMessages.AccountUnavailable, MessageArguments.Of(("status", actor?.Status.ToString() ?? "Deleted")));
        }

        // 目标上下文：不填即发起人当前上下文；租户主体钉在自己的租户，平台主体按请求
        var actorTenant = security.Tenant?.Id;
        var requested = ImpersonationScope.Resolve(request.TenantId, actorTenant);
        if (actorTenant is not null && !string.Equals(actorTenant, requested, StringComparison.Ordinal))
        {
            return Error.Denied(IdentityErrors.TenantNotAccessible, IdentityMessages.TenantNotAccessible, MessageArguments.Of(("tenantId", requested ?? TenantIds.Platform)));
        }

        string? tenantName = null;
        if (requested is not null)
        {
            var directory = await TenantDirectoryGate.CheckAsync(tenants, requested, clock.GetUtcNow(), context.CancellationToken).ConfigureAwait(false);
            if (!directory.TryGetValue(out var info))
            {
                return directory.Error!;
            }

            tenantName = info.DisplayName;
        }

        if (request.TargetUserId == actor.Id || !(await users.FindAsync(request.TargetUserId, context.CancellationToken).ConfigureAwait(false)).TryGetValue(out var target) || target.Status != UserStatus.Active)
        {
            return Error.Denied(IdentityErrors.ImpersonationTargetUnavailable, IdentityMessages.ImpersonationTargetUnavailable, MessageArguments.Of(("id", request.TargetUserId)));
        }

        var eligible = string.Equals(target.HomeTenantId, requested ?? TenantIds.Platform, StringComparison.Ordinal)
            || (requested is not null && await IsMemberAsync(requested, target.Id, context.CancellationToken).ConfigureAwait(false));
        if (!eligible)
        {
            return Error.Denied(IdentityErrors.ImpersonationTargetUnavailable, IdentityMessages.ImpersonationTargetUnavailable, MessageArguments.Of(("id", request.TargetUserId)));
        }

        // 目标不够格与目标权限超出发起人是同一个回答：不透露对方持有什么
        if (IsSuperAdministrator(target))
        {
            return Error.Denied(IdentityErrors.ImpersonationTargetUnavailable, IdentityMessages.ImpersonationTargetUnavailable, MessageArguments.Of(("id", request.TargetUserId)));
        }

        foreach (var guard in guards)
        {
            var allowed = await guard.AllowsAsync(security, target.Id, requested, context.CancellationToken).ConfigureAwait(false);
            if (!allowed.TryGetValue(out var permitted))
            {
                return allowed.Error!;
            }

            if (!permitted)
            {
                return Error.Denied(IdentityErrors.ImpersonationTargetUnavailable, IdentityMessages.ImpersonationTargetUnavailable, MessageArguments.Of(("id", request.TargetUserId)));
            }
        }

        log.TenantId = requested ?? TenantIds.Platform;
        log.UserId = target.Id;
        log.UserName = target.UserName;
        log.AccountTenantId = target.HomeTenantId;
        log.Detail = actor.UserName;
        var configured = await ImpersonationSettings.ReadAsync(context).ConfigureAwait(false);
        if (!configured.TryGetValue(out var settings))
        {
            return configured.Error!;
        }

        await notices.ImpersonationAsync(target, actor, requested, settings, context).ConfigureAwait(false);
        var refreshToken = RefreshTokens.Generate();
        var session = new SysUserSession
        {
            Id = ids.NewId(),
            UserId = target.Id,
            ActiveTenantId = requested,
            RefreshTokenHash = RefreshTokens.Hash(refreshToken),
            IssuedAt = now,
            RefreshExpiresAt = now + settings.SessionLifetime,
            ImpersonatorUserId = actor.Id,
            ImpersonatorSessionId = origin.Id,
            IpAddress = SessionClient.AddressOf(context),
            ClientInfo = SessionClient.AgentOf(context),
        };
        SessionClient.Describe(session, context);
        var added = await sessions.AddAsync(session, context.CancellationToken).ConfigureAwait(false);
        if (added.IsFailure)
        {
            return added.Error!;
        }

        log.SessionId = session.Id;
        var roles = await accounts.RolesOfAsync(target.Id, requested, context.CancellationToken).ConfigureAwait(false);
        var active = await activators.StartAsync(session.Id, target.Id, requested, roles, context).ConfigureAwait(false);
        var access = issuer.Issue(target, session.Id, requested, active, actor.Id, notAfter: session.RefreshExpiresAt);
        return new TokenResponse(access.Token, access.ExpiresAt, refreshToken, session.RefreshExpiresAt, session.Id, requested, session.IsPasswordChangeRequired(), UserResponse.From(target), tenantName, actor.UserName);
    }

    /// <summary>
    /// 平台的系统账号就是超级管理员
    /// </summary>
    private static bool IsSuperAdministrator(SysUser user) => user.IsSystemAccount && string.Equals(user.HomeTenantId, TenantIds.Platform, StringComparison.Ordinal);

    private async Task<bool> IsMemberAsync(string tenantId, Guid userId, CancellationToken cancellationToken)
    {
        foreach (var resolver in memberships)
        {
            if (await resolver.IsMemberAsync(tenantId, userId, cancellationToken).ConfigureAwait(false))
            {
                return true;
            }
        }

        return false;
    }
}
