// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using SqlSugar;
using XiHan.BasicApp.Contracts.Entities;

namespace XiHan.BasicApp.Workflow.Domain.Entities;

/// <summary>
/// 审批记录（租户库实体）
/// </summary>
/// <remarks>
/// 照 4.x 的 <c>SysWorkflowTaskRecord</c>：办理、转办、加签各写一行（归办理人），抄送每个被抄送人一行；只追加不改
/// 「我已审批」「抄送我的」与审批轨迹都读它，引擎表里没有这些动作的历史。平台一侧的记录租户为 <c>platform</c>
/// 任务、节点与实例的名字按写入时的快照存，定义改了、实例清理了也照常显示
/// </remarks>
[SugarIndex("ix_{table}_tenant_id_user_id_action", nameof(TenantId), OrderByType.Asc, nameof(UserId), OrderByType.Asc, nameof(Action), OrderByType.Asc, nameof(CreatedAt), OrderByType.Desc)]
[SugarIndex("ix_{table}_tenant_id_instance_id", nameof(TenantId), OrderByType.Asc, nameof(InstanceId), OrderByType.Asc, nameof(CreatedAt), OrderByType.Asc)]
public sealed class SysWorkflowTaskRecord : TenantScopedRecord
{
    /// <summary>
    /// 流程实例
    /// </summary>
    public Guid InstanceId { get; set; }

    /// <summary>
    /// 任务（框架的用户任务书签）
    /// </summary>
    public Guid TaskId { get; set; }

    /// <summary>
    /// 节点 ID
    /// </summary>
    [StringLength(64)]
    public string NodeId { get; set; } = string.Empty;

    /// <summary>
    /// 节点显示名的快照
    /// </summary>
    [StringLength(200)]
    public string NodeName { get; set; } = string.Empty;

    /// <summary>
    /// 流程定义名
    /// </summary>
    [StringLength(128)]
    public string DefinitionName { get; set; } = string.Empty;

    /// <summary>
    /// 实例标题的快照
    /// </summary>
    [StringLength(200)]
    public string InstanceTitle { get; set; } = string.Empty;

    /// <summary>
    /// 任务标题的快照
    /// </summary>
    [StringLength(500)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 关联 ID（审批业务的单据 ID）
    /// </summary>
    [StringLength(128)]
    public string? CorrelationId { get; set; }

    /// <summary>
    /// 记录归属的用户：办理人或被抄送人
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// 动作
    /// </summary>
    public WorkflowTaskRecordAction Action { get; set; }

    /// <summary>
    /// 办理的结论（只有办理有）
    /// </summary>
    [StringLength(64)]
    public string? Outcome { get; set; }

    /// <summary>
    /// 意见
    /// </summary>
    [StringLength(2000)]
    public string? Comment { get; set; }

    /// <summary>
    /// 转办与加签的目标用户（逗号分隔的用户 ID）
    /// </summary>
    [StringLength(1000)]
    public string? TargetUserIds { get; set; }
}

/// <summary>
/// 审批记录的动作
/// </summary>
public enum WorkflowTaskRecordAction
{
    /// <summary>
    /// 办理（同意、驳回或业务自定义的结论）
    /// </summary>
    Completed = 1,

    /// <summary>
    /// 转办
    /// </summary>
    Transferred = 2,

    /// <summary>
    /// 加签
    /// </summary>
    AddedAssignees = 3,

    /// <summary>
    /// 抄送
    /// </summary>
    CarbonCopied = 4,
}
