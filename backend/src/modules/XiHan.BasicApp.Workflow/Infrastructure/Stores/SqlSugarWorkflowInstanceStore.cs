// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.Workflow.Domain.Repositories;
using XiHan.Framework.Workflow.Abstractions.Runtime;
using XiHan.Framework.Workflow.Abstractions.Stores;

namespace XiHan.BasicApp.Workflow.Infrastructure.Stores;

/// <summary>
/// SqlSugar 工作流实例存储（替换框架内存默认实现）
/// </summary>
/// <remarks>
/// 框架把存储注册为单例，此处按操作创建作用域解析仓储；
/// 引擎对同一实例的读写已由实例级分布式锁串行化，无需乐观并发控制。
/// </remarks>
public sealed class SqlSugarWorkflowInstanceStore : IWorkflowInstanceStore
{
    private readonly IServiceScopeFactory _scopeFactory;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="scopeFactory">服务作用域工厂</param>
    public SqlSugarWorkflowInstanceStore(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    /// <inheritdoc />
    public async Task<WorkflowInstance?> FindAsync(string id, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IWorkflowInstanceRepository>();
        var entity = await repository.GetByIdAsync(WorkflowStoreMapper.ParseId(id), cancellationToken);
        return entity is null ? null : WorkflowStoreMapper.ToModel(entity);
    }

    /// <inheritdoc />
    public async Task<List<WorkflowInstance>> GetListAsync(
        WorkflowInstanceStatus? status = null,
        string? definitionCode = null,
        string? correlationId = null,
        int maxResultCount = 100,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IWorkflowInstanceRepository>();
        var entities = await repository.GetInstanceListAsync(status, definitionCode, correlationId, maxResultCount, cancellationToken);
        return [.. entities.Select(WorkflowStoreMapper.ToModel)];
    }

    /// <inheritdoc />
    public async Task<List<WorkflowInstance>> GetChildrenAsync(string parentInstanceId, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IWorkflowInstanceRepository>();
        var entities = await repository.GetChildrenAsync(WorkflowStoreMapper.ParseId(parentInstanceId), cancellationToken);
        return [.. entities.Select(WorkflowStoreMapper.ToModel)];
    }

    /// <inheritdoc />
    public async Task InsertAsync(WorkflowInstance instance, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IWorkflowInstanceRepository>();
        await repository.AddAsync(WorkflowStoreMapper.ToEntity(instance), cancellationToken);
    }

    /// <inheritdoc />
    public async Task UpdateAsync(WorkflowInstance instance, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IWorkflowInstanceRepository>();
        await repository.UpdateAsync(WorkflowStoreMapper.ToEntity(instance), cancellationToken);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var instanceId = WorkflowStoreMapper.ParseId(id);
        var instanceRepository = scope.ServiceProvider.GetRequiredService<IWorkflowInstanceRepository>();
        var nodeInstanceRepository = scope.ServiceProvider.GetRequiredService<IWorkflowNodeInstanceRepository>();

        // 契约：删除实例级联删除节点实例
        await nodeInstanceRepository.DeleteByInstanceIdAsync(instanceId, cancellationToken);
        await instanceRepository.DeleteByIdAsync(instanceId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<WorkflowNodeInstance?> FindNodeInstanceAsync(string id, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IWorkflowNodeInstanceRepository>();
        var entity = await repository.GetByIdAsync(WorkflowStoreMapper.ParseId(id), cancellationToken);
        return entity is null ? null : WorkflowStoreMapper.ToModel(entity);
    }

    /// <inheritdoc />
    public async Task<List<WorkflowNodeInstance>> GetNodeInstancesAsync(string instanceId, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IWorkflowNodeInstanceRepository>();
        var entities = await repository.GetByInstanceIdAsync(WorkflowStoreMapper.ParseId(instanceId), cancellationToken);
        return [.. entities.Select(WorkflowStoreMapper.ToModel)];
    }

    /// <inheritdoc />
    public async Task InsertNodeInstanceAsync(WorkflowNodeInstance nodeInstance, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IWorkflowNodeInstanceRepository>();
        await repository.AddAsync(WorkflowStoreMapper.ToEntity(nodeInstance), cancellationToken);
    }

    /// <inheritdoc />
    public async Task UpdateNodeInstanceAsync(WorkflowNodeInstance nodeInstance, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IWorkflowNodeInstanceRepository>();
        await repository.UpdateAsync(WorkflowStoreMapper.ToEntity(nodeInstance), cancellationToken);
    }
}
