// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.Uow;

namespace XiHan.BasicApp.Saas.Infrastructure.Repositories;

/// <summary>
/// 操作仓储实现
/// </summary>
public sealed class OperationRepository(
    ISqlSugarClientResolver clientResolver,
    IUnitOfWorkManager unitOfWorkManager)
    : SaasAggregateRepository<SysOperation>(clientResolver, unitOfWorkManager), IOperationRepository
{
    /// <inheritdoc />
    public async Task<SysOperation?> GetByCodeAsync(string operationCode, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operationCode);
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateQueryable()
            .Where(operation => operation.OperationCode == operationCode)
            .FirstAsync(cancellationToken);
    }
}
