// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.BasicApp.Workflow.Domain.Entities;

namespace XiHan.BasicApp.Workflow.Domain.Repositories;

/// <summary>
/// 工作流书签仓储接口
/// </summary>
public interface IWorkflowBookmarkRepository : ISaasRepository<SysWorkflowBookmark>
{
    /// <summary>
    /// 获取实例的全部书签（按创建时间升序）
    /// </summary>
    /// <param name="instanceId">实例标识</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>书签实体列表</returns>
    Task<List<SysWorkflowBookmark>> GetByInstanceIdAsync(long instanceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取节点实例的全部书签（按创建时间升序）
    /// </summary>
    /// <param name="nodeInstanceId">节点实例标识</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>书签实体列表</returns>
    Task<List<SysWorkflowBookmark>> GetByNodeInstanceIdAsync(long nodeInstanceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取到期的定时类书签（DueTime 非空且不晚于当前时间，按到期时间升序）
    /// </summary>
    /// <param name="now">当前时间</param>
    /// <param name="maxResultCount">最大返回条数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>到期书签实体列表</returns>
    Task<List<SysWorkflowBookmark>> GetDueAsync(DateTime now, int maxResultCount, CancellationToken cancellationToken = default);

    /// <summary>
    /// 按种类和索引键查询书签（按创建时间升序）
    /// </summary>
    /// <param name="kind">书签种类</param>
    /// <param name="key">索引键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>书签实体列表</returns>
    Task<List<SysWorkflowBookmark>> GetByKindAndKeyAsync(string kind, string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// 查询匹配信号的书签（相关性为空的书签不限相关性；按创建时间升序）
    /// </summary>
    /// <param name="signalName">信号名称</param>
    /// <param name="correlationId">业务相关性标识（为空表示广播）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>书签实体列表</returns>
    Task<List<SysWorkflowBookmark>> GetBySignalAsync(string signalName, string? correlationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除实例的全部书签（物理删除）
    /// </summary>
    /// <param name="instanceId">实例标识</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>任务</returns>
    Task DeleteByInstanceIdAsync(long instanceId, CancellationToken cancellationToken = default);
}
