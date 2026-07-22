// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.Uow;

namespace XiHan.BasicApp.Saas.Infrastructure.Repositories;

/// <summary>
/// 资源仓储实现
/// </summary>
public sealed class ResourceRepository(
    ISqlSugarClientResolver clientResolver,
    IUnitOfWorkManager unitOfWorkManager)
    : SaasAggregateRepository<SysResource>(clientResolver, unitOfWorkManager), IResourceRepository
{
    /// <inheritdoc />
    public async Task<SysResource?> GetByCodeAsync(string resourceCode, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resourceCode);
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateQueryable()
            .Where(resource => resource.ResourceCode == resourceCode)
            .FirstAsync(cancellationToken);
    }
}
