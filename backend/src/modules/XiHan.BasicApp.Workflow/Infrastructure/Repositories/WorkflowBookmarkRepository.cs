// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Infrastructure.Repositories;
using XiHan.BasicApp.Workflow.Domain.Entities;
using XiHan.BasicApp.Workflow.Domain.Repositories;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.Workflow.Abstractions;

namespace XiHan.BasicApp.Workflow.Infrastructure.Repositories;

/// <summary>
/// 工作流书签仓储实现
/// </summary>
public sealed class WorkflowBookmarkRepository(ISqlSugarClientResolver clientResolver)
    : SaasRepository<SysWorkflowBookmark>(clientResolver), IWorkflowBookmarkRepository
{
    /// <summary>
    /// 获取实例的全部书签（按创建时间升序）
    /// </summary>
    /// <param name="instanceId">实例标识</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>书签实体列表</returns>
    public async Task<List<SysWorkflowBookmark>> GetByInstanceIdAsync(long instanceId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateQueryable()
            .Where(bookmark => bookmark.InstanceId == instanceId)
            .OrderBy(bookmark => bookmark.CreationTime)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 获取节点实例的全部书签（按创建时间升序）
    /// </summary>
    /// <param name="nodeInstanceId">节点实例标识</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>书签实体列表</returns>
    public async Task<List<SysWorkflowBookmark>> GetByNodeInstanceIdAsync(long nodeInstanceId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateQueryable()
            .Where(bookmark => bookmark.NodeInstanceId == nodeInstanceId)
            .OrderBy(bookmark => bookmark.CreationTime)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 获取到期的定时类书签（DueTime 非空且不晚于当前时间，按到期时间升序）
    /// </summary>
    /// <param name="now">当前时间</param>
    /// <param name="maxResultCount">最大返回条数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>到期书签实体列表</returns>
    public async Task<List<SysWorkflowBookmark>> GetDueAsync(DateTime now, int maxResultCount, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateQueryable()
            .Where(bookmark => bookmark.DueTime != null && bookmark.DueTime <= now)
            .OrderBy(bookmark => bookmark.DueTime)
            .Take(maxResultCount)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 按种类和索引键查询书签（按创建时间升序）
    /// </summary>
    /// <param name="kind">书签种类</param>
    /// <param name="key">索引键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>书签实体列表</returns>
    public async Task<List<SysWorkflowBookmark>> GetByKindAndKeyAsync(string kind, string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(kind);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateQueryable()
            .Where(bookmark => bookmark.Kind == kind && bookmark.Key == key)
            .OrderBy(bookmark => bookmark.CreationTime)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 查询匹配信号的书签（相关性为空的书签不限相关性；按创建时间升序）
    /// </summary>
    /// <param name="signalName">信号名称</param>
    /// <param name="correlationId">业务相关性标识（为空表示广播）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>书签实体列表</returns>
    public async Task<List<SysWorkflowBookmark>> GetBySignalAsync(string signalName, string? correlationId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(signalName);
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateQueryable()
            .Where(bookmark => bookmark.Kind == WorkflowBookmarkKinds.Signal && bookmark.Key == signalName)
            .WhereIF(!string.IsNullOrWhiteSpace(correlationId),
                bookmark => bookmark.CorrelationId == null || bookmark.CorrelationId == correlationId)
            .OrderBy(bookmark => bookmark.CreationTime)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 删除实例的全部书签（物理删除）
    /// </summary>
    /// <param name="instanceId">实例标识</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>任务</returns>
    public async Task DeleteByInstanceIdAsync(long instanceId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await DeleteAsync(bookmark => bookmark.InstanceId == instanceId, cancellationToken);
    }
}
