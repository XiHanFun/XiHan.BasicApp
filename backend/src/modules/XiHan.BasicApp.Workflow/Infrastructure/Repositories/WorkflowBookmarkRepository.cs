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
    /// <inheritdoc />
    public async Task<List<SysWorkflowBookmark>> GetByInstanceIdAsync(long instanceId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateQueryable()
            .Where(bookmark => bookmark.InstanceId == instanceId)
            .OrderBy(bookmark => bookmark.CreationTime)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<List<SysWorkflowBookmark>> GetByNodeInstanceIdAsync(long nodeInstanceId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateQueryable()
            .Where(bookmark => bookmark.NodeInstanceId == nodeInstanceId)
            .OrderBy(bookmark => bookmark.CreationTime)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<List<SysWorkflowBookmark>> GetDueAsync(DateTime now, int maxResultCount, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateQueryable()
            .Where(bookmark => bookmark.DueTime != null && bookmark.DueTime <= now)
            .OrderBy(bookmark => bookmark.DueTime)
            .Take(maxResultCount)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
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

    /// <inheritdoc />
    public async Task DeleteByInstanceIdAsync(long instanceId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await DeleteAsync(bookmark => bookmark.InstanceId == instanceId, cancellationToken);
    }
}
