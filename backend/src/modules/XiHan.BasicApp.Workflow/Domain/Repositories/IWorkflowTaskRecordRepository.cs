// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.BasicApp.Workflow.Domain.Entities;

namespace XiHan.BasicApp.Workflow.Domain.Repositories;

/// <summary>
/// 工作流审批记录仓储接口
/// </summary>
public interface IWorkflowTaskRecordRepository : ISaasRepository<SysWorkflowTaskRecord>
{
    /// <summary>
    /// 获取实例的全部审批记录（按记录时间升序）
    /// </summary>
    /// <param name="instanceId">实例标识</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>审批记录列表</returns>
    Task<List<SysWorkflowTaskRecord>> GetByInstanceIdAsync(long instanceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 用户是否参与过实例（办理、转办、加签或被抄送）
    /// </summary>
    /// <param name="instanceId">实例标识</param>
    /// <param name="userId">用户主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>参与过返回 true</returns>
    Task<bool> HasParticipatedAsync(long instanceId, long userId, CancellationToken cancellationToken = default);
}
