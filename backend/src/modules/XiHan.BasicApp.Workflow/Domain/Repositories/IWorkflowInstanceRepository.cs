#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IWorkflowInstanceRepository
// Guid:80d34c1e-7f92-4b58-a6d0-42e91c73f5b8
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/17 10:06:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.BasicApp.Workflow.Domain.Entities;
using XiHan.Framework.Workflow.Abstractions.Runtime;

namespace XiHan.BasicApp.Workflow.Domain.Repositories;

/// <summary>
/// 工作流实例仓储接口
/// </summary>
public interface IWorkflowInstanceRepository : ISaasRepository<SysWorkflowInstance>
{
    /// <summary>
    /// 获取实例的直接子实例列表（按创建时间升序）
    /// </summary>
    /// <param name="parentInstanceId">父实例标识</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>子实例实体列表</returns>
    Task<List<SysWorkflowInstance>> GetChildrenAsync(long parentInstanceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 查询实例列表（按创建时间降序）
    /// </summary>
    /// <param name="status">状态（为空表示不过滤）</param>
    /// <param name="definitionCode">定义编码（为空表示不过滤）</param>
    /// <param name="correlationId">业务相关性标识（为空表示不过滤）</param>
    /// <param name="maxResultCount">最大返回条数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>实例实体列表</returns>
    Task<List<SysWorkflowInstance>> GetInstanceListAsync(
        WorkflowInstanceStatus? status,
        string? definitionCode,
        string? correlationId,
        int maxResultCount,
        CancellationToken cancellationToken = default);
}
