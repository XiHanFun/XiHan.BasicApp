// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.Workflow.Domain.Repositories;
using XiHan.Framework.Workflow.Abstractions.Runtime;
using XiHan.Framework.Workflow.Abstractions.Stores;

namespace XiHan.BasicApp.Workflow.Infrastructure.Stores;

/// <summary>
/// SqlSugar 工作流书签存储（替换框架内存默认实现）
/// </summary>
/// <remarks>
/// 框架把存储注册为单例，此处按操作创建作用域解析仓储；
/// 定时器 Worker 已通过分布式锁保证集群单活，无需查询层原子领取。
/// </remarks>
public sealed class SqlSugarWorkflowBookmarkStore : IWorkflowBookmarkStore
{
    private readonly IServiceScopeFactory _scopeFactory;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="scopeFactory">服务作用域工厂</param>
    public SqlSugarWorkflowBookmarkStore(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    /// <summary>
    /// 按标识查找书签
    /// </summary>
    /// <param name="id">书签标识</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>书签（不存在返回 null）</returns>
    public async Task<WorkflowBookmark?> FindAsync(string id, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IWorkflowBookmarkRepository>();
        var entity = await repository.GetByIdAsync(WorkflowStoreMapper.ParseId(id), cancellationToken);
        return entity is null ? null : WorkflowStoreMapper.ToModel(entity);
    }

    /// <summary>
    /// 获取实例的全部书签
    /// </summary>
    /// <param name="instanceId">实例标识</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>书签列表</returns>
    public async Task<List<WorkflowBookmark>> GetByInstanceAsync(string instanceId, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IWorkflowBookmarkRepository>();
        var entities = await repository.GetByInstanceIdAsync(WorkflowStoreMapper.ParseId(instanceId), cancellationToken);
        return [.. entities.Select(WorkflowStoreMapper.ToModel)];
    }

    /// <summary>
    /// 获取节点实例的全部书签
    /// </summary>
    /// <param name="nodeInstanceId">节点实例标识</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>书签列表</returns>
    public async Task<List<WorkflowBookmark>> GetByNodeInstanceAsync(string nodeInstanceId, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IWorkflowBookmarkRepository>();
        var entities = await repository.GetByNodeInstanceIdAsync(WorkflowStoreMapper.ParseId(nodeInstanceId), cancellationToken);
        return [.. entities.Select(WorkflowStoreMapper.ToModel)];
    }

    /// <summary>
    /// 获取到期的定时类书签
    /// </summary>
    /// <remarks>
    /// 语义契约：过滤 <c>DueTime 非空 &amp;&amp; DueTime &lt;= now</c>；排序 <c>DueTime 升序</c>；最多返回 <paramref name="maxResultCount"/> 条。
    /// </remarks>
    /// <param name="now">当前时间</param>
    /// <param name="maxResultCount">最大返回条数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>到期书签列表</returns>
    public async Task<List<WorkflowBookmark>> GetDueAsync(DateTime now, int maxResultCount, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IWorkflowBookmarkRepository>();
        var entities = await repository.GetDueAsync(now, maxResultCount, cancellationToken);
        return [.. entities.Select(WorkflowStoreMapper.ToModel)];
    }

    /// <summary>
    /// 按种类和索引键查询书签
    /// </summary>
    /// <param name="kind">书签种类</param>
    /// <param name="key">索引键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>书签列表（按创建时间升序）</returns>
    public async Task<List<WorkflowBookmark>> GetByKindAndKeyAsync(string kind, string key, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IWorkflowBookmarkRepository>();
        var entities = await repository.GetByKindAndKeyAsync(kind, key, cancellationToken);
        return [.. entities.Select(WorkflowStoreMapper.ToModel)];
    }

    /// <summary>
    /// 查询匹配信号的书签
    /// </summary>
    /// <remarks>
    /// 语义契约：过滤 <c>Kind == Signal &amp;&amp; Key == signalName</c>；
    /// <paramref name="correlationId"/> 非空时额外要求 <c>书签 CorrelationId 为空（不限相关性）或与之相等</c>，
    /// 为空时表示广播，不按相关性过滤。
    /// </remarks>
    /// <param name="signalName">信号名称</param>
    /// <param name="correlationId">业务相关性标识</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>书签列表（按创建时间升序）</returns>
    public async Task<List<WorkflowBookmark>> GetBySignalAsync(string signalName, string? correlationId, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IWorkflowBookmarkRepository>();
        var entities = await repository.GetBySignalAsync(signalName, correlationId, cancellationToken);
        return [.. entities.Select(WorkflowStoreMapper.ToModel)];
    }

    /// <summary>
    /// 插入书签
    /// </summary>
    /// <param name="bookmark">书签</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>任务</returns>
    public async Task InsertAsync(WorkflowBookmark bookmark, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IWorkflowBookmarkRepository>();
        await repository.AddAsync(WorkflowStoreMapper.ToEntity(bookmark), cancellationToken);
    }

    /// <summary>
    /// 更新书签
    /// </summary>
    /// <param name="bookmark">书签</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>任务</returns>
    public async Task UpdateAsync(WorkflowBookmark bookmark, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IWorkflowBookmarkRepository>();
        await repository.UpdateAsync(WorkflowStoreMapper.ToEntity(bookmark), cancellationToken);
    }

    /// <summary>
    /// 删除书签
    /// </summary>
    /// <param name="id">书签标识</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>任务</returns>
    public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IWorkflowBookmarkRepository>();
        await repository.DeleteByIdAsync(WorkflowStoreMapper.ParseId(id), cancellationToken);
    }

    /// <summary>
    /// 删除实例的全部书签
    /// </summary>
    /// <param name="instanceId">实例标识</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>任务</returns>
    public async Task DeleteByInstanceAsync(string instanceId, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IWorkflowBookmarkRepository>();
        await repository.DeleteByInstanceIdAsync(WorkflowStoreMapper.ParseId(instanceId), cancellationToken);
    }
}
