// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Data.SqlSugar.Clients;

namespace XiHan.BasicApp.Saas.Infrastructure.Repositories;

/// <summary>
/// 租户版本仓储实现
/// </summary>
public sealed class TenantEditionRepository(ISqlSugarClientResolver clientResolver)
    : SaasRepository<SysTenantEdition>(clientResolver), ITenantEditionRepository
{
    /// <inheritdoc />
    public async Task<SysTenantEdition?> GetDefaultEditionAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateQueryable()
            .Where(edition => edition.IsDefault)
            .FirstAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> ExistsCodeAsync(string editionCode, long? excludeId = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(editionCode);
        cancellationToken.ThrowIfCancellationRequested();

        var query = CreateQueryable().Where(edition => edition.EditionCode == editionCode);
        if (excludeId.HasValue)
        {
            query = query.Where(edition => edition.BasicId != excludeId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }
}
