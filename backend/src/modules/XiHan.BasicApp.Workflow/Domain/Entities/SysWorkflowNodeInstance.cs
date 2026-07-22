// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Core.Entities;
using XiHan.Framework.Workflow.Abstractions.Runtime;

namespace XiHan.BasicApp.Workflow.Domain.Entities;

/// <summary>
/// 系统工作流节点实例表
/// </summary>
/// <remarks>
/// 引擎运行时行（硬删；主键 = 引擎节点实例标识）：真源为 <see cref="NodeInstanceJson"/>，
/// 其余列是执行历史检索用投影。
/// </remarks>
[SugarTable(TableName = "Sys_Workflow_Node_Instance", TableDescription = "系统工作流节点实例表")]
[SugarIndex("IX_{table}_InId_StTi", nameof(InstanceId), OrderByType.Asc, nameof(StartTime), OrderByType.Asc)]
[SugarIndex("IX_{table}_TeId", nameof(TenantId), OrderByType.Asc)]
public partial class SysWorkflowNodeInstance : BasicAppEntity
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public SysWorkflowNodeInstance()
    {
    }

    /// <summary>
    /// 构造函数（主键 = 引擎节点实例标识）
    /// </summary>
    /// <param name="basicId"></param>
    public SysWorkflowNodeInstance(long basicId)
        : base(basicId)
    {
    }

    /// <summary>
    /// 所属流程实例标识
    /// </summary>
    [SugarColumn(ColumnName = "Instance_Id", ColumnDescription = "所属流程实例标识", IsNullable = false)]
    public virtual long InstanceId { get; set; }

    /// <summary>
    /// 节点标识
    /// </summary>
    [SugarColumn(ColumnName = "Node_Id", ColumnDescription = "节点标识", Length = 100, IsNullable = false)]
    public virtual string NodeId { get; set; } = string.Empty;

    /// <summary>
    /// 活动类型编码
    /// </summary>
    [SugarColumn(ColumnName = "Activity_Type", ColumnDescription = "活动类型编码", Length = 100, IsNullable = false)]
    public virtual string ActivityType { get; set; } = string.Empty;

    /// <summary>
    /// 节点实例状态
    /// </summary>
    [SugarColumn(ColumnName = "Status", ColumnDescription = "节点实例状态", IsNullable = false)]
    public virtual WorkflowNodeInstanceStatus Status { get; set; } = WorkflowNodeInstanceStatus.Running;

    /// <summary>
    /// 开始时间
    /// </summary>
    [SugarColumn(ColumnName = "Start_Time", ColumnDescription = "开始时间", IsNullable = false)]
    public virtual DateTime StartTime { get; set; }

    /// <summary>
    /// 结束时间
    /// </summary>
    [SugarColumn(ColumnName = "End_Time", ColumnDescription = "结束时间", IsNullable = true)]
    public virtual DateTime? EndTime { get; set; }

    /// <summary>
    /// 节点实例完整 JSON（真源，含输入/输出/活动私有状态）
    /// </summary>
    [SugarColumn(ColumnName = "Node_Instance_Json", ColumnDescription = "节点实例完整JSON", ColumnDataType = StaticConfig.CodeFirst_BigString, IsNullable = false)]
    public virtual string NodeInstanceJson { get; set; } = string.Empty;
}
