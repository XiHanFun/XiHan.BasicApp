#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IPermissionRepository
// Guid:2cf13cb1-5c84-4e5e-bf98-f7f9b7d859d3
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/26 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

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
