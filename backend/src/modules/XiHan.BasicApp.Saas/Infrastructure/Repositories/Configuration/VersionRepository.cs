// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Data.SqlSugar.Clients;

namespace XiHan.BasicApp.Saas.Infrastructure.Repositories;

/// <summary>
/// 版本仓储实现
/// </summary>
public sealed class VersionRepository(ISqlSugarClientResolver clientResolver)
    : SaasRepository<SysVersion>(clientResolver), IVersionRepository
{
    /// <inheritdoc />
    public async Task<SysVersion?> GetLatestAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateQueryable()
            .OrderByDescending(version => version.CreatedTime)
            .FirstAsync(cancellationToken);
    }
}
