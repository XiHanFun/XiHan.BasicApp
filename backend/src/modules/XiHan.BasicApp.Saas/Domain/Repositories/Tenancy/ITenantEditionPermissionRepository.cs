// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Domain.Repositories;

/// <summary>
/// 租户版本权限仓储接口
/// </summary>
public interface ITenantEditionPermissionRepository : ISaasRepository<SysTenantEditionPermission>
{
    /// <summary>
    /// 根据版本ID获取权限映射列表
    /// </summary>
    Task<IReadOnlyList<SysTenantEditionPermission>> GetByEditionIdAsync(long editionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 全量替换版本权限映射（先删后插）
    /// </summary>
    Task ReplaceByEditionIdAsync(long editionId, IEnumerable<SysTenantEditionPermission> items, CancellationToken cancellationToken = default);
}
