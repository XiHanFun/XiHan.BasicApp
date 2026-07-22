// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Domain.Repositories;

/// <summary>
/// 菜单仓储接口
/// </summary>
public interface IMenuRepository : ISaasRepository<SysMenu>
{
    /// <summary>
    /// 根据父级ID获取子菜单列表
    /// </summary>
    Task<IReadOnlyList<SysMenu>> GetByParentIdAsync(long? parentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取完整菜单树（全部记录）
    /// </summary>
    Task<IReadOnlyList<SysMenu>> GetTreeAsync(CancellationToken cancellationToken = default);
}
