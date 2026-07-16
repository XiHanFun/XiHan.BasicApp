#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IWorkflowNodeInstanceRepository
// Guid:5f60a2d8-c397-4e15-b0a4-81d72c95e6f3
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/17 10:07:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.BasicApp.Workflow.Domain.Entities;

namespace XiHan.BasicApp.Workflow.Domain.Repositories;

/// <summary>
/// 工作流节点实例仓储接口
/// </summary>
public interface IWorkflowNodeInstanceRepository : ISaasRepository<SysWorkflowNodeInstance>
{
    /// <summary>
    /// 获取实例的节点实例列表（按开始时间升序，同刻按主键升序）
    /// </summary>
    /// <param name="instanceId">实例标识</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>节点实例实体列表</returns>
    Task<List<SysWorkflowNodeInstance>> GetByInstanceIdAsync(long instanceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除实例的全部节点实例（物理删除）
    /// </summary>
    /// <param name="instanceId">实例标识</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>任务</returns>
    Task DeleteByInstanceIdAsync(long instanceId, CancellationToken cancellationToken = default);
}
