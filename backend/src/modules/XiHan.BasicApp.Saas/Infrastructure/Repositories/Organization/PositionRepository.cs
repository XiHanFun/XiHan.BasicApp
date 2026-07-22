// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Data.SqlSugar.Clients;

namespace XiHan.BasicApp.Saas.Infrastructure.Repositories;

/// <summary>
/// 岗位仓储实现
/// </summary>
public sealed class PositionRepository(ISqlSugarClientResolver clientResolver)
    : SaasRepository<SysPosition>(clientResolver), IPositionRepository
{
    /// <inheritdoc />
    public async Task<SysPosition?> GetByCodeAsync(string positionCode, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(positionCode);
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateQueryable()
            .Where(position => position.PositionCode == positionCode.Trim())
            .FirstAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> ExistsCodeAsync(string positionCode, long? excludeId = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(positionCode);
        cancellationToken.ThrowIfCancellationRequested();

        var query = CreateQueryable().Where(position => position.PositionCode == positionCode.Trim());
        if (excludeId.HasValue)
        {
            query = query.Where(position => position.BasicId != excludeId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }
}
