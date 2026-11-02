// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using XiHan.BasicApp.Workflow.Domain.Entities;
using XiHan.Framework.Workflow.Abstractions.Instances;

namespace XiHan.BasicApp.Workflow.Operations.Approvals;

/// <summary>
/// 我已审批、抄送我的里的一条。
/// </summary>
/// <param name="Id">记录 ID。</param>
/// <param name="InstanceId">流程实例。</param>
/// <param name="TaskId">任务。</param>
/// <param name="InstanceTitle">实例标题。</param>
/// <param name="DefinitionName">流程定义名。</param>
/// <param name="NodeId">节点 ID。</param>
/// <param name="NodeName">节点名。</param>
/// <param name="Title">任务标题。</param>
/// <param name="CorrelationId">关联 ID。</param>
/// <param name="Action">动作。</param>
/// <param name="Outcome">办理的结论。</param>
/// <param name="Comment">意见。</param>
/// <param name="CreatedAt">时间。</param>
/// <param name="BusinessNameKey">审批业务显示名的文案键（不是审批业务的顶层实例为空）。</param>
/// <param name="StarterId">发起人。</param>
/// <param name="StarterName">发起人显示名。</param>
/// <param name="InstanceStatus">实例现在的状态（实例已清理时为空）。</param>
/// <param name="InstanceOutcome">实例的结论。</param>
public sealed record WorkflowApprovalRecordResponse(
    Guid Id,
    Guid InstanceId,
    Guid TaskId,
    string InstanceTitle,
    string DefinitionName,
    string NodeId,
    string NodeName,
    string Title,
    string? CorrelationId,
    WorkflowTaskRecordAction Action,
    string? Outcome,
    string? Comment,
    DateTimeOffset CreatedAt,
    string? BusinessNameKey,
    Guid? StarterId,
    string? StarterName,
    WorkflowInstanceStatus? InstanceStatus,
    string? InstanceOutcome);

/// <summary>
/// 我发起的里的一条。
/// </summary>
/// <param name="Id">流程实例。</param>
/// <param name="DefinitionName">流程定义名。</param>
/// <param name="DefinitionVersion">定义版本。</param>
/// <param name="Title">实例标题。</param>
/// <param name="Status">状态。</param>
/// <param name="Outcome">结论。</param>
/// <param name="CorrelationId">关联 ID。</param>
/// <param name="BusinessNameKey">审批业务显示名的文案键。</param>
/// <param name="CreatedAt">发起时间。</param>
/// <param name="EndedAt">结束时间。</param>
/// <param name="FaultMessage">故障原因。</param>
/// <param name="PendingNodeNames">运行中时等着办的节点。</param>
/// <param name="PendingAssigneeNames">运行中时等着办的人。</param>
public sealed record WorkflowApprovalInstanceResponse(
    Guid Id,
    string DefinitionName,
    int DefinitionVersion,
    string Title,
    WorkflowInstanceStatus Status,
    string? Outcome,
    string? CorrelationId,
    string? BusinessNameKey,
    DateTimeOffset CreatedAt,
    DateTimeOffset? EndedAt,
    string? FaultMessage,
    IReadOnlyList<string> PendingNodeNames,
    IReadOnlyList<string> PendingAssigneeNames);

/// <summary>
/// 审批详情。
/// </summary>
/// <param name="Id">流程实例。</param>
/// <param name="TaskId">只看调用方自己的这条待办（不给即调用方在该实例里的第一条）。</param>
public sealed record GetApprovalRequest(Guid Id, Guid? TaskId = null);

