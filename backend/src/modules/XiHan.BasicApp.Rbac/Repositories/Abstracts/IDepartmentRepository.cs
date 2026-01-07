#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IDepartmentRepository
// Guid:c3d4e5f6-a7b8-4c5d-9e0f-1a2b3c4d5e6f
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/1/7 0:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Entities;
using XiHan.Framework.Domain.Repositories;

namespace XiHan.BasicApp.Rbac.Repositories.Abstracts;

/// <summary>
/// 部门仓储接口
/// </summary>
public interface IDepartmentRepository : IAggregateRootRepository<SysDepartment, long>
{
    /// <summary>
    /// 根据部门编码查询部门
    /// </summary>
    /// <param name="departmentCode">部门编码</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>部门实体</returns>
    Task<SysDepartment?> GetByDepartmentCodeAsync(string departmentCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查部门编码是否存在
    /// </summary>
    /// <param name="departmentCode">部门编码</param>
    /// <param name="excludeDepartmentId">排除的部门ID（用于更新时检查）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否存在</returns>
    Task<bool> ExistsByDepartmentCodeAsync(string departmentCode, long? excludeDepartmentId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取子部门列表
    /// </summary>
    /// <param name="parentId">父部门ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>子部门列表</returns>
    Task<List<SysDepartment>> GetChildrenAsync(long parentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 递归获取所有子部门ID（包括孙部门）
    /// </summary>
    /// <param name="parentId">父部门ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>所有子部门ID列表</returns>
    Task<List<long>> GetAllChildIdsAsync(long parentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取部门的父部门链（从根到当前部门）
    /// </summary>
    /// <param name="departmentId">部门ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>父部门链列表</returns>
    Task<List<SysDepartment>> GetParentChainAsync(long departmentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取租户下的所有部门
    /// </summary>
    /// <param name="tenantId">租户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>部门列表</returns>
    Task<List<SysDepartment>> GetByTenantIdAsync(long tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取部门的所有用户
    /// </summary>
    /// <param name="departmentId">部门ID</param>
    /// <param name="includeChildren">是否包含子部门的用户</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户列表</returns>
    Task<List<SysUser>> GetUsersAsync(long departmentId, bool includeChildren = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查部门是否有子部门
    /// </summary>
    /// <param name="departmentId">部门ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否有子部门</returns>
    Task<bool> HasChildrenAsync(long departmentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查部门是否有用户
    /// </summary>
    /// <param name="departmentId">部门ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否有用户</returns>
    Task<bool> HasUsersAsync(long departmentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查设置父部门是否会形成循环引用
    /// </summary>
    /// <param name="departmentId">部门ID</param>
    /// <param name="parentId">父部门ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否会形成循环</returns>
    Task<bool> WouldCreateCycleAsync(long departmentId, long parentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据父部门ID获取子部门列表（仅直接子部门）
    /// </summary>
    /// <param name="parentId">父部门ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>子部门列表</returns>
    Task<List<SysDepartment>> GetByParentIdAsync(long parentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取部门的用户数量
    /// </summary>
    /// <param name="departmentId">部门ID</param>
    /// <param name="includeChildren">是否包含子部门</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户数量</returns>
    Task<int> GetDepartmentUserCountAsync(long departmentId, bool includeChildren = false, CancellationToken cancellationToken = default);
}
