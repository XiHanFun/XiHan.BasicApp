// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Domain.Repositories;

/// <summary>
/// 角色层级仓储接口
/// </summary>
public interface IRoleHierarchyRepository : ISaasRepository<SysRoleHierarchy>
{
    /// <summary>
    /// 获取角色继承链中的祖先角色ID
    /// </summary>
    Task<IReadOnlyList<long>> GetAncestorIdsAsync(IEnumerable<long> roleIds, bool includeSelf, CancellationToken cancellationToken = default);
}
