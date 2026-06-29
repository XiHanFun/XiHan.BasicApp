#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:PositionRepository
// Guid:5c8eb4fd-a067-4b3c-cf94-4d5e6f708193
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/06/29 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

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
