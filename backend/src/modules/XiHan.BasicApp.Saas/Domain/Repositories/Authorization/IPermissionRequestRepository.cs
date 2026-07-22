// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Domain.Repositories;

/// <summary>
/// 权限申请仓储接口
/// </summary>
public interface IPermissionRequestRepository : ISaasRepository<SysPermissionRequest>
{
    /// <summary>
    /// 获取某权限的待处理申请
    /// </summary>
    Task<IReadOnlyList<SysPermissionRequest>> GetPendingByPermissionIdAsync(long permissionId, CancellationToken cancellationToken = default);
}
