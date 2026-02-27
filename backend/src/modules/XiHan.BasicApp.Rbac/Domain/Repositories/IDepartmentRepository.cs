#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IDepartmentRepository
// Guid:5cf2ce0c-b1ae-499c-b0e7-58da4ff84559
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/02/28 05:34:42
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Domain.Entities;
using XiHan.Framework.Domain.Repositories;

namespace XiHan.BasicApp.Rbac.Domain.Repositories;

/// <summary>
/// 部门聚合仓储接口
/// </summary>
public interface IDepartmentRepository : IAggregateRootRepository<SysDepartment, long>
{
    /// <summary>
    /// 根据部门编码获取部门
    /// </summary>
    Task<SysDepartment?> GetByDepartmentCodeAsync(string departmentCode, long? tenantId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取下级部门
    /// </summary>
    Task<IReadOnlyList<SysDepartment>> GetChildrenAsync(long? parentId, long? tenantId = null, CancellationToken cancellationToken = default);
}
