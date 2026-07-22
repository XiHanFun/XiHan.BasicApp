// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Domain.Repositories;

/// <summary>
/// 权限仓储接口
/// </summary>
public interface IPermissionRepository : ISaasAggregateRepository<SysPermission>
{
    /// <summary>
    /// 根据当前租户和权限编码获取权限
    /// </summary>
    Task<SysPermission?> GetByCodeAsync(string permissionCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据权限编码集合获取权限
    /// </summary>
    Task<IReadOnlyList<SysPermission>> GetByCodesAsync(IEnumerable<string> permissionCodes, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据资源和操作获取权限
    /// </summary>
    Task<SysPermission?> GetByResourceOperationAsync(long resourceId, long operationId, CancellationToken cancellationToken = default);
}
