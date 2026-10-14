// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#pragma warning disable CS1591

using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Workflow.Domain.Entities;
using XiHan.Framework.Workflow.Abstractions.Runtime;

namespace XiHan.BasicApp.Workflow.Application.Dtos;

/// <summary>
/// 审批中心分页查询 DTO（我的已办 / 抄送我的 / 我发起的，主体服务端锁定为当前用户）
/// </summary>
public sealed class WorkflowApprovalPageQueryDto : BasicAppPRDto
{
    public string? Keyword { get; set; }

    /// <summary>实例状态（为空不过滤）</summary>
    public WorkflowInstanceStatus? Status { get; set; }
}

/// <summary>
/// 审批记录列表项 DTO（我的已办 / 抄送我的）
/// </summary>
public sealed class WorkflowApprovalRecordItemDto : BasicAppDto
{
    public long InstanceId { get; set; }
    public string TaskId { get; set; } = string.Empty;
    public string InstanceName { get; set; } = string.Empty;
    public string DefinitionCode { get; set; } = string.Empty;
    public string NodeId { get; set; } = string.Empty;
    public string NodeName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? CorrelationId { get; set; }
    public WorkflowTaskRecordAction Action { get; set; }
    public string? Outcome { get; set; }
    public string? Comment { get; set; }
    public DateTimeOffset CreatedTime { get; set; }

    /// <summary>业务名称（非审批业务流程为空）</summary>
    public string? BusinessName { get; set; }

    public string? StarterId { get; set; }
    public string? StarterName { get; set; }

    /// <summary>实例当前状态</summary>
    public WorkflowInstanceStatus InstanceStatus { get; set; }

    /// <summary>实例结果</summary>
    public string? InstanceOutcome { get; set; }
}

/// <summary>
/// 我发起的列表项 DTO
/// </summary>
public sealed class WorkflowApprovalInstanceItemDto : BasicAppDto
{
    public string DefinitionCode { get; set; } = string.Empty;
    public int DefinitionVersion { get; set; }
    public string Name { get; set; } = string.Empty;
    public WorkflowInstanceStatus Status { get; set; }
    public string? Outcome { get; set; }
    public string? CorrelationId { get; set; }
    public string? BusinessName { get; set; }
    public DateTime CreationTime { get; set; }
    public DateTime? EndTime { get; set; }
    public string? FaultMessage { get; set; }

    /// <summary>当前待办节点名称</summary>
    public List<string> PendingNodeNames { get; set; } = [];

    /// <summary>当前待办审批人显示名</summary>
    public List<string> PendingAssigneeNames { get; set; } = [];
}

/// <summary>
/// 审批详情 DTO（审批抽屉：单据 + 当前任务 + 审批轨迹）
/// </summary>
public sealed class WorkflowApprovalDetailDto
{
    public long InstanceId { get; set; }
    public string InstanceName { get; set; } = string.Empty;
    public string DefinitionCode { get; set; } = string.Empty;
    public string DefinitionName { get; set; } = string.Empty;
    public int DefinitionVersion { get; set; }
    public WorkflowInstanceStatus Status { get; set; }
    public string? Outcome { get; set; }
    public string? CorrelationId { get; set; }
    public string? StarterId { get; set; }
    public string? StarterName { get; set; }
    public DateTime CreationTime { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public string? FaultMessage { get; set; }
    public string? CancellationReason { get; set; }

    /// <summary>当前用户能否撤回（发起人且流程进行中）</summary>
    public bool CanWithdraw { get; set; }

    /// <summary>审批业务单据（非审批业务流程为空）</summary>
    public WorkflowApprovalBusinessDto? Business { get; set; }

    /// <summary>当前用户待办理的任务（仅从待办打开且任务仍属于当前用户时有值）</summary>
    public WorkflowApprovalTaskDto? Task { get; set; }

    /// <summary>审批轨迹（按时间升序，最后是当前待办）</summary>
    public List<WorkflowApprovalTimelineItemDto> Timeline { get; set; } = [];
}

/// <summary>
/// 审批业务单据 DTO
/// </summary>
public sealed class WorkflowApprovalBusinessDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    /// <summary>单据视图组件（相对前端 src/views 的组件路径）</summary>
    public string FormComponent { get; set; } = string.Empty;

