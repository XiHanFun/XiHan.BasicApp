#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:RbacPolicyEvaluator
// Guid:d4e5f6a7-b8c9-0123-4567-890abcdef012
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/01/06 12:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.Framework.Authorization.Permissions;
using XiHan.Framework.Authorization.Policies;
using XiHan.Framework.Authorization.Roles;

namespace XiHan.BasicApp.Rbac.Adapters.Authorization;

/// <summary>
/// RBAC 策略评估器
/// </summary>
public class RbacPolicyEvaluator : IPolicyEvaluator
{
    private readonly IPolicyStore _policyStore;
    private readonly IPermissionChecker _permissionChecker;
    private readonly IRoleStore _roleStore;

    /// <summary>
    /// 构造函数
    /// </summary>
    public RbacPolicyEvaluator(
        IPolicyStore policyStore,
        IPermissionChecker permissionChecker,
        IRoleStore roleStore)
    {
        _policyStore = policyStore;
        _permissionChecker = permissionChecker;
        _roleStore = roleStore;
    }

    /// <summary>
    /// 评估策略
    /// </summary>
    public async Task<PolicyEvaluationResult> EvaluateAsync(string userId, string policyName, object? resource = null, CancellationToken cancellationToken = default)
    {
        var policy = await _policyStore.GetPolicyByNameAsync(policyName, cancellationToken);
        if (policy == null)
        {
            return PolicyEvaluationResult.Failure($"策略不存在: {policyName}");
        }

        if (!policy.IsEnabled)
        {
            return PolicyEvaluationResult.Failure($"策略已禁用: {policyName}");
        }

        var failedRequirements = new List<string>();

        // 检查角色要求（任意一个即可）
        if (policy.RequiredRoles.Count > 0)
        {
            var hasRole = false;
            foreach (var roleName in policy.RequiredRoles)
            {
                if (await _roleStore.IsInRoleAsync(userId, roleName, cancellationToken))
                {
                    hasRole = true;
                    break;
                }
            }

            if (!hasRole)
            {
                failedRequirements.AddRange(policy.RequiredRoles);
            }
        }

        // 检查权限要求（所有都必须有）
        if (policy.RequiredPermissions.Count > 0)
        {
            foreach (var permissionName in policy.RequiredPermissions)
            {
                if (!await _permissionChecker.IsGrantedAsync(userId, permissionName, cancellationToken))
                {
                    failedRequirements.Add(permissionName);
                }
            }
        }

        // 检查声明要求
        // 注意：这里需要根据实际情况实现声明检查，暂时跳过

        // 检查自定义要求
        if (policy.CustomRequirements.Count > 0)
        {
            var context = await BuildAuthorizationContextAsync(userId, policyName, resource, cancellationToken);

            foreach (var requirement in policy.CustomRequirements)
            {
                var result = await requirement.EvaluateAsync(context);
                if (!result)
                {
                    failedRequirements.Add(requirement.Name);
                }
            }
        }

        // 返回结果
        if (failedRequirements.Count > 0)
        {
            return PolicyEvaluationResult.Failure("策略评估失败", failedRequirements);
        }

        return PolicyEvaluationResult.Success();
    }

    /// <summary>
    /// 评估多个策略（所有策略都必须通过）
    /// </summary>
    public async Task<PolicyEvaluationResult> EvaluateAllAsync(string userId, IEnumerable<string> policyNames, object? resource = null, CancellationToken cancellationToken = default)
    {
        var policies = policyNames.ToList();
        var allFailedRequirements = new List<string>();

        foreach (var policyName in policies)
        {
            var result = await EvaluateAsync(userId, policyName, resource, cancellationToken);
            if (!result.Succeeded)
            {
                allFailedRequirements.AddRange(result.FailedRequirements);
            }
        }

        if (allFailedRequirements.Count > 0)
        {
            return PolicyEvaluationResult.Failure("部分策略评估失败", allFailedRequirements);
        }

        return PolicyEvaluationResult.Success();
    }

    /// <summary>
    /// 评估多个策略（任意一个策略通过即可）
    /// </summary>
    public async Task<PolicyEvaluationResult> EvaluateAnyAsync(string userId, IEnumerable<string> policyNames, object? resource = null, CancellationToken cancellationToken = default)
    {
        var policies = policyNames.ToList();
        var allFailedRequirements = new List<string>();

        foreach (var policyName in policies)
        {
            var result = await EvaluateAsync(userId, policyName, resource, cancellationToken);
            if (result.Succeeded)
            {
                return PolicyEvaluationResult.Success();
            }
            allFailedRequirements.AddRange(result.FailedRequirements);
        }

        return PolicyEvaluationResult.Failure("所有策略评估均失败", allFailedRequirements);
    }

    /// <summary>
    /// 构建授权上下文
    /// </summary>
    private async Task<AuthorizationContext> BuildAuthorizationContextAsync(string userId, string policyName, object? resource, CancellationToken cancellationToken)
    {
        var userRoles = await _roleStore.GetUserRolesAsync(userId, cancellationToken);
        var userPermissions = await _permissionChecker.GetGrantedPermissionsAsync(userId, cancellationToken);

        return new AuthorizationContext
        {
            UserId = userId,
            UserRoles = [.. userRoles.Select(r => r.Name)],
            UserPermissions = [.. userPermissions],
            Resource = resource,
            PolicyName = policyName
        };
    }
}
