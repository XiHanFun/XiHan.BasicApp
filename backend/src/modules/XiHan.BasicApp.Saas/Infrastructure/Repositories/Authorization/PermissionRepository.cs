#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:PermissionRepository
// Guid:d9f9d85d-c6e3-43e5-879f-de26b4972d88
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/26 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.Uow;

namespace XiHan.BasicApp.Saas.Infrastructure.Repositories;

/// <summary>
/// 权限仓储实现
/// </summary>
public sealed class PermissionRepository(
    ISqlSugarClientResolver clientResolver,
    IUnitOfWorkManager unitOfWorkManager)
    : SaasAggregateRepository<SysPermission>(clientResolver, unitOfWorkManager), IPermissionRepository
{
    /// <inheritdoc />
    public async Task<SysPermission?> GetByCodeAsync(string permissionCode, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(permissionCode);
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateQueryable()
            .Where(permission => permission.PermissionCode == permissionCode)
            .FirstAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SysPermission>> GetByCodesAsync(IEnumerable<string> permissionCodes, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(permissionCodes);

        var codeArray = permissionCodes
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .Distinct()
            .ToArray();
        if (codeArray.Length == 0)
        {
            return [];
        }

        cancellationToken.ThrowIfCancellationRequested();

        return await CreateQueryable()
            .Where(permission => codeArray.Contains(permission.PermissionCode))
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<SysPermission?> GetByResourceOperationAsync(long resourceId, long operationId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateQueryable()
            .Where(permission => permission.ResourceId == resourceId)
            .Where(permission => permission.OperationId == operationId)
            .FirstAsync(cancellationToken);
    }
}
