// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Contracts.Entities;

namespace XiHan.BasicApp.Workflow.Domain.Entities;

/// <summary>
/// 审批记录（租户库实体）
/// </summary>
/// <remarks>
/// 办理、转办、加签各写一行（归办理人），抄送每个被抄送人一行；只追加不改
/// 「我已审批」「抄送我的」与审批轨迹都读它，引擎表里没有这些动作的历史。平台一侧的记录租户为 <c>platform</c>
/// 任务、节点与实例的名字按写入时的快照存，定义改了、实例清理了也照常显示
/// </remarks>
[SugarTable(TableName = "sys_workflow_task_record", TableDescription = "审批记录（租户库实体）")]
[SugarIndex("ix_sys_workflow_task_record_tenant_id_user_id_action", nameof(TenantId), OrderByType.Asc, nameof(UserId), OrderByType.Asc, nameof(Action), OrderByType.Asc, nameof(CreatedAt), OrderByType.Desc)]
[SugarIndex("ix_sys_workflow_task_record_tenant_id_instance_id", nameof(TenantId), OrderByType.Asc, nameof(InstanceId), OrderByType.Asc, nameof(CreatedAt), OrderByType.Asc)]
public sealed class SysWorkflowTaskRecord : TenantScopedRecord
{
    /// <summary>
    /// 流程实例
    /// </summary>
    [SugarColumn(ColumnName = "instance_id", IsNullable = false, ColumnDescription = "流程实例")]
    public Guid InstanceId { get; set; }

    /// <summary>
    /// 任务（框架的用户任务书签）
    /// </summary>
    [SugarColumn(ColumnName = "task_id", IsNullable = false, ColumnDescription = "任务（框架的用户任务书签）")]
    public Guid TaskId { get; set; }

    /// <summary>
    /// 节点 ID
    /// </summary>
    [SugarColumn(ColumnName = "node_id", Length = 64, IsNullable = false, ColumnDescription = "节点 ID")]
    public string NodeId { get; set; } = string.Empty;

    /// <summary>
    /// 节点显示名的快照
    /// </summary>
    [SugarColumn(ColumnName = "node_name", Length = 200, IsNullable = false, ColumnDescription = "节点显示名的快照")]
    public string NodeName { get; set; } = string.Empty;

    /// <summary>
    /// 流程定义名
    /// </summary>
    [SugarColumn(ColumnName = "definition_name", Length = 128, IsNullable = false, ColumnDescription = "流程定义名")]
    public string DefinitionName { get; set; } = string.Empty;

    /// <summary>
    /// 实例标题的快照
    /// </summary>
    [SugarColumn(ColumnName = "instance_title", Length = 200, IsNullable = false, ColumnDescription = "实例标题的快照")]
    public string InstanceTitle { get; set; } = string.Empty;

    /// <summary>
    /// 任务标题的快照
    /// </summary>
    [SugarColumn(ColumnName = "title", Length = 500, IsNullable = false, ColumnDescription = "任务标题的快照")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 关联 ID（审批业务的单据 ID）
    /// </summary>
    [SugarColumn(ColumnName = "correlation_id", Length = 128, IsNullable = true, ColumnDescription = "关联 ID（审批业务的单据 ID）")]
    public string? CorrelationId { get; set; }

    /// <summary>
    /// 记录归属的用户：办理人或被抄送人
    /// </summary>
    [SugarColumn(ColumnName = "user_id", IsNullable = false, ColumnDescription = "记录归属的用户：办理人或被抄送人")]
    public Guid UserId { get; set; }

    /// <summary>
    /// 动作
    /// </summary>
    [SugarColumn(ColumnName = "action", IsNullable = false, ColumnDescription = "动作")]
    public WorkflowTaskRecordAction Action { get; set; }

    /// <summary>
    /// 办理的结论（只有办理有）
    /// </summary>
    [SugarColumn(ColumnName = "outcome", Length = 64, IsNullable = true, ColumnDescription = "办理的结论（只有办理有）")]
    public string? Outcome { get; set; }

    /// <summary>
    /// 意见
    /// </summary>
    [SugarColumn(ColumnName = "comment", Length = 2000, IsNullable = true, ColumnDescription = "意见")]
    public string? Comment { get; set; }

    /// <summary>
    /// 转办与加签的目标用户（逗号分隔的用户 ID）
    /// </summary>
    [SugarColumn(ColumnName = "target_user_ids", Length = 1000, IsNullable = true, ColumnDescription = "转办与加签的目标用户（逗号分隔的用户 ID）")]
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
