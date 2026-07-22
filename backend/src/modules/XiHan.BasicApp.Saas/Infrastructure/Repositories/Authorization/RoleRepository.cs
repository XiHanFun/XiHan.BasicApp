// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.Uow;

namespace XiHan.BasicApp.Saas.Infrastructure.Repositories;

/// <summary>
/// 角色仓储实现
/// </summary>
public sealed class RoleRepository(
    ISqlSugarClientResolver clientResolver,
    IUnitOfWorkManager unitOfWorkManager)
    : SaasAggregateRepository<SysRole>(clientResolver, unitOfWorkManager), IRoleRepository
{
    /// <inheritdoc />
    public async Task<SysRole?> GetByCodeAsync(string roleCode, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(roleCode);
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateQueryable()
            .Where(role => role.RoleCode == roleCode)
            .FirstAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SysRole>> GetEnabledByIdsAsync(IEnumerable<long> roleIds, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(roleIds);

        var roleIdArray = roleIds.Distinct().ToArray();
        if (roleIdArray.Length == 0)
        {
            return [];
        }

        cancellationToken.ThrowIfCancellationRequested();

        return await CreateQueryable()
            .Where(role => roleIdArray.Contains(role.BasicId))
            .Where(role => role.Status == EnableStatus.Enabled)
            .ToListAsync(cancellationToken);
    }
}
