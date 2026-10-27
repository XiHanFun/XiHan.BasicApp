// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts;
using XiHan.BasicApp.Contracts.MultiTenancy;
using XiHan.BasicApp.Identity.Domain.Entities;
using XiHan.BasicApp.Identity.Domain.Services;
using XiHan.BasicApp.Identity.Infrastructure;
using XiHan.BasicApp.Identity.Operations.Auth;
using XiHan.BasicApp.Identity.Operations.Users;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Kernel.Security;
using XiHan.Framework.MultiTenancy.Abstractions.Tenants;
using XiHan.Framework.Persistence.Abstractions.Identifiers;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Identity.Operations.Impersonation;

/// <summary>
/// 发起模仿登录 <c>impersonation.start</c>：以目标用户的身份另开一个会话并签发令牌（<c>sub</c> 是目标、<c>act</c> 是发起人），客户端换令牌后整页重载。
/// 不是目标用户的一次登录：不写登录痕迹。发起人的会话保留，<c>impersonation.stop</c> 时回到它。
/// 范围：不填租户即发起人当前所在的上下文；租户主体只能在自己的租户里模仿；平台主体可指定租户（目标须是该租户的注册地用户或成员）或平台（目标须是平台账号）。不能模仿自己，不能在模仿态里再模仿。
/// </summary>
/// <param name="users">用户仓储。</param>
/// <param name="sessions">会话仓储。</param>
/// <param name="accounts">账号域服务。</param>
/// <param name="memberships">成员关系（Tenancy 实现）。</param>
/// <param name="issuer">令牌签发器。</param>
/// <param name="options">身份配置。</param>
/// <param name="ids">标识生成器。</param>
/// <param name="clock">时间源。</param>
/// <param name="tenants">框架租户目录。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = IdentityPermissions.ImpersonationStart)]
public sealed class StartImpersonation(
    IRepository<SysUser, Guid> users,
    IRepository<SysUserSession, Guid> sessions,
    UserAccountService accounts,
    IEnumerable<ITenantMembershipResolver> memberships,
    AccessTokenIssuer issuer,
    IdentityOptions options,
    IIdGenerator<Guid> ids,
    TimeProvider clock,
    ITenantStore tenants) : IOperation<StartImpersonationRequest, TokenResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "impersonation.start";

    /// <inheritdoc />
    public async ValueTask<Result<TokenResponse>> HandleAsync(StartImpersonationRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var now = clock.GetUtcNow();
        var security = context.GetSecurity();
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
            var directory = await tenants.FindAsync(requested, context.CancellationToken).ConfigureAwait(false);
            if (!directory.TryGetValue(out var info) || !info.IsEnabled)
            {
                return Error.Denied(IdentityErrors.TenantUnavailable, IdentityMessages.TenantUnavailable, MessageArguments.Of(("tenantId", requested)));
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

        var refreshToken = RefreshTokens.Generate();
        var session = new SysUserSession
        {
            Id = ids.NewId(),
            UserId = target.Id,
            ActiveTenantId = requested,
            RefreshTokenHash = RefreshTokens.Hash(refreshToken),
            IssuedAt = now,
            RefreshExpiresAt = now + options.RefreshTokenLifetime,
            ImpersonatorUserId = actor.Id,
            ImpersonatorSessionId = origin.Id,
        };
        var added = await sessions.AddAsync(session, context.CancellationToken).ConfigureAwait(false);
        if (added.IsFailure)
        {
            return added.Error!;
        }

        var securityResult = await accounts.SecurityOfAsync(target.Id, context.CancellationToken).ConfigureAwait(false);
        if (!securityResult.TryGetValue(out var targetSecurity))
        {
            return securityResult.Error!;
        }

        var roles = await accounts.RolesOfAsync(target.Id, requested, context.CancellationToken).ConfigureAwait(false);
        var access = issuer.Issue(target, session.Id, requested, roles, actor.Id);
        return new TokenResponse(access.Token, access.ExpiresAt, refreshToken, session.RefreshExpiresAt, session.Id, requested, targetSecurity.MustChangePassword, UserResponse.From(target), tenantName, actor.UserName);
    }

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
