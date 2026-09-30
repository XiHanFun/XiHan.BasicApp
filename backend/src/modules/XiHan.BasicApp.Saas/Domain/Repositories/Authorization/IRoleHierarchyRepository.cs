// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Domain.Repositories;

/// <summary>
/// 角色层级仓储接口
/// </summary>
/// <remarks>
/// 表里只存直接继承边（上级 → 下级），间接继承、深度与路径都由 <c>RoleInheritanceGraph</c> 按需推出。
/// </remarks>
public interface IRoleHierarchyRepository : ISaasRepository<SysRoleHierarchy>
{
    /// <summary>
    /// 当前上下文可见的全部直接继承边（租户里含平台全局角色之间的边）
    /// </summary>
    Task<IReadOnlyList<SysRoleHierarchy>> GetEdgesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 跨租户获取有角色直接继承了给定角色的租户（不含平台）
    /// </summary>
    /// <remarks>平台调整全局角色的继承后，据此找出继承链随之变化、要复核职责分离的租户。</remarks>
    Task<IReadOnlyList<long>> GetTenantIdsInheritingAsync(IReadOnlyCollection<long> parentRoleIds, CancellationToken cancellationToken = default);
}
