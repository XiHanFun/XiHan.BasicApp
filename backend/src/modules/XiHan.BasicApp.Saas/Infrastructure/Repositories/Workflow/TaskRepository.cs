// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.Uow;

namespace XiHan.BasicApp.Saas.Infrastructure.Repositories;

/// <summary>
/// 任务仓储实现
/// </summary>
public sealed class TaskRepository(
    ISqlSugarClientResolver clientResolver,
    IUnitOfWorkManager unitOfWorkManager)
    : SaasAggregateRepository<SysTask>(clientResolver, unitOfWorkManager), ITaskRepository
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<SysTask>> GetPendingTasksAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateQueryable()
            .Where(task => task.RunTaskStatus == RunTaskStatus.Pending)
            .ToListAsync(cancellationToken);
    }
}
