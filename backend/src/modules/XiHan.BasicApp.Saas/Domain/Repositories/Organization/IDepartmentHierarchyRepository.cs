#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IDepartmentHierarchyRepository
// Guid:4a9f153b-f3d4-48ed-b4c9-9cb837f38c83
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/26 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

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