    public string BusinessId { get; set; } = string.Empty;

    /// <summary>单据数据（JSON 文本；单据已不存在时为空）</summary>
    public string? DataJson { get; set; }
}

/// <summary>
/// 待办理任务 DTO
/// </summary>
public sealed class WorkflowApprovalTaskDto
{
    public string TaskId { get; set; } = string.Empty;
    public string NodeId { get; set; } = string.Empty;
    public string NodeName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public DateTime CreationTime { get; set; }

    /// <summary>节点允许的办理结果（为空表示同意/驳回）</summary>
    public List<string> AllowedOutcomes { get; set; } = [];

    /// <summary>节点声明的表单数据（JSON 文本）</summary>
    public string FormDataJson { get; set; } = "{}";
}

/// <summary>
/// 审批轨迹项类型
/// </summary>
public enum WorkflowApprovalTimelineKind
{
    /// <summary>发起</summary>
    Started = 1,

    /// <summary>办理</summary>
    Completed = 2,

    /// <summary>转办</summary>
    Transferred = 3,

    /// <summary>加签</summary>
    AddedAssignees = 4,

    /// <summary>抄送</summary>
    CarbonCopied = 5,

    /// <summary>无审批人自动处理</summary>
    AutoDecided = 6,

    /// <summary>待办理</summary>
    Pending = 7,

    /// <summary>结束（完成/撤回/终止/故障）</summary>
    Finished = 8
}

/// <summary>
/// 审批轨迹项 DTO
/// </summary>
public sealed class WorkflowApprovalTimelineItemDto
{
    public WorkflowApprovalTimelineKind Kind { get; set; }
    public string? NodeId { get; set; }
    public string? NodeName { get; set; }
    public string? UserId { get; set; }
    public string? UserName { get; set; }
    public string? Outcome { get; set; }
    public string? Comment { get; set; }

    /// <summary>转办/加签/抄送的目标人显示名</summary>
    public List<string> TargetUserNames { get; set; } = [];

    /// <summary>结束项的实例状态</summary>
    public WorkflowInstanceStatus? InstanceStatus { get; set; }

    /// <summary>发生时间（待办理项为待办创建时间）</summary>
    public DateTimeOffset? Time { get; set; }
}

/// <summary>
/// 撤回 DTO（发起人撤回进行中的流程）
/// </summary>
public sealed class WorkflowApprovalWithdrawDto
{
    public long InstanceId { get; set; }
    public string? Reason { get; set; }
}

/// <summary>
/// 审批业务类型 DTO（设计器关联业务用）
/// </summary>
public sealed class WorkflowBusinessTypeDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string FormComponent { get; set; } = string.Empty;
    public List<string> Outcomes { get; set; } = [];
    public List<WorkflowBusinessVariableDto> Variables { get; set; } = [];
}

/// <summary>
/// 审批业务流程变量 DTO
/// </summary>
public sealed class WorkflowBusinessVariableDto
{
    public string Name { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string ValueType { get; set; } = string.Empty;
    public string? Description { get; set; }
}

/// <summary>
/// 审批人选择：用户查询 DTO
/// </summary>
public sealed class WorkflowUserOptionQueryDto
{
    public string? Keyword { get; set; }

    /// <summary>按主键取（设计器回显已选用户；传入时忽略关键字）</summary>
    public List<long>? Ids { get; set; }

    public int Limit { get; set; } = 50;
}

/// <summary>
/// 审批人选择：用户选项 DTO
/// </summary>
public sealed class WorkflowUserOptionDto
{
    public long UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Avatar { get; set; }
}

/// <summary>
/// 审批人选择：组织对象选项 DTO（角色 / 岗位，按编码引用）
/// </summary>
public sealed class WorkflowOrganizationOptionDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// 审批人选择：部门树节点 DTO（按编码引用）
/// </summary>
public sealed class WorkflowDepartmentOptionDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public List<WorkflowDepartmentOptionDto> Children { get; set; } = [];
}
