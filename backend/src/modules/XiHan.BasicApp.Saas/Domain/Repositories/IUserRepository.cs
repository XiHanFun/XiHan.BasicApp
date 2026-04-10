#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IUserRepository
// Guid:fd3374ae-4884-43ed-80f4-da3b1c02802d
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/02/28 05:33:56
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.Framework.Domain.Repositories;

namespace XiHan.BasicApp.Saas.Domain.Repositories;

/// <summary>
/// 用户聚合仓储接口
/// </summary>
public interface IUserRepository : IAggregateRootRepository<SysUser, long>
{
    /// <summary>
    /// 根据用户名获取用户
    /// </summary>
    Task<SysUser?> GetByUserNameAsync(string userName, long? tenantId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据手机号获取用户
    /// </summary>
    Task<SysUser?> GetByPhoneAsync(string phone, long? tenantId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据邮箱获取用户
    /// </summary>
    Task<SysUser?> GetByEmailAsync(string email, long? tenantId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 校验用户名是否已存在
    /// </summary>
    Task<bool> IsUserNameExistsAsync(string userName, long? excludeUserId = null, long? tenantId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据用户ID获取安全状态
    /// </summary>
    Task<SysUserSecurity?> GetSecurityByUserIdAsync(long userId, long? tenantId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 保存用户安全状态（新增或更新）
    /// </summary>
    Task<SysUserSecurity> SaveSecurityAsync(SysUserSecurity security, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取用户角色关系
    /// </summary>
    Task<IReadOnlyList<SysUserRole>> GetUserRolesAsync(long userId, long? tenantId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量获取用户角色 ID 映射
    /// </summary>
    Task<IReadOnlyDictionary<long, IReadOnlyList<long>>> GetRoleIdsMapByUserIdsAsync(
        IReadOnlyCollection<long> userIds,
        long? tenantId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据角色ID获取关联用户ID列表
    /// </summary>
    Task<IReadOnlyCollection<long>> GetUserIdsByRoleIdAsync(
        long roleId,
        long? tenantId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取用户直授权限关系
    /// </summary>
    Task<IReadOnlyList<SysUserPermission>> GetUserPermissionsAsync(long userId, long? tenantId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取用户部门关系
    /// </summary>
    Task<IReadOnlyList<SysUserDepartment>> GetUserDepartmentsAsync(long userId, long? tenantId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 替换用户角色关系
    /// </summary>
    Task ReplaceUserRolesAsync(long userId, IReadOnlyCollection<long> roleIds, long? tenantId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 替换用户直授权限关系
    /// </summary>
    Task ReplaceUserPermissionsAsync(long userId, IReadOnlyCollection<long> permissionIds, long? tenantId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 替换用户部门关系
    /// </summary>
    Task ReplaceUserDepartmentsAsync(
        long userId,
        IReadOnlyCollection<long> departmentIds,
        long? mainDepartmentId = null,
        long? tenantId = null,
        CancellationToken cancellationToken = default);
}
