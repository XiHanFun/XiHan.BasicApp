#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ISysRoleHierarchyRepository
// Guid:5a2b3c4d-5e6f-7890-abcd-ef1234567804
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026-01-07 15:30:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Entities;
using XiHan.Framework.Domain.Repositories;

namespace XiHan.BasicApp.Rbac.Repositories.RoleHierarchies;

/// <summary>
/// 系统角色层次仓储接口
/// </summary>
public interface ISysRoleHierarchyRepository : IRepositoryBase<SysRoleHierarchy, long>
{
    /// <summary>
    /// 获取角色的所有父角色层次
    /// </summary>
    /// <param name="childRoleId">子角色ID</param>
    /// <param name="includingIndirect">是否包含间接父角色</param>
    /// <returns></returns>
    Task<List<SysRoleHierarchy>> GetParentHierarchiesAsync(long childRoleId, bool includingIndirect = true);

    /// <summary>
    /// 获取角色的所有子角色层次
    /// </summary>
    /// <param name="parentRoleId">父角色ID</param>
    /// <param name="includingIndirect">是否包含间接子角色</param>
    /// <returns></returns>
    Task<List<SysRoleHierarchy>> GetChildHierarchiesAsync(long parentRoleId, bool includingIndirect = true);

    /// <summary>
    /// 获取直接父角色ID列表
    /// </summary>
    /// <param name="childRoleId">子角色ID</param>
    /// <returns></returns>
    Task<List<long>> GetDirectParentRoleIdsAsync(long childRoleId);

    /// <summary>
    /// 获取直接子角色ID列表
    /// </summary>
    /// <param name="parentRoleId">父角色ID</param>
    /// <returns></returns>
    Task<List<long>> GetDirectChildRoleIdsAsync(long parentRoleId);

    /// <summary>
    /// 获取所有父角色ID列表（包括间接父角色）
    /// </summary>
    /// <param name="childRoleId">子角色ID</param>
    /// <returns></returns>
    Task<List<long>> GetAllParentRoleIdsAsync(long childRoleId);

    /// <summary>
    /// 获取所有子角色ID列表（包括间接子角色）
    /// </summary>
    /// <param name="parentRoleId">父角色ID</param>
    /// <returns></returns>
    Task<List<long>> GetAllChildRoleIdsAsync(long parentRoleId);

    /// <summary>
    /// 检查角色层次关系是否存在
    /// </summary>
    /// <param name="parentRoleId">父角色ID</param>
    /// <param name="childRoleId">子角色ID</param>
    /// <returns></returns>
    Task<bool> ExistsHierarchyAsync(long parentRoleId, long childRoleId);

    /// <summary>
    /// 添加角色层次关系
    /// </summary>
    /// <param name="parentRoleId">父角色ID</param>
    /// <param name="childRoleId">子角色ID</param>
    Task AddHierarchyAsync(long parentRoleId, long childRoleId);

    /// <summary>
    /// 移除角色层次关系
    /// </summary>
    /// <param name="parentRoleId">父角色ID</param>
    /// <param name="childRoleId">子角色ID</param>
    Task RemoveHierarchyAsync(long parentRoleId, long childRoleId);

    /// <summary>
    /// 检查是否会形成循环继承
    /// </summary>
    /// <param name="parentRoleId">父角色ID</param>
    /// <param name="childRoleId">子角色ID</param>
    /// <returns></returns>
    Task<bool> WouldCreateCycleAsync(long parentRoleId, long childRoleId);
}
