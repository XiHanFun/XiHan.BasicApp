#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysWorkflowBookmark
// Guid:e72a05c8-914d-4b60-bf83-26d19e05c7a4
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/17 10:04:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Core.Entities;

namespace XiHan.BasicApp.Workflow.Domain.Entities;

/// <summary>
/// 系统工作流书签表
/// </summary>
/// <remarks>
/// 引擎运行时行（硬删；主键 = 引擎书签标识）：真源为 <see cref="BookmarkJson"/>，
/// 其余列是待办/信号/到期轮询检索用投影。
/// </remarks>
[SugarTable(TableName = "Sys_Workflow_Bookmark", TableDescription = "系统工作流书签表")]
[SugarIndex("IX_{table}_InId", nameof(InstanceId), OrderByType.Asc)]
[SugarIndex("IX_{table}_NoIn", nameof(NodeInstanceId), OrderByType.Asc)]
[SugarIndex("IX_{table}_Ki_Ke", nameof(Kind), OrderByType.Asc, nameof(Key), OrderByType.Asc)]
[SugarIndex("IX_{table}_DuTi", nameof(DueTime), OrderByType.Asc)]
public partial class SysWorkflowBookmark : BasicAppEntity
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public SysWorkflowBookmark()
    {
    }

    /// <summary>
    /// 构造函数（主键 = 引擎书签标识）
    /// </summary>
    /// <param name="basicId"></param>
    public SysWorkflowBookmark(long basicId)
        : base(basicId)
    {
    }

    /// <summary>
    /// 所属流程实例标识
    /// </summary>
    [SugarColumn(ColumnName = "Instance_Id", ColumnDescription = "所属流程实例标识", IsNullable = false)]
    public virtual long InstanceId { get; set; }

    /// <summary>
    /// 节点实例标识
    /// </summary>
    [SugarColumn(ColumnName = "Node_Instance_Id", ColumnDescription = "节点实例标识", IsNullable = false)]
    public virtual long NodeInstanceId { get; set; }

    /// <summary>
    /// 书签种类（UserTask/Timer/Signal/SubWorkflow/Retry/NodeTimeout）
    /// </summary>
    [SugarColumn(ColumnName = "Kind", ColumnDescription = "书签种类", Length = 50, IsNullable = false)]
    public virtual string Kind { get; set; } = string.Empty;

    /// <summary>
    /// 索引键（受理人标识/信号名称/父节点实例标识）
    /// </summary>
    [SugarColumn(ColumnName = "Key", ColumnDescription = "索引键", Length = 200, IsNullable = true)]
    public virtual string? Key { get; set; }

    /// <summary>
    /// 业务相关性标识
    /// </summary>
    [SugarColumn(ColumnName = "Correlation_Id", ColumnDescription = "业务相关性标识", Length = 100, IsNullable = true)]
    public virtual string? CorrelationId { get; set; }

    /// <summary>
    /// 到期时间（定时类书签）
    /// </summary>
    [SugarColumn(ColumnName = "Due_Time", ColumnDescription = "到期时间", IsNullable = true)]
    public virtual DateTime? DueTime { get; set; }

    /// <summary>
    /// 创建时间（引擎时钟）
    /// </summary>
    [SugarColumn(ColumnName = "Creation_Time", ColumnDescription = "创建时间", IsNullable = false)]
    public virtual DateTime CreationTime { get; set; }

    /// <summary>
    /// 书签完整 JSON（真源，含附加数据）
    /// </summary>
    [SugarColumn(ColumnName = "Bookmark_Json", ColumnDescription = "书签完整JSON", ColumnDataType = StaticConfig.CodeFirst_BigString, IsNullable = false)]
    public virtual string BookmarkJson { get; set; } = string.Empty;
}
