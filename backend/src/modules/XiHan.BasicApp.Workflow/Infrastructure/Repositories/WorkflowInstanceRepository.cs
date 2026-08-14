// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

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
    /// <summary>
    /// 获取实例的直接子实例列表（按创建时间升序）
    /// </summary>
    /// <param name="parentInstanceId">父实例标识</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>子实例实体列表</returns>
    public async Task<List<SysWorkflowInstance>> GetChildrenAsync(long parentInstanceId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateQueryable()
            .Where(instance => instance.ParentInstanceId == parentInstanceId)
            .OrderBy(instance => instance.CreationTime)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 查询实例列表（按创建时间降序）
    /// </summary>
    /// <param name="status">状态（为空表示不过滤）</param>
    /// <param name="definitionCode">定义编码（为空表示不过滤）</param>
    /// <param name="correlationId">业务相关性标识（为空表示不过滤）</param>
    /// <param name="maxResultCount">最大返回条数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>实例实体列表</returns>
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
