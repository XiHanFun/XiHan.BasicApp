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

    /// <summary>
    /// 按标识查找实例
    /// </summary>
    /// <param name="id">实例标识</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>实例（不存在返回 null）</returns>
    public async Task<WorkflowInstance?> FindAsync(string id, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IWorkflowInstanceRepository>();
        var entity = await repository.GetByIdAsync(WorkflowStoreMapper.ParseId(id), cancellationToken);
        return entity is null ? null : WorkflowStoreMapper.ToModel(entity);
    }

    /// <summary>
    /// 查询实例列表
    /// </summary>
    /// <param name="status">状态（为空表示不过滤）</param>
    /// <param name="definitionCode">定义编码（为空表示不过滤）</param>
    /// <param name="correlationId">业务相关性标识（为空表示不过滤）</param>
    /// <param name="maxResultCount">最大返回条数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>实例列表（按创建时间降序）</returns>
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

    /// <summary>
    /// 获取实例的直接子实例列表
    /// </summary>
    /// <param name="parentInstanceId">父实例标识</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>子实例列表（按创建时间升序）</returns>
    public async Task<List<WorkflowInstance>> GetChildrenAsync(string parentInstanceId, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IWorkflowInstanceRepository>();
        var entities = await repository.GetChildrenAsync(WorkflowStoreMapper.ParseId(parentInstanceId), cancellationToken);
        return [.. entities.Select(WorkflowStoreMapper.ToModel)];
    }

    /// <summary>
    /// 插入实例
    /// </summary>
    /// <param name="instance">实例</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>任务</returns>
    public async Task InsertAsync(WorkflowInstance instance, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IWorkflowInstanceRepository>();
        await repository.AddAsync(WorkflowStoreMapper.ToEntity(instance), cancellationToken);
    }

    /// <summary>
    /// 更新实例
    /// </summary>
    /// <param name="instance">实例</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>任务</returns>
    public async Task UpdateAsync(WorkflowInstance instance, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IWorkflowInstanceRepository>();
        await repository.UpdateAsync(WorkflowStoreMapper.ToEntity(instance), cancellationToken);
    }

    /// <summary>
    /// 删除实例（级联删除节点实例）
    /// </summary>
    /// <param name="id">实例标识</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>任务</returns>
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

    /// <summary>
    /// 按标识查找节点实例
    /// </summary>
    /// <param name="id">节点实例标识</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>节点实例（不存在返回 null）</returns>
    public async Task<WorkflowNodeInstance?> FindNodeInstanceAsync(string id, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IWorkflowNodeInstanceRepository>();
        var entity = await repository.GetByIdAsync(WorkflowStoreMapper.ParseId(id), cancellationToken);
        return entity is null ? null : WorkflowStoreMapper.ToModel(entity);
    }

    /// <summary>
    /// 获取实例的节点实例列表（执行历史）
    /// </summary>
    /// <param name="instanceId">实例标识</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>节点实例列表（按开始时间升序，同刻按创建先后；补偿逆序依赖该顺序）</returns>
    public async Task<List<WorkflowNodeInstance>> GetNodeInstancesAsync(string instanceId, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IWorkflowNodeInstanceRepository>();
        var entities = await repository.GetByInstanceIdAsync(WorkflowStoreMapper.ParseId(instanceId), cancellationToken);
        return [.. entities.Select(WorkflowStoreMapper.ToModel)];
    }

    /// <summary>
    /// 插入节点实例
    /// </summary>
    /// <param name="nodeInstance">节点实例</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>任务</returns>
    public async Task InsertNodeInstanceAsync(WorkflowNodeInstance nodeInstance, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IWorkflowNodeInstanceRepository>();
        await repository.AddAsync(WorkflowStoreMapper.ToEntity(nodeInstance), cancellationToken);
    }

    /// <summary>
    /// 更新节点实例
    /// </summary>
    /// <param name="nodeInstance">节点实例</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>任务</returns>
    public async Task UpdateNodeInstanceAsync(WorkflowNodeInstance nodeInstance, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IWorkflowNodeInstanceRepository>();
        await repository.UpdateAsync(WorkflowStoreMapper.ToEntity(nodeInstance), cancellationToken);
    }
}
