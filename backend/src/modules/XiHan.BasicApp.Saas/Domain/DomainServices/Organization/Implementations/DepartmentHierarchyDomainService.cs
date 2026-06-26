#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:DepartmentHierarchyDomainService
// Guid:f6a7b8c9-6666-7777-8888-999900001111
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/06 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Repositories;

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 部门层级领域服务实现
/// </summary>
public sealed class DepartmentHierarchyDomainService
    : IDepartmentHierarchyDomainService
{
    private readonly IDepartmentHierarchyRepository _departmentHierarchyRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public DepartmentHierarchyDomainService(IDepartmentHierarchyRepository departmentHierarchyRepository)
    {
        _departmentHierarchyRepository = departmentHierarchyRepository;
    }

    /// <inheritdoc />
    public async Task<bool> WouldCreateCycleAsync(long departmentId, long? newParentId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!newParentId.HasValue)
        {
            // 移动到根节点，不可能形成环路
            return false;
        }

        if (departmentId == newParentId.Value)
        {
            return true;
        }

        // 获取目标部门的所有后代，若 newParentId 在后代中则形成环路
        var descendantIds = await _departmentHierarchyRepository.GetDescendantIdsAsync(departmentId, includeSelf: false, cancellationToken);
        return descendantIds.Contains(newParentId.Value);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<long>> GetDescendantIdsAsync(long departmentId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await _departmentHierarchyRepository.GetDescendantIdsAsync(departmentId, includeSelf: true, cancellationToken);
    }
}