/// <summary>
/// 审批详情。
/// </summary>
/// <param name="InstanceId">流程实例。</param>
/// <param name="Title">实例标题。</param>
/// <param name="DefinitionName">流程定义名。</param>
/// <param name="DefinitionDisplayName">定义的显示名（没有时是定义名）。</param>
/// <param name="DefinitionVersion">定义版本。</param>
/// <param name="Status">状态。</param>
/// <param name="Outcome">结论。</param>
/// <param name="CorrelationId">关联 ID。</param>
/// <param name="StarterId">发起人。</param>
/// <param name="StarterName">发起人显示名。</param>
/// <param name="CreatedAt">发起时间。</param>
/// <param name="StartedAt">开始执行的时间。</param>
/// <param name="EndedAt">结束时间。</param>
/// <param name="FaultMessage">故障原因。</param>
/// <param name="CancellationReason">取消或终止的原因。</param>
/// <param name="CanWithdraw">调用方能否撤回（发起人，且实例运行中、挂起或故障）。</param>
/// <param name="Business">审批业务的单据（不是审批业务的顶层实例为空）。</param>
/// <param name="Task">调用方在这个实例里的待办（没有时为空）。</param>
/// <param name="Timeline">审批轨迹：发起、办理 / 转办 / 加签 / 抄送与无人时的自动处理（按时间）、结束或当前待办。</param>
public sealed record WorkflowApprovalDetailResponse(
    Guid InstanceId,
    string Title,
    string DefinitionName,
    string DefinitionDisplayName,
    int DefinitionVersion,
    WorkflowInstanceStatus Status,
    string? Outcome,
    string? CorrelationId,
    Guid? StarterId,
    string? StarterName,
    DateTimeOffset CreatedAt,
    DateTimeOffset? StartedAt,
    DateTimeOffset? EndedAt,
    string? FaultMessage,
    string? CancellationReason,
    bool CanWithdraw,
    WorkflowApprovalBusinessResponse? Business,
    WorkflowApprovalTaskResponse? Task,
    IReadOnlyList<WorkflowApprovalTimelineItem> Timeline);

/// <summary>
/// 审批业务的单据。
/// </summary>
/// <param name="Code">业务码。</param>
/// <param name="NameKey">显示名的文案键。</param>
/// <param name="FormComponent">单据组件路径。</param>
/// <param name="BusinessId">单据 ID。</param>
/// <param name="Data">单据数据（JSON 文本；单据已不存在时为空）。</param>
public sealed record WorkflowApprovalBusinessResponse(string Code, string NameKey, string FormComponent, string BusinessId, string? Data);

/// <summary>
/// 调用方的待办。
/// </summary>
/// <param name="TaskId">任务 ID。</param>
/// <param name="NodeId">节点 ID。</param>
/// <param name="NodeName">节点名。</param>
/// <param name="Title">任务标题。</param>
/// <param name="CreatedAt">到达时间。</param>
/// <param name="AllowedOutcomes">节点允许的结论（没写时为空，前端按同意 / 驳回）。</param>
/// <param name="FormData">任务表单数据（JSON 对象文本）。</param>
public sealed record WorkflowApprovalTaskResponse(Guid TaskId, string NodeId, string NodeName, string Title, DateTimeOffset CreatedAt, IReadOnlyList<string> AllowedOutcomes, string FormData);

/// <summary>
/// 审批轨迹的一项。
/// </summary>
/// <param name="Kind">种类。</param>
/// <param name="NodeId">节点 ID。</param>
/// <param name="NodeName">节点名。</param>
/// <param name="UserId">办理人、被抄送人或待办人。</param>
/// <param name="UserName">显示名。</param>
/// <param name="Outcome">结论。</param>
/// <param name="Comment">意见、故障或取消的原因。</param>
/// <param name="TargetUserNames">转办与加签的目标。</param>
/// <param name="InstanceStatus">结束时实例的状态。</param>
/// <param name="Time">时间。</param>
public sealed record WorkflowApprovalTimelineItem(
    WorkflowApprovalTimelineKind Kind,
    string? NodeId,
    string? NodeName,
    Guid? UserId,
    string? UserName,
    string? Outcome,
    string? Comment,
    IReadOnlyList<string> TargetUserNames,
    WorkflowInstanceStatus? InstanceStatus,
    DateTimeOffset? Time);

/// <summary>
/// 审批轨迹的种类。
/// </summary>
public enum WorkflowApprovalTimelineKind
{
    /// <summary>
    /// 发起。
    /// </summary>
    Started = 1,

    /// <summary>
    /// 办理。
    /// </summary>
    Completed = 2,

    /// <summary>
    /// 转办。
    /// </summary>
    Transferred = 3,

    /// <summary>
    /// 加签。
    /// </summary>
    AddedAssignees = 4,

    /// <summary>
    /// 抄送。
    /// </summary>
    CarbonCopied = 5,

    /// <summary>
    /// 找不到人时按策略自动通过或驳回。
    /// </summary>
    AutoDecided = 6,

    /// <summary>
    /// 等着办。
    /// </summary>
    Pending = 7,

    /// <summary>
    /// 结束。
    /// </summary>
    Finished = 8,
}

/// <summary>
/// 撤回。
/// </summary>
/// <param name="Id">流程实例。</param>
/// <param name="Reason">原因（不写即「发起人撤回」）。</param>
public sealed record WithdrawApprovalRequest(Guid Id, [property: StringLength(500)] string? Reason = null);
