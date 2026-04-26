#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IDepartmentRepository
// Guid:9e6bd7a0-83c1-4764-bc3e-4e8cf00ba336
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/26 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Domain.Repositories;

/// <summary>
/// 部门仓储接口
/// </summary>
public interface IDepartmentRepository : ISaasAggregateRepository<SysDepartment>
{
    /// <summary>
    /// 根据当前租户和部门编码获取部门
    /// </summary>
    Task<SysDepartment?> GetByCodeAsync(string departmentCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取当前租户下的子部门
    /// </summary>
    Task<IReadOnlyList<SysDepartment>> GetChildrenAsync(long? parentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 当前租户下是否存在子部门
    /// </summary>
    Task<bool> HasChildrenAsync(long departmentId, CancellationToken cancellationToken = default);
}
