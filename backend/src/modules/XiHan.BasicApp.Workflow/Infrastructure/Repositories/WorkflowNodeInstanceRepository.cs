// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Infrastructure.Repositories;
using XiHan.BasicApp.Workflow.Domain.Entities;
using XiHan.BasicApp.Workflow.Domain.Repositories;
using XiHan.Framework.Data.SqlSugar.Clients;

namespace XiHan.BasicApp.Workflow.Infrastructure.Repositories;

/// <summary>
/// 工作流节点实例仓储实现
/// </summary>
public sealed class WorkflowNodeInstanceRepository(ISqlSugarClientResolver clientResolver)
    : SaasRepository<SysWorkflowNodeInstance>(clientResolver), IWorkflowNodeInstanceRepository
{
    /// <summary>
    /// 获取实例的节点实例列表（按开始时间升序，同刻按主键升序）
    /// </summary>
    /// <param name="instanceId">实例标识</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>节点实例实体列表</returns>
    public async Task<List<SysWorkflowNodeInstance>> GetByInstanceIdAsync(long instanceId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateQueryable()
            .Where(nodeInstance => nodeInstance.InstanceId == instanceId)
            .OrderBy(nodeInstance => nodeInstance.StartTime)
            .OrderBy(nodeInstance => nodeInstance.BasicId)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 删除实例的全部节点实例（物理删除）
    /// </summary>
    /// <param name="instanceId">实例标识</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>任务</returns>
    public async Task DeleteByInstanceIdAsync(long instanceId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await DeleteAsync(nodeInstance => nodeInstance.InstanceId == instanceId, cancellationToken);
    }
}
