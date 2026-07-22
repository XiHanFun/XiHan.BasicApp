// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Data.SqlSugar.Clients;

namespace XiHan.BasicApp.Saas.Infrastructure.Repositories;

/// <summary>
/// 权限申请仓储实现
/// </summary>
public sealed class PermissionRequestRepository(ISqlSugarClientResolver clientResolver)
    : SaasRepository<SysPermissionRequest>(clientResolver), IPermissionRequestRepository
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<SysPermissionRequest>> GetPendingByPermissionIdAsync(long permissionId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateQueryable()
            .Where(r => r.PermissionId == permissionId && r.RequestStatus == PermissionRequestStatus.Pending)
            .ToListAsync(cancellationToken);
    }
}
