// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.ValueObjects;

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 权限裁决领域服务
/// </summary>
public sealed class PermissionDecisionDomainService : IPermissionDecisionDomainService
{
    /// <inheritdoc />
    public AuthorizationDecision Decide(string permissionCode, IEnumerable<PermissionGrantSnapshot> grants, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(permissionCode))
        {
            return AuthorizationDecision.Deny(string.Empty, "权限编码为空。");
        }

        var effectiveGrants = grants
            .Where(grant => grant.IsEnabled
                            && grant.PermissionCode.Equals(permissionCode, StringComparison.OrdinalIgnoreCase)
                            && grant.Period.IsActive(now))
            .OrderByDescending(grant => grant.Priority)
            .ThenBy(grant => grant.Source)
            .ToList();

        if (effectiveGrants.Count == 0)
        {
            return AuthorizationDecision.Deny(permissionCode, "未匹配有效授权。");
        }

        var userDeny = effectiveGrants.FirstOrDefault(grant =>
            grant.Source == AuthorizationGrantSource.User && grant.Action == PermissionAction.Deny);
        if (userDeny is not null)
        {
            return AuthorizationDecision.Deny(permissionCode, "用户直授拒绝优先。", userDeny.PermissionId);
        }

        var userGrant = effectiveGrants.FirstOrDefault(grant =>
            grant.Source == AuthorizationGrantSource.User && grant.Action == PermissionAction.Grant);
        if (userGrant is not null)
        {
            return AuthorizationDecision.Grant(permissionCode, "用户直授通过。", userGrant.PermissionId);
        }

        var roleGrant = effectiveGrants.FirstOrDefault(grant =>
            grant.Source == AuthorizationGrantSource.Role && grant.Action == PermissionAction.Grant);
        if (roleGrant is not null)
        {
            return AuthorizationDecision.Grant(permissionCode, "角色授权通过。", roleGrant.PermissionId);
        }

        var roleDeny = effectiveGrants.FirstOrDefault(grant =>
            grant.Source == AuthorizationGrantSource.Role && grant.Action == PermissionAction.Deny);
        if (roleDeny is not null)
        {
            return AuthorizationDecision.Deny(permissionCode, "角色授权拒绝。", roleDeny.PermissionId);
        }

        // 委派授权优先级低于用户直授和角色授权
        var delegationGrant = effectiveGrants.FirstOrDefault(grant =>
            grant.Source == AuthorizationGrantSource.Delegation && grant.Action == PermissionAction.Grant);
        if (delegationGrant is not null)
        {
            return AuthorizationDecision.Grant(permissionCode, "委派授权通过。", delegationGrant.PermissionId);
        }

        return AuthorizationDecision.Deny(permissionCode, "授权来源未命中裁决规则。");
    }
}
