// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Core.Entities;
using XiHan.Framework.Domain.Entities.Abstracts;

namespace XiHan.BasicApp.Workflow.Domain.Entities;

/// <summary>
/// 系统工作流审批记录表
/// </summary>
/// <remarks>
/// 记录「人」与审批任务之间发生过的事：办理（同意/驳回/自定义结果）、转办、加签、被抄送。
/// 引擎的待办书签办理即消失、节点私有状态不可检索，「我的已办」「抄送我的」与审批轨迹都从本表读取。
/// 只增不改不删；一次动作一行，<see cref="UserId"/> 是办理人（抄送行为被抄送人）。
///
/// 租户隔离：严格——与流程实例同租户。
/// </remarks>
[SugarTable(TableName = "Sys_Workflow_Task_Record", TableDescription = "系统工作流审批记录表")]
[SugarIndex("IX_{table}_TeId_UsId_Ac", nameof(TenantId), OrderByType.Asc, nameof(UserId), OrderByType.Asc, nameof(Action), OrderByType.Asc)]
[SugarIndex("IX_{table}_InId_CrTi", nameof(InstanceId), OrderByType.Asc, nameof(CreatedTime), OrderByType.Asc)]
public partial class SysWorkflowTaskRecord : BasicAppCreationEntity, IStrictMultiTenantEntity
{
    /// <summary>
    /// 流程实例标识
    /// </summary>
    [SugarColumn(ColumnName = "Instance_Id", ColumnDescription = "流程实例标识", IsNullable = false)]
    public virtual long InstanceId { get; set; }

    /// <summary>
    /// 任务标识（待办书签标识）
    /// </summary>
    [SugarColumn(ColumnName = "Task_Id", ColumnDescription = "任务标识", IsNullable = false)]
    public virtual long TaskId { get; set; }

    /// <summary>
    /// 节点标识
    /// </summary>
    [SugarColumn(ColumnName = "Node_Id", ColumnDescription = "节点标识", Length = 100, IsNullable = false)]
    public virtual string NodeId { get; set; } = string.Empty;

    /// <summary>
    /// 节点名称（记录时的快照）
    /// </summary>
    [SugarColumn(ColumnName = "Node_Name", ColumnDescription = "节点名称", Length = 200, IsNullable = false)]
    public virtual string NodeName { get; set; } = string.Empty;

    /// <summary>
    /// 定义编码
    /// </summary>
    [SugarColumn(ColumnName = "Definition_Code", ColumnDescription = "定义编码", Length = 100, IsNullable = false)]
    public virtual string DefinitionCode { get; set; } = string.Empty;

    /// <summary>
    /// 实例名称（记录时的快照）
    /// </summary>
    [SugarColumn(ColumnName = "Instance_Name", ColumnDescription = "实例名称", Length = 200, IsNullable = false)]
    public virtual string InstanceName { get; set; } = string.Empty;

    /// <summary>
    /// 任务标题（记录时的快照）
    /// </summary>
    [SugarColumn(ColumnName = "Title", ColumnDescription = "任务标题", Length = 500, IsNullable = false)]
    public virtual string Title { get; set; } = string.Empty;

    /// <summary>
    /// 业务相关性标识
    /// </summary>
    [SugarColumn(ColumnName = "Correlation_Id", ColumnDescription = "业务相关性标识", Length = 100, IsNullable = true)]
    public virtual string? CorrelationId { get; set; }

    /// <summary>
    /// 用户主键（办理人；抄送记录为被抄送人）
    /// </summary>
    [SugarColumn(ColumnName = "User_Id", ColumnDescription = "用户主键", IsNullable = false)]
    public virtual long UserId { get; set; }

    /// <summary>
    /// 动作
    /// </summary>
    [SugarColumn(ColumnName = "Action", ColumnDescription = "动作", IsNullable = false)]
    public virtual WorkflowTaskRecordAction Action { get; set; }

    /// <summary>
    /// 办理结果（办理动作填写）
    /// </summary>
    [SugarColumn(ColumnName = "Outcome", ColumnDescription = "办理结果", Length = 100, IsNullable = true)]
    public virtual string? Outcome { get; set; }

    /// <summary>
    /// 意见
    /// </summary>
    [SugarColumn(ColumnName = "Comment", ColumnDescription = "意见", Length = 2000, IsNullable = true)]
    public virtual string? Comment { get; set; }

    /// <summary>
    /// 目标用户主键（转办为新受理人、加签为新增受理人，逗号分隔）
    /// </summary>
    [SugarColumn(ColumnName = "Target_User_Ids", ColumnDescription = "目标用户主键", Length = 1000, IsNullable = true)]
    public virtual string? TargetUserIds { get; set; }
}
