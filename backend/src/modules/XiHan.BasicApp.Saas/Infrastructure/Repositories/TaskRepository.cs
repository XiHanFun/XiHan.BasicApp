#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:TaskRepository
// Guid:60bac9f9-0dd8-462d-9a2f-b5db31e9d6fa
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/03/04 12:19:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.Data.SqlSugar.Repository;

using XiHan.Framework.Uow;

namespace XiHan.BasicApp.Saas.Infrastructure.Repositories;

/// <summary>
/// 任务仓储实现
/// </summary>
public class TaskRepository : SqlSugarAggregateRepository<SysTask, long>, ITaskRepository
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public TaskRepository(
        ISqlSugarClientResolver clientResolver,
        IUnitOfWorkManager unitOfWorkManager)
        : base(clientResolver, unitOfWorkManager)
    {
    }

    /// <summary>
    /// 根据任务编码获取任务
    /// </summary>
    public async Task<SysTask?> GetByTaskCodeAsync(string taskCode, long? tenantId = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(taskCode);
        var resolvedTenantId = tenantId;

        var query = CreateQueryable()
            .Where(task => task.TaskCode == taskCode);

        query = resolvedTenantId.HasValue
            ? query.Where(task => task.TenantId == resolvedTenantId.Value)
            : query.Where(task => task.TenantId == 0);

        return await query.FirstAsync(cancellationToken);
    }

    /// <summary>
    /// 校验任务编码是否已存在
    /// </summary>
    public async Task<bool> IsTaskCodeExistsAsync(string taskCode, long? tenantId = null, long? excludeTaskId = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(taskCode);
        var resolvedTenantId = tenantId;

        var query = CreateQueryable()
            .Where(task => task.TaskCode == taskCode);

        query = resolvedTenantId.HasValue
            ? query.Where(task => task.TenantId == resolvedTenantId.Value)
            : query.Where(task => task.TenantId == 0);

        if (excludeTaskId.HasValue)
        {
            query = query.Where(task => task.BasicId != excludeTaskId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }
}
