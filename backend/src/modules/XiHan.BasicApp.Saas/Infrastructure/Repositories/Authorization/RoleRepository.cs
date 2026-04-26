#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:RoleRepository
// Guid:dbb99c79-9ec5-4b58-bb04-b3a2c99ffc61
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/26 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

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
