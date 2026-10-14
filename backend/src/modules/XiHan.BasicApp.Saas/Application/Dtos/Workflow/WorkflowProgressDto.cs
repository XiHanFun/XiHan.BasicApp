// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.Framework.Workflow.Abstractions.Runtime;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 业务单据的审批进度 DTO
/// </summary>
public sealed class WorkflowProgressDto
{
    /// <summary>
    /// 流程实例主键
    /// </summary>
    public long InstanceId { get; set; }

    /// <summary>
    /// 实例状态
    /// </summary>
    public WorkflowInstanceStatus Status { get; set; }

    /// <summary>
    /// 流程结果（approved / rejected，未结束时为空）
    /// </summary>
    public string? Outcome { get; set; }

    /// <summary>
    /// 当前待办节点名称
    /// </summary>
    public List<string> PendingNodeNames { get; set; } = [];

    /// <summary>
    /// 当前待办审批人显示名
    /// </summary>
    public List<string> PendingAssigneeNames { get; set; } = [];

    /// <summary>
    /// 故障信息
    /// </summary>
    public string? FaultMessage { get; set; }

    /// <summary>
    /// 开始时间
    /// </summary>
    public DateTimeOffset? StartTime { get; set; }

    /// <summary>
    /// 结束时间
    /// </summary>
    public DateTimeOffset? EndTime { get; set; }
}
