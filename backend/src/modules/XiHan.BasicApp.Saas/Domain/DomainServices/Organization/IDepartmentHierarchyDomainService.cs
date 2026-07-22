// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 部门层级领域服务
/// </summary>
/// <remarks>
/// 职责：部门树操作（移动节点、环路检测、闭包表重建）
/// 任何部门树结构变更操作都必须通过此服务以保证闭包表一致性
/// </remarks>
public interface IDepartmentHierarchyDomainService
{
    /// <summary>
    /// 检测部门移动是否会形成环路
    /// </summary>
    /// <param name="departmentId">被移动部门ID</param>
    /// <param name="newParentId">新父级部门ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否存在环路</returns>
    Task<bool> WouldCreateCycleAsync(long departmentId, long? newParentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取部门的所有后代ID（含自身）
    /// </summary>
    /// <param name="departmentId">部门ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>所有后代部门ID集合</returns>
    Task<IReadOnlyList<long>> GetDescendantIdsAsync(long departmentId, CancellationToken cancellationToken = default);
}
