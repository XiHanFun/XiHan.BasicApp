#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ISysPermissionRepository
// Guid:aa2b3c4d-5e6f-7890-abcd-ef1234567899
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/10/31 4:45:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Entities;
using XiHan.Framework.Domain.Repositories;

namespace XiHan.BasicApp.Rbac.Repositories.Abstractions;

/// <summary>
/// 系统权限仓储接口
/// </summary>
public interface ISysPermissionRepository : IRepositoryBase<SysPermission, RbacIdType>
{
    /// <summary>
    /// 根据权限编码获取权限
    /// </summary>
    /// <param name="permissionCode">权限编码</param>
    /// <returns></returns>
    Task<SysPermission?> GetByPermissionCodeAsync(string permissionCode);

    /// <summary>
    /// 检查权限编码是否存在
    /// </summary>
    /// <param name="permissionCode">权限编码</param>
    /// <param name="excludeId">排除的权限ID</param>
    /// <returns></returns>
    Task<bool> ExistsByPermissionCodeAsync(string permissionCode, RbacIdType? excludeId = null);

    /// <summary>
    /// 根据角色ID获取权限列表
    /// </summary>
    /// <param name="roleId">角色ID</param>
    /// <returns></returns>
    Task<List<SysPermission>> GetByRoleIdAsync(RbacIdType roleId);

    /// <summary>
    /// 根据用户ID获取权限列表
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns></returns>
    Task<List<SysPermission>> GetByUserIdAsync(RbacIdType userId);
}
