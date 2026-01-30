#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ISysDepartmentRepository
// Guid:e5f6a7b8-c9d0-1234-5678-901234ef0123
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/1/30 0:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Entities;
using XiHan.Framework.Domain.Repositories;

namespace XiHan.BasicApp.Rbac.Repositories.Abstracts;

/// <summary>
/// 部门聚合仓储接口
/// </summary>
/// <remarks>
/// 聚合范围：SysDepartment + SysDepartmentHierarchy
/// </remarks>
public interface ISysDepartmentRepository : IAggregateRootRepository<SysDepartment, long>
{
    /// <summary>
    /// 根据部门编码获取部门
    /// </summary>
    Task<SysDepartment?> GetByDeptCodeAsync(string deptCode, long? tenantId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取用户所属部门
    /// </summary>
    Task<List<SysDepartment>> GetByUserIdAsync(long userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取子部门
    /// </summary>
    Task<List<SysDepartment>> GetChildrenAsync(long parentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取部门树
    /// </summary>
    Task<List<SysDepartment>> GetDepartmentTreeAsync(long? tenantId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量获取部门
    /// </summary>
    Task<List<SysDepartment>> GetByIdsAsync(IEnumerable<long> deptIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查部门编码是否存在
    /// </summary>
    Task<bool> ExistsByDeptCodeAsync(string deptCode, long? excludeDeptId = null, long? tenantId = null, CancellationToken cancellationToken = default);
}
