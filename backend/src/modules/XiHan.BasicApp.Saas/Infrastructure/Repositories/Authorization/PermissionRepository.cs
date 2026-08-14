// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

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
    /// <summary>
    /// 根据当前租户和权限编码获取权限
    /// </summary>
    public async Task<SysPermission?> GetByCodeAsync(string permissionCode, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(permissionCode);
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateQueryable()
            .Where(permission => permission.PermissionCode == permissionCode)
            .FirstAsync(cancellationToken);
    }

    /// <summary>
    /// 根据权限编码集合获取权限
    /// </summary>
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

    /// <summary>
    /// 根据资源和操作获取权限
    /// </summary>
    public async Task<SysPermission?> GetByResourceOperationAsync(long resourceId, long operationId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateQueryable()
            .Where(permission => permission.ResourceId == resourceId)
            .Where(permission => permission.OperationId == operationId)
            .FirstAsync(cancellationToken);
    }
}
