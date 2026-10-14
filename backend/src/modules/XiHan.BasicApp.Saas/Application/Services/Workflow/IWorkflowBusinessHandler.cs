// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Saas.Application.Services;

/// <summary>
/// 审批业务处理器（每个接入审批流的业务实现一个，注册为瞬时服务）
/// </summary>
/// <remarks>
/// 业务以 <see cref="WorkflowBusinessDescriptor.Code"/> 与流程定义编码一一绑定：发起时按编码取最新已发布版本，
/// 单据主键写入实例的业务相关性标识。工作流模块在以下时机回调本处理器：
/// <list type="bullet">
/// <item>审批人办理待办之前（<see cref="OnTaskCompletingAsync"/>，审批人请求上下文内，抛异常即阻断办理）；</item>
/// <item>流程走到结果结束节点时（<see cref="OnOutcomeAsync"/>，失败则实例停在故障态，重试再次调用）；</item>
/// <item>流程被撤回、取消或终止之后（<see cref="OnAbortedAsync"/>）；</item>
/// <item>审批人或参与人查看单据时（<see cref="GetFormDataAsync"/>，由工作流按参与关系鉴权，业务不必再开权限）。</item>
/// </list>
/// 除 <see cref="GetFormDataAsync"/> 外的回调都必须幂等。
/// </remarks>
public interface IWorkflowBusinessHandler
{
    /// <summary>
    /// 业务描述
    /// </summary>
    WorkflowBusinessDescriptor Descriptor { get; }

    /// <summary>
    /// 办理前校验（只读校验，不写数据；抛出异常即拒绝本次办理，待办保持不变）
    /// </summary>
    /// <param name="context">办理上下文</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>任务</returns>
    Task OnTaskCompletingAsync(WorkflowBusinessTaskContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// 流程得出结论（结果结束节点执行时调用；在工作单元内执行，抛出异常即回滚本次写入、实例进入故障态）
    /// </summary>
    /// <param name="context">结论上下文</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>任务</returns>
    Task OnOutcomeAsync(WorkflowBusinessOutcomeContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// 流程被撤回、取消或终止（审批中心撤回、管理员取消/终止实例后调用；业务自己发起的撤回不会回调）
    /// </summary>
    /// <param name="context">中止上下文</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>任务</returns>
    Task OnAbortedAsync(WorkflowBusinessAbortedContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// 读取单据数据（审批抽屉里的业务表单据此渲染；单据不存在返回 null）
    /// </summary>
    /// <param name="businessId">业务单据标识</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>可 JSON 序列化的单据视图</returns>
    Task<object?> GetFormDataAsync(string businessId, CancellationToken cancellationToken = default);
}
