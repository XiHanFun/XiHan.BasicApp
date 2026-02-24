#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ISysDepartmentRepository
// Guid:c9d0e1f2-a3b4-5678-9abc-def123456789
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/01/31 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Entities;
using XiHan.Framework.Domain.Repositories;

namespace XiHan.BasicApp.Rbac.Repositories;

/// <summary>
/// 系统部门仓储接口
/// </summary>
public interface ISysDepartmentRepository : IAggregateRootRepository<SysDepartment, long>
{
    /// <summary>
    /// 根据部门编码获取部门
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
    Task<bool> IsDepartmentCodeExistsAsync(string departmentCode, long? excludeDepartmentId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取用户所属的部门列表
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>部门列表</returns>
    Task<List<SysDepartment>> GetDepartmentsByUserIdAsync(long userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取父级部门下的子部门列表
    /// </summary>
    /// <param name="parentId">父级部门ID（null 表示获取顶级部门）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>部门列表</returns>
    Task<List<SysDepartment>> GetChildrenDepartmentsAsync(long? parentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取租户下的部门树
    /// </summary>
    /// <param name="tenantId">租户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>部门列表</returns>
    Task<List<SysDepartment>> GetDepartmentTreeByTenantAsync(long tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取部门的所有祖先部门
    /// </summary>
    /// <param name="departmentId">部门ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>祖先部门列表</returns>
    Task<List<SysDepartment>> GetAncestorDepartmentsAsync(long departmentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 保存部门
    /// </summary>
    /// <param name="department">部门实体</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>保存的部门实体</returns>
    Task<SysDepartment> SaveAsync(SysDepartment department, CancellationToken cancellationToken = default);

    /// <summary>
    /// 启用部门
    /// </summary>
    /// <param name="departmentId">部门ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task EnableDepartmentAsync(long departmentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 禁用部门
    /// </summary>
    /// <param name="departmentId">部门ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task DisableDepartmentAsync(long departmentId, CancellationToken cancellationToken = default);
}
