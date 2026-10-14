// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Saas.Application.Services;

/// <summary>
/// 审批业务服务（业务模块经此发起、撤回审批流并查询进度）
/// </summary>
/// <remarks>
/// 实现由工作流模块提供；工作流模块未加载时默认实现对每个调用都抛出业务异常，业务不会静默跳过审批。
/// 发起与撤回会驱动工作流引擎，应在业务写入之后、同一工作单元内调用：引擎写入随业务事务一起提交或回滚。
/// </remarks>
public interface IWorkflowBusinessService
{
    /// <summary>
    /// 发起审批流（当前登录用户为发起人）
    /// </summary>
    /// <param name="request">发起请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>发起结果</returns>
    /// <exception cref="XiHan.Framework.Core.Exceptions.BusinessException">
    /// 业务未登记、同一单据已有进行中的审批、流程未发布或发起即故障（如首个节点解析不到审批人）时抛出
    /// </exception>
    Task<WorkflowBusinessStartResult> StartAsync(WorkflowBusinessStartRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// 撤回进行中的审批流（业务已自行更新单据状态，不再回调 <see cref="IWorkflowBusinessHandler.OnAbortedAsync"/>）
    /// </summary>
    /// <param name="businessCode">业务编码</param>
    /// <param name="businessId">业务单据标识</param>
    /// <param name="reason">撤回原因</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否撤回了进行中的实例（没有进行中的实例返回 false）</returns>
    Task<bool> WithdrawAsync(string businessCode, string businessId, string? reason, CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量查询单据的审批进度（每张单据取最近一次发起的实例）
    /// </summary>
    /// <param name="businessCode">业务编码</param>
    /// <param name="businessIds">业务单据标识集合</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>单据标识 → 审批进度（从未发起的单据不在结果中）</returns>
    Task<IReadOnlyDictionary<string, WorkflowBusinessProgress>> GetProgressAsync(
        string businessCode,
        IReadOnlyCollection<string> businessIds,
        CancellationToken cancellationToken = default);
}
