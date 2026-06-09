#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:OperationRepository
// Guid:68b90ca3-720d-4bdb-b99a-923fdb063a3d
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
