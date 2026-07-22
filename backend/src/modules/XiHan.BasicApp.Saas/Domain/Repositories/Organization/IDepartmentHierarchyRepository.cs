// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Domain.Repositories;

/// <summary>
/// 部门层级仓储接口
/// </summary>
public interface IDepartmentHierarchyRepository : ISaasRepository<SysDepartmentHierarchy>
{
    /// <summary>
    /// 获取后代部门ID
    /// </summary>
    Task<IReadOnlyList<long>> GetDescendantIdsAsync(long departmentId, bool includeSelf, CancellationToken cancellationToken = default);
}
