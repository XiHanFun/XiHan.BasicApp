#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:WorkflowInstanceRepository
// Guid:6b93e0f5-27d4-4a81-95c3-e07d16b42f89
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/17 10:10:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Infrastructure.Repositories;
using XiHan.BasicApp.Workflow.Domain.Entities;
using XiHan.BasicApp.Workflow.Domain.Repositories;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.Workflow.Abstractions.Runtime;

namespace XiHan.BasicApp.Workflow.Infrastructure.Repositories;

/// <summary>
/// 工作流实例仓储实现
/// </summary>
public sealed class WorkflowInstanceRepository(ISqlSugarClientResolver clientResolver)
    : SaasRepository<SysWorkflowInstance>(clientResolver), IWorkflowInstanceRepository
{
    /// <inheritdoc />
    public async Task<List<SysWorkflowInstance>> GetChildrenAsync(long parentInstanceId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateQueryable()
            .Where(instance => instance.ParentInstanceId == parentInstanceId)
            .OrderBy(instance => instance.CreationTime)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<List<SysWorkflowInstance>> GetInstanceListAsync(
        WorkflowInstanceStatus? status,
        string? definitionCode,
        string? correlationId,
        int maxResultCount,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateQueryable()
            .WhereIF(status.HasValue, instance => instance.Status == status!.Value)
            .WhereIF(!string.IsNullOrWhiteSpace(definitionCode), instance => instance.DefinitionCode == definitionCode)
            .WhereIF(!string.IsNullOrWhiteSpace(correlationId), instance => instance.CorrelationId == correlationId)
            .OrderByDescending(instance => instance.CreationTime)
            .Take(maxResultCount)
            .ToListAsync(cancellationToken);
    }
}
