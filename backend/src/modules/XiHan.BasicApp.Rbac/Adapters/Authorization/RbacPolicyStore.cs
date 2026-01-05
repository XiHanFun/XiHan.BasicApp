#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:RbacPolicyStore
// Guid:c3d4e5f6-a7b8-9012-3456-789 0abcdef01
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/01/06 12:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.Framework.Authorization.Policies;

namespace XiHan.BasicApp.Rbac.Adapters.Authorization;

/// <summary>
/// RBAC 策略存储适配器
/// </summary>
/// <remarks>
/// 默认实现，可根据需要扩展
/// </remarks>
public class RbacPolicyStore : IPolicyStore
{
    private readonly Dictionary<string, PolicyDefinition> _policies = [];

    /// <summary>
    /// 构造函数
    /// </summary>
    public RbacPolicyStore()
    {
        // 初始化一些默认策略
        InitializeDefaultPolicies();
    }

    /// <summary>
    /// 获取所有策略
    /// </summary>
    public Task<IEnumerable<PolicyDefinition>> GetAllPoliciesAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IEnumerable<PolicyDefinition>>(_policies.Values);
    }

    /// <summary>
    /// 根据名称获取策略
    /// </summary>
    public Task<PolicyDefinition?> GetPolicyByNameAsync(string policyName, CancellationToken cancellationToken = default)
    {
        _policies.TryGetValue(policyName, out var policy);
        return Task.FromResult(policy);
    }

    /// <summary>
    /// 创建策略
    /// </summary>
    public Task CreatePolicyAsync(PolicyDefinition policy, CancellationToken cancellationToken = default)
    {
        _policies[policy.Name] = policy;
        return Task.CompletedTask;
    }

    /// <summary>
    /// 更新策略
    /// </summary>
    public Task UpdatePolicyAsync(PolicyDefinition policy, CancellationToken cancellationToken = default)
    {
        _policies[policy.Name] = policy;
        return Task.CompletedTask;
    }

    /// <summary>
    /// 删除策略
    /// </summary>
    public Task DeletePolicyAsync(string policyName, CancellationToken cancellationToken = default)
    {
        _policies.Remove(policyName);
        return Task.CompletedTask;
    }

    /// <summary>
    /// 初始化默认策略
    /// </summary>
    private void InitializeDefaultPolicies()
    {
        // 管理员策略
        _policies["AdminPolicy"] = new PolicyDefinition
        {
            Name = "AdminPolicy",
            DisplayName = "管理员策略",
            Description = "要求用户必须是管理员角色",
            RequiredRoles = ["Admin"],
            IsEnabled = true
        };

        // 用户管理策略
        _policies["UserManagementPolicy"] = new PolicyDefinition
        {
            Name = "UserManagementPolicy",
            DisplayName = "用户管理策略",
            Description = "要求用户拥有用户管理相关权限",
            RequiredPermissions = ["User.Create", "User.Update", "User.Delete"],
            IsEnabled = true
        };

        // 角色管理策略
        _policies["RoleManagementPolicy"] = new PolicyDefinition
        {
            Name = "RoleManagementPolicy",
            DisplayName = "角色管理策略",
            Description = "要求用户拥有角色管理相关权限",
            RequiredPermissions = ["Role.Create", "Role.Update", "Role.Delete"],
            IsEnabled = true
        };
    }
}
