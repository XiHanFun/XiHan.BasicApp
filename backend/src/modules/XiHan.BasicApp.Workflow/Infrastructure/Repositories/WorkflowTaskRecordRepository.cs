// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Infrastructure.Repositories;
using XiHan.BasicApp.Workflow.Domain.Entities;
using XiHan.BasicApp.Workflow.Domain.Repositories;
using XiHan.Framework.Data.SqlSugar.Clients;

namespace XiHan.BasicApp.Workflow.Infrastructure.Repositories;

/// <summary>
/// 工作流审批记录仓储实现
/// </summary>
public sealed class WorkflowTaskRecordRepository(ISqlSugarClientResolver clientResolver)
    : SaasRepository<SysWorkflowTaskRecord>(clientResolver), IWorkflowTaskRecordRepository
{
    /// <summary>
    /// 获取实例的全部审批记录（按记录时间升序）
    /// </summary>
    /// <param name="instanceId">实例标识</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>审批记录列表</returns>
    public async Task<List<SysWorkflowTaskRecord>> GetByInstanceIdAsync(long instanceId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateQueryable()
            .Where(record => record.InstanceId == instanceId)
            .OrderBy(record => record.CreatedTime)
            .OrderBy(record => record.BasicId)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 用户是否参与过实例
    /// </summary>
    /// <param name="instanceId">实例标识</param>
    /// <param name="userId">用户主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>参与过返回 true</returns>
    public async Task<bool> HasParticipatedAsync(long instanceId, long userId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateQueryable()
            .Where(record => record.InstanceId == instanceId && record.UserId == userId)
            .AnyAsync(cancellationToken);
    }
}
