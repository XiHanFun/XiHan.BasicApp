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
    /// <summary>
    /// 按编码和版本查找定义
    /// </summary>
    /// <param name="code">流程编码</param>
    /// <param name="version">版本号</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>定义实体（不存在返回 null）</returns>
    public async Task<SysWorkflowDefinition?> GetByCodeAndVersionAsync(string code, int version, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateQueryable()
            .Where(definition => definition.Code == code && definition.Version == version)
            .FirstAsync(cancellationToken);
    }

    /// <summary>
    /// 查找编码下最新的已发布定义
    /// </summary>
    /// <param name="code">流程编码</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>定义实体（不存在返回 null）</returns>
    public async Task<SysWorkflowDefinition?> GetLatestPublishedAsync(string code, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateQueryable()
            .Where(definition => definition.Code == code && definition.Status == WorkflowDefinitionStatus.Published)
            .OrderByDescending(definition => definition.Version)
            .FirstAsync(cancellationToken);
    }

    /// <summary>
    /// 获取编码下的最大版本号
    /// </summary>
    /// <param name="code">流程编码</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>最大版本号（编码不存在返回 0）</returns>
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

    /// <summary>
    /// 查询定义列表（按编码升序、版本降序）
    /// </summary>
    /// <param name="code">流程编码（为空表示不过滤）</param>
    /// <param name="status">状态（为空表示不过滤）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>定义实体列表</returns>
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
