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

    /// <inheritdoc />
    public async Task<WorkflowDefinition?> FindAsync(string id, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IWorkflowDefinitionRepository>();
        var entity = await repository.GetByIdAsync(WorkflowStoreMapper.ParseId(id), cancellationToken);
        return entity is null ? null : WorkflowStoreMapper.ToModel(entity);
    }

    /// <inheritdoc />
    public async Task<WorkflowDefinition?> FindByVersionAsync(string code, int version, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IWorkflowDefinitionRepository>();
        var entity = await repository.GetByCodeAndVersionAsync(code, version, cancellationToken);
        return entity is null ? null : WorkflowStoreMapper.ToModel(entity);
    }

    /// <inheritdoc />
    public async Task<WorkflowDefinition?> FindLatestPublishedAsync(string code, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IWorkflowDefinitionRepository>();
        var entity = await repository.GetLatestPublishedAsync(code, cancellationToken);
        return entity is null ? null : WorkflowStoreMapper.ToModel(entity);
    }

    /// <inheritdoc />
    public async Task<int> GetMaxVersionAsync(string code, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IWorkflowDefinitionRepository>();
        return await repository.GetMaxVersionAsync(code, cancellationToken);
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
    public async Task InsertAsync(WorkflowDefinition definition, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IWorkflowDefinitionRepository>();
        await repository.AddAsync(WorkflowStoreMapper.ToEntity(definition), cancellationToken);
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
    public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IWorkflowDefinitionRepository>();
        await repository.DeleteByIdAsync(WorkflowStoreMapper.ParseId(id), cancellationToken);
    }
}
