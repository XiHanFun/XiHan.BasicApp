// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Core.Entities;
using XiHan.Framework.Workflow.Abstractions.Runtime;

namespace XiHan.BasicApp.Workflow.Domain.Entities;

/// <summary>
/// 系统工作流实例表
/// </summary>
/// <remarks>
/// 引擎运行时行（硬删；主键 = 引擎实例标识）：真源为 <see cref="InstanceJson"/>，
/// 其余列是分页/检索用投影。引擎对同一实例的写入已由实例级分布式锁串行化。
/// </remarks>
[SugarTable(TableName = "Sys_Workflow_Instance", TableDescription = "系统工作流实例表")]
[SugarIndex("IX_{table}_TeId_CrTi", nameof(TenantId), OrderByType.Asc, nameof(CreationTime), OrderByType.Desc)]
[SugarIndex("IX_{table}_Co_St", nameof(DefinitionCode), OrderByType.Asc, nameof(Status), OrderByType.Asc)]
[SugarIndex("IX_{table}_PaIn", nameof(ParentInstanceId), OrderByType.Asc)]
[SugarIndex("IX_{table}_CoId", nameof(CorrelationId), OrderByType.Asc)]
public partial class SysWorkflowInstance : BasicAppEntity
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public SysWorkflowInstance()
    {
    }

    /// <summary>
    /// 构造函数（主键 = 引擎实例标识）
    /// </summary>
    /// <param name="basicId"></param>
    public SysWorkflowInstance(long basicId)
        : base(basicId)
    {
    }

    /// <summary>
    /// 定义标识
    /// </summary>
    [SugarColumn(ColumnName = "Definition_Id", ColumnDescription = "定义标识", IsNullable = false)]
    public virtual long DefinitionId { get; set; }

    /// <summary>
    /// 定义编码
    /// </summary>
    [SugarColumn(ColumnName = "Definition_Code", ColumnDescription = "定义编码", Length = 100, IsNullable = false)]
    public virtual string DefinitionCode { get; set; } = string.Empty;

    /// <summary>
    /// 定义版本
    /// </summary>
    [SugarColumn(ColumnName = "Definition_Version", ColumnDescription = "定义版本", IsNullable = false)]
    public virtual int DefinitionVersion { get; set; }

    /// <summary>
    /// 实例名称
    /// </summary>
    [SugarColumn(ColumnName = "Name", ColumnDescription = "实例名称", Length = 200, IsNullable = false)]
    public virtual string Name { get; set; } = string.Empty;

    /// <summary>
    /// 实例状态
    /// </summary>
    [SugarColumn(ColumnName = "Status", ColumnDescription = "实例状态", IsNullable = false)]
    public virtual WorkflowInstanceStatus Status { get; set; } = WorkflowInstanceStatus.Running;

    /// <summary>
    /// 业务相关性标识
    /// </summary>
    [SugarColumn(ColumnName = "Correlation_Id", ColumnDescription = "业务相关性标识", Length = 100, IsNullable = true)]
    public virtual string? CorrelationId { get; set; }

    /// <summary>
    /// 发起人标识
    /// </summary>
    [SugarColumn(ColumnName = "Starter_Id", ColumnDescription = "发起人标识", Length = 100, IsNullable = true)]
    public virtual string? StarterId { get; set; }

    /// <summary>
    /// 父实例标识
    /// </summary>
    [SugarColumn(ColumnName = "Parent_Instance_Id", ColumnDescription = "父实例标识", IsNullable = true)]
    public virtual long? ParentInstanceId { get; set; }

    /// <summary>
    /// 实例深度
    /// </summary>
    [SugarColumn(ColumnName = "Depth", ColumnDescription = "实例深度", IsNullable = false)]
    public virtual int Depth { get; set; }

    /// <summary>
    /// 创建时间（引擎时钟）
    /// </summary>
    [SugarColumn(ColumnName = "Creation_Time", ColumnDescription = "创建时间", IsNullable = false)]
    public virtual DateTime CreationTime { get; set; }

    /// <summary>
    /// 开始时间
    /// </summary>
    [SugarColumn(ColumnName = "Start_Time", ColumnDescription = "开始时间", IsNullable = true)]
    public virtual DateTime? StartTime { get; set; }

    /// <summary>
    /// 结束时间
    /// </summary>
    [SugarColumn(ColumnName = "End_Time", ColumnDescription = "结束时间", IsNullable = true)]
    public virtual DateTime? EndTime { get; set; }

    /// <summary>
    /// 故障节点标识
    /// </summary>
    [SugarColumn(ColumnName = "Fault_Node_Id", ColumnDescription = "故障节点标识", Length = 100, IsNullable = true)]
    public virtual string? FaultNodeId { get; set; }

    /// <summary>
    /// 故障信息
    /// </summary>
    [SugarColumn(ColumnName = "Fault_Message", ColumnDescription = "故障信息", Length = 2000, IsNullable = true)]
    public virtual string? FaultMessage { get; set; }

    /// <summary>
    /// 实例完整 JSON（真源，含变量/汇聚波次/父子链接）
    /// </summary>
    [SugarColumn(ColumnName = "Instance_Json", ColumnDescription = "实例完整JSON", ColumnDataType = StaticConfig.CodeFirst_BigString, IsNullable = false)]
    public virtual string InstanceJson { get; set; } = string.Empty;
}
