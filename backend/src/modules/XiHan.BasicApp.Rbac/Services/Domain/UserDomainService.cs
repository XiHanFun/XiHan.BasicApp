#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:UserDomainService
// Guid:a1b2c3d4-e5f6-4a5b-8c9d-0e1f2a3b4c5d
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/1/7 0:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Repositories.Abstracts;
using XiHan.Framework.Authentication.Password;
using XiHan.Framework.Domain.Services;

namespace XiHan.BasicApp.Rbac.Services.Domain;

/// <summary>
/// 用户领域服务
/// 处理用户相关的跨聚合业务逻辑
/// </summary>
/// <remarks>
/// 包含从 UserManager 迁移的验证逻辑
/// </remarks>
public class UserDomainService : DomainService
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IPermissionRepository _permissionRepository;
    private readonly ITenantRepository _tenantRepository;
    private readonly IPasswordHasher _passwordHasher;

    /// <summary>
    /// 构造函数
    /// </summary>
    public UserDomainService(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IPermissionRepository permissionRepository,
        ITenantRepository tenantRepository,
        IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _permissionRepository = permissionRepository;
        _tenantRepository = tenantRepository;
        _passwordHasher = passwordHasher;
    }

    /// <summary>
    /// 分配角色给用户（跨聚合操作）
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="roleIds">角色ID列表</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    public async Task<bool> AssignRolesToUserAsync(long userId, List<long> roleIds, CancellationToken cancellationToken = default)
    {
        LogDomainOperation(nameof(AssignRolesToUserAsync), new { userId, roleIds });

        // 验证用户存在
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            throw new KeyNotFoundException($"用户 {userId} 不存在");
        }

        // 验证角色存在
        var roles = await _roleRepository.GetByIdsAsync(roleIds, cancellationToken);
        if (roles.Count != roleIds.Count)
        {
            throw new InvalidOperationException("部分角色不存在");
        }

        // 业务逻辑：检查角色是否可分配
        foreach (var role in roles)
        {
            if (role.Status != Enums.YesOrNo.Yes)
            {
                throw new InvalidOperationException($"角色 {role.RoleName} 已禁用，无法分配");
            }
        }

        // 注意：实际的关系维护在 Application Service 层
        Logger.LogInformation("用户 {UserId} 角色分配验证通过，待分配角色: {RoleCount}", userId, roleIds.Count);
        return true;
    }

    /// <summary>
    /// 移除用户的角色
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="roleIds">角色ID列表</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    public async Task<bool> RemoveRolesFromUserAsync(long userId, List<long> roleIds, CancellationToken cancellationToken = default)
    {
        LogDomainOperation(nameof(RemoveRolesFromUserAsync), new { userId, roleIds });

        // 验证用户存在
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            throw new KeyNotFoundException($"用户 {userId} 不存在");
        }

        Logger.LogInformation("用户 {UserId} 角色移除验证通过，待移除角色: {RoleCount}", userId, roleIds.Count);
        return true;
    }

    /// <summary>
    /// 授予用户直接权限（跨聚合操作）
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="permissionIds">权限ID列表</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    public async Task<bool> GrantPermissionsToUserAsync(long userId, List<long> permissionIds, CancellationToken cancellationToken = default)
    {
        LogDomainOperation(nameof(GrantPermissionsToUserAsync), new { userId, permissionIds });

        // 验证用户存在
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            throw new KeyNotFoundException($"用户 {userId} 不存在");
        }

        // 验证权限存在
        var permissions = await _permissionRepository.GetByIdsAsync(permissionIds, cancellationToken);
        if (permissions.Count != permissionIds.Count)
        {
            throw new InvalidOperationException("部分权限不存在");
        }

        Logger.LogInformation("用户 {UserId} 权限授予验证通过，待授予权限: {PermissionCount}", userId, permissionIds.Count);
        return true;
    }

    /// <summary>
    /// 检查用户名唯一性
    /// </summary>
    /// <param name="userName">用户名</param>
    /// <param name="excludeUserId">排除的用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否唯一</returns>
    public async Task<bool> IsUserNameUniqueAsync(string userName, long? excludeUserId = null, CancellationToken cancellationToken = default)
    {
        var exists = await _userRepository.ExistsByUserNameAsync(userName, excludeUserId, cancellationToken);
        return !exists;
    }

    /// <summary>
    /// 检查邮箱唯一性
    /// </summary>
    /// <param name="email">邮箱</param>
    /// <param name="excludeUserId">排除的用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否唯一</returns>
    public async Task<bool> IsEmailUniqueAsync(string email, long? excludeUserId = null, CancellationToken cancellationToken = default)
    {
        var exists = await _userRepository.ExistsByEmailAsync(email, excludeUserId, cancellationToken);
        return !exists;
    }

    /// <summary>
    /// 检查手机号唯一性
    /// </summary>
    /// <param name="phone">手机号</param>
    /// <param name="excludeUserId">排除的用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否唯一</returns>
    public async Task<bool> IsPhoneUniqueAsync(string phone, long? excludeUserId = null, CancellationToken cancellationToken = default)
    {
        var exists = await _userRepository.ExistsByPhoneAsync(phone, excludeUserId, cancellationToken);
        return !exists;
    }

    /// <summary>
    /// 验证租户限制
    /// </summary>
    /// <param name="tenantId">租户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否通过验证</returns>
    public async Task<bool> ValidateTenantLimitAsync(long tenantId, CancellationToken cancellationToken = default)
    {
        var tenant = await _tenantRepository.GetByIdAsync(tenantId, cancellationToken);
        if (tenant == null)
        {
            throw new KeyNotFoundException($"租户 {tenantId} 不存在");
        }

        // 检查租户状态
        if (tenant.Status != Enums.YesOrNo.Yes)
        {
            throw new InvalidOperationException("租户已禁用");
        }

        // 检查用户数量限制
        var userCount = await _tenantRepository.GetUserCountAsync(tenantId, cancellationToken);
        if (tenant.UserLimit.HasValue && userCount >= tenant.UserLimit.Value)
        {
            throw new InvalidOperationException($"租户用户数量已达上限 ({tenant.UserLimit.Value})");
        }

        return true;
    }

    /// <summary>
    /// 验证密码
    /// </summary>
    /// <param name="user">用户</param>
    /// <param name="password">密码</param>
    /// <returns>密码是否正确</returns>
    public bool VerifyPassword(SysUser user, string password)
    {
        return _passwordHasher.VerifyPassword(user.Password, password);
    }

    /// <summary>
    /// 加密密码
    /// </summary>
    /// <param name="password">明文密码</param>
    /// <returns>加密后的密码</returns>
    public string HashPassword(string password)
    {
        return _passwordHasher.HashPassword(password);
    }

    /// <summary>
    /// 检查密码是否需要重新哈希
    /// </summary>
    /// <param name="user">用户</param>
    /// <returns>是否需要重新哈希</returns>
    public bool NeedsPasswordRehash(SysUser user)
    {
        return _passwordHasher.NeedsRehash(user.Password);
    }

    /// <summary>
    /// 验证用户状态是否有效（活跃且未删除）
    /// </summary>
    /// <param name="user">用户</param>
    /// <returns>是否活跃</returns>
    public bool IsUserActive(SysUser user)
    {
        return user.Status == Enums.YesOrNo.Yes && !user.IsDeleted;
    }

    /// <summary>
    /// 检查用户是否可以删除
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否可以删除</returns>
    public async Task<bool> CanDeleteAsync(long userId, CancellationToken cancellationToken = default)
    {
        LogDomainOperation(nameof(CanDeleteAsync), new { userId });

        // 检查用户是否存在
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            throw new KeyNotFoundException($"用户 {userId} 不存在");
        }

        // 检查用户是否已被删除
        if (user.IsDeleted)
        {
            throw new InvalidOperationException("用户已被删除");
        }

        // TODO: 可以添加更多业务规则检查
        // - 检查用户是否有未完成的任务
        // - 检查用户是否有关联的订单
        // - 检查用户是否是系统管理员

        Logger.LogInformation("用户 {UserId} 可以删除", userId);
        return true;
    }
}
