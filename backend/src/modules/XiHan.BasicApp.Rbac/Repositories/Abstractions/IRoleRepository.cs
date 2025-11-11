#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IRoleRepository
// Guid:9a2b3c4d-5e6f-7890-abcd-ef1234567898
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/10/31 4:40:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Entities;
using XiHan.Framework.Domain.Repositories;

namespace XiHan.BasicApp.Rbac.Repositories.Abstractions;

/// <summary>
/// 角色仓储接口
/// </summary>
public interface IRoleRepository : IRepositoryBase<SysRole, RbacIdType>
{
    /// <summary>
    /// 根据角色编码获取角色
    /// </summary>
    /// <param name="roleCode">角色编码</param>
    /// <returns></returns>
    Task<SysRole?> GetByRoleCodeAsync(string roleCode);

    /// <summary>
    /// 检查角色编码是否存在
    /// </summary>
    /// <param name="roleCode">角色编码</param>
    /// <param name="excludeId">排除的角色ID</param>
    /// <returns></returns>
    Task<bool> ExistsByRoleCodeAsync(string roleCode, long? excludeId = null);

    /// <summary>
    /// 获取角色的菜单ID列表
    /// </summary>
    /// <param name="roleId">角色ID</param>
    /// <returns></returns>
    Task<List<long>> GetRoleMenuIdsAsync(long roleId);

    /// <summary>
    /// 获取角色的权限ID列表
    /// </summary>
    /// <param name="roleId">角色ID</param>
    /// <returns></returns>
    Task<List<long>> GetRolePermissionIdsAsync(long roleId);

    /// <summary>
    /// 获取角色的用户数量
    /// </summary>
    /// <param name="roleId">角色ID</param>
    /// <returns></returns>
    Task<int> GetRoleUserCountAsync(long roleId);

    /// <summary>
    /// 根据用户ID获取角色列表
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns></returns>
    Task<List<SysRole>> GetByUserIdAsync(long userId);
}

