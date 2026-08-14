// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.Workflow.Domain.Repositories;
using XiHan.Framework.Workflow.Abstractions.Definitions;
using XiHan.Framework.Workflow.Abstractions.Stores;

namespace XiHan.BasicApp.Workflow.Infrastructure.Stores;

/// <summary>
/// SqlSugar 工作流定义存储（替换框架内存默认实现）
/// </summary>
/// <remarks>
/// 框架把存储注册为单例，此处按操作创建作用域解析仓储（与 SaasJobStore 同构）。
/// 更新走"加载现有实体、只覆盖业务列"以保全审计列与乐观锁。
/// </remarks>
public sealed class SqlSugarWorkflowDefinitionStore : IWorkflowDefinitionStore
{
    private readonly IServiceScopeFactory _scopeFactory;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="scopeFactory">服务作用域工厂</param>
    public SqlSugarWorkflowDefinitionStore(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    /// <summary>
    /// 按标识查找定义
    /// </summary>
    /// <param name="id">定义标识</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>定义（不存在返回 null）</returns>
    public async Task<WorkflowDefinition?> FindAsync(string id, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IWorkflowDefinitionRepository>();
        var entity = await repository.GetByIdAsync(WorkflowStoreMapper.ParseId(id), cancellationToken);
        return entity is null ? null : WorkflowStoreMapper.ToModel(entity);
    }

    /// <summary>
    /// 按编码和版本查找定义
    /// </summary>
    /// <param name="code">流程编码</param>
    /// <param name="version">版本号</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>定义（不存在返回 null）</returns>
    public async Task<WorkflowDefinition?> FindByVersionAsync(string code, int version, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IWorkflowDefinitionRepository>();
        var entity = await repository.GetByCodeAndVersionAsync(code, version, cancellationToken);
        return entity is null ? null : WorkflowStoreMapper.ToModel(entity);
    }

    /// <summary>
    /// 查找编码下最新的已发布定义
    /// </summary>
    /// <remarks>
    /// 语义契约：过滤 <c>Code 匹配 &amp;&amp; Status == Published</c>，按 <c>Version 降序</c> 取第一条。
    /// </remarks>
    /// <param name="code">流程编码</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>定义（不存在返回 null）</returns>
    public async Task<WorkflowDefinition?> FindLatestPublishedAsync(string code, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IWorkflowDefinitionRepository>();
        var entity = await repository.GetLatestPublishedAsync(code, cancellationToken);
        return entity is null ? null : WorkflowStoreMapper.ToModel(entity);
    }

    /// <summary>
    /// 获取编码下的最大版本号
    /// </summary>
    /// <param name="code">流程编码</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>最大版本号（编码不存在返回 0）</returns>
    public async Task<int> GetMaxVersionAsync(string code, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IWorkflowDefinitionRepository>();
        return await repository.GetMaxVersionAsync(code, cancellationToken);
    }

    /// <summary>
    /// 查询定义列表
    /// </summary>
    /// <param name="code">流程编码（为空表示不过滤）</param>
    /// <param name="status">状态（为空表示不过滤）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>定义列表（按编码升序、版本降序）</returns>
    public async Task<List<WorkflowDefinition>> GetListAsync(
        string? code = null,
        WorkflowDefinitionStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IWorkflowDefinitionRepository>();
        var entities = await repository.GetDefinitionListAsync(code, status, cancellationToken);
        return [.. entities.Select(WorkflowStoreMapper.ToModel)];
    }

    /// <summary>
    /// 插入定义
    /// </summary>
    /// <param name="definition">定义</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>任务</returns>
    public async Task InsertAsync(WorkflowDefinition definition, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IWorkflowDefinitionRepository>();
        await repository.AddAsync(WorkflowStoreMapper.ToEntity(definition), cancellationToken);
    }

    /// <summary>
    /// 更新定义
    /// </summary>
    /// <param name="definition">定义</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>任务</returns>
    public async Task UpdateAsync(WorkflowDefinition definition, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IWorkflowDefinitionRepository>();

        var existing = await repository.GetByIdAsync(WorkflowStoreMapper.ParseId(definition.Id), cancellationToken);
        if (existing is null)
        {
            return;
        }

        WorkflowStoreMapper.ToEntity(definition, existing);
        await repository.UpdateAsync(existing, cancellationToken);
    }

    /// <summary>
    /// 删除定义
    /// </summary>
    /// <param name="id">定义标识</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>任务</returns>
    public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IWorkflowDefinitionRepository>();
        await repository.DeleteByIdAsync(WorkflowStoreMapper.ParseId(id), cancellationToken);
    }
}
