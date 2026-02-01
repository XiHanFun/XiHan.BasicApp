#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IDepartmentHierarchyService
// Guid:bcdef123-4567-890a-bcde-f12345678901
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/01/31 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Entities;
using XiHan.Framework.Domain.Services.Abstracts;

namespace XiHan.BasicApp.Rbac.Domain.Services;

/// <summary>
/// 部门层级领域服务接口
/// </summary>
public interface IDepartmentHierarchyService : IDomainService
{
    /// <summary>
    /// 构建部门树
    /// </summary>
    /// <param name="tenantId">租户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>部门树（按层级结构组织）</returns>
    Task<List<SysDepartment>> BuildDepartmentTreeAsync(long tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取部门的所有子部门（递归）
    /// </summary>
    /// <param name="departmentId">部门ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>子部门列表</returns>
    Task<List<SysDepartment>> GetDescendantDepartmentsAsync(long departmentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取部门的所有祖先部门
    /// </summary>
    /// <param name="departmentId">部门ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>祖先部门列表（从根到父部门）</returns>
    Task<List<SysDepartment>> GetAncestorDepartmentsAsync(long departmentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查部门是否为另一个部门的子部门
    /// </summary>
    /// <param name="departmentId">部门ID</param>
    /// <param name="ancestorId">祖先部门ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否为子部门</returns>
    Task<bool> IsDescendantOfAsync(long departmentId, long ancestorId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 验证部门层级关系是否有效
    /// </summary>
    /// <param name="departmentId">部门ID</param>
    /// <param name="parentId">父部门ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否有效（防止循环引用）</returns>
    Task<bool> ValidateDepartmentHierarchyAsync(long departmentId, long? parentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取部门路径字符串
    /// </summary>
    /// <param name="departmentId">部门ID</param>
    /// <param name="separator">分隔符</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>部门路径（如：公司/研发部/前端组）</returns>
    Task<string> GetDepartmentPathAsync(long departmentId, string separator = "/", CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量为用户分配部门
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="departmentIds">部门ID列表</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task AssignDepartmentsToUserAsync(long userId, IEnumerable<long> departmentIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量移除用户的部门
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="departmentIds">部门ID列表</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task RemoveDepartmentsFromUserAsync(long userId, IEnumerable<long> departmentIds, CancellationToken cancellationToken = default);
}
