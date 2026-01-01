#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ISysRoleRepository
// Guid:9a2b3c4d-5e6f-7890-abcd-ef1234567898
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/10/31 4:40:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.Framework.Domain.Repositories;

namespace XiHan.BasicApp.Rbac.Repositories.Roles;

/// <summary>
/// 系统角色仓储接口
/// </summary>
public interface ISysRoleRepository : IRepositoryBase<SysRole, XiHanBasicAppIdType>
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
    Task<bool> ExistsByRoleCodeAsync(string roleCode, XiHanBasicAppIdType? excludeId = null);

    /// <summary>
    /// 获取角色的菜单ID列表
    /// </summary>
    /// <param name="roleId">角色ID</param>
    /// <returns></returns>
    Task<List<XiHanBasicAppIdType>> GetRoleMenuIdsAsync(XiHanBasicAppIdType roleId);

    /// <summary>
    /// 获取角色的权限ID列表
    /// </summary>
    /// <param name="roleId">角色ID</param>
    /// <returns></returns>
    Task<List<XiHanBasicAppIdType>> GetRolePermissionIdsAsync(XiHanBasicAppIdType roleId);

    /// <summary>
    /// 获取角色的用户数量
    /// </summary>
    /// <param name="roleId">角色ID</param>
    /// <returns></returns>
    Task<int> GetRoleUserCountAsync(XiHanBasicAppIdType roleId);

    /// <summary>
    /// 根据用户ID获取角色列表
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns></returns>
    Task<List<SysRole>> GetByUserIdAsync(XiHanBasicAppIdType userId);

    /// <summary>
    /// 获取角色的所有父角色ID（递归查询继承链）
    /// </summary>
    /// <param name="roleId">角色ID</param>
    /// <returns>父角色ID列表（包括所有祖先角色）</returns>
    Task<List<XiHanBasicAppIdType>> GetParentRoleIdsAsync(XiHanBasicAppIdType roleId);

    /// <summary>
    /// 获取角色的所有子角色ID（递归查询继承链）
    /// </summary>
    /// <param name="roleId">角色ID</param>
    /// <returns>子角色ID列表（包括所有后代角色）</returns>
    Task<List<XiHanBasicAppIdType>> GetChildRoleIdsAsync(XiHanBasicAppIdType roleId);

    /// <summary>
    /// 获取角色的所有父角色（递归查询继承链）
    /// </summary>
    /// <param name="roleId">角色ID</param>
    /// <returns>父角色列表（包括所有祖先角色）</returns>
    Task<List<SysRole>> GetParentRolesAsync(XiHanBasicAppIdType roleId);

    /// <summary>
    /// 获取角色的所有子角色（递归查询继承链）
    /// </summary>
    /// <param name="roleId">角色ID</param>
    /// <returns>子角色列表（包括所有后代角色）</returns>
    Task<List<SysRole>> GetChildRolesAsync(XiHanBasicAppIdType roleId);

    /// <summary>
    /// 检查是否会形成循环继承
    /// </summary>
    /// <param name="roleId">当前角色ID</param>
    /// <param name="parentRoleId">要设置的父角色ID</param>
    /// <returns>是否会形成循环</returns>
    Task<bool> WouldCreateCycleAsync(XiHanBasicAppIdType roleId, XiHanBasicAppIdType parentRoleId);

    /// <summary>
    /// 获取角色树（包含子角色）
    /// </summary>
    /// <param name="parentRoleId">父角色ID，null表示获取根角色</param>
    /// <returns>角色树</returns>
    Task<List<SysRole>> GetRoleTreeAsync(XiHanBasicAppIdType? parentRoleId = null);
}
