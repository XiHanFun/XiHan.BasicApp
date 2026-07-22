// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Infrastructure.Repositories;
using XiHan.BasicApp.Workflow.Domain.Entities;
using XiHan.BasicApp.Workflow.Domain.Repositories;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.Workflow.Abstractions.Definitions;

namespace XiHan.BasicApp.Workflow.Infrastructure.Repositories;

/// <summary>
/// 工作流定义仓储实现
/// </summary>
public sealed class WorkflowDefinitionRepository(ISqlSugarClientResolver clientResolver)
    : SaasRepository<SysWorkflowDefinition>(clientResolver), IWorkflowDefinitionRepository
{
    /// <inheritdoc />
    public async Task<SysWorkflowDefinition?> GetByCodeAndVersionAsync(string code, int version, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateQueryable()
            .Where(definition => definition.Code == code && definition.Version == version)
            .FirstAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<SysWorkflowDefinition?> GetLatestPublishedAsync(string code, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateQueryable()
            .Where(definition => definition.Code == code && definition.Status == WorkflowDefinitionStatus.Published)
            .OrderByDescending(definition => definition.Version)
            .FirstAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> GetMaxVersionAsync(string code, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        cancellationToken.ThrowIfCancellationRequested();

        var versions = await CreateQueryable()
            .Where(definition => definition.Code == code)
            .Select(definition => definition.Version)
            .ToListAsync(cancellationToken);
        return versions.Count == 0 ? 0 : versions.Max();
    }

    /// <inheritdoc />
    public async Task<List<SysWorkflowDefinition>> GetDefinitionListAsync(
        string? code,
        WorkflowDefinitionStatus? status,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateQueryable()
            .WhereIF(!string.IsNullOrWhiteSpace(code), definition => definition.Code == code)
            .WhereIF(status.HasValue, definition => definition.Status == status!.Value)
            .OrderBy(definition => definition.Code)
            .OrderByDescending(definition => definition.Version)
            .ToListAsync(cancellationToken);
    }
}
