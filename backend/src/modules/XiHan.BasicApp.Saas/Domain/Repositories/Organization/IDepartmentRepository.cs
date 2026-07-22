// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

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
