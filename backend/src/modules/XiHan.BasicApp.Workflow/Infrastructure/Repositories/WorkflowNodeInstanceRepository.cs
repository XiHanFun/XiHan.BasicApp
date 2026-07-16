#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:WorkflowNodeInstanceRepository
// Guid:07c48e2d-63b9-4f50-a1e8-92d05c74b6f1
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/17 10:11:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

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
    /// <inheritdoc />
    public async Task<List<SysWorkflowNodeInstance>> GetByInstanceIdAsync(long instanceId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateQueryable()
            .Where(nodeInstance => nodeInstance.InstanceId == instanceId)
            .OrderBy(nodeInstance => nodeInstance.StartTime)
            .OrderBy(nodeInstance => nodeInstance.BasicId)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task DeleteByInstanceIdAsync(long instanceId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await DeleteAsync(nodeInstance => nodeInstance.InstanceId == instanceId, cancellationToken);
    }
}
