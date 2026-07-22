// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Core.Entities;
using XiHan.Framework.Workflow.Abstractions.Definitions;

namespace XiHan.BasicApp.Workflow.Domain.Entities;

/// <summary>
/// 系统工作流定义表
/// </summary>
/// <remarks>
/// 真源为 <see cref="DefinitionJson"/>（框架定义模型的完整 JSON 快照，读取时反序列化还原）；
/// 其余列是分页/检索用投影，与 JSON 同写同变，不单独修改。
/// </remarks>
[SugarTable(TableName = "Sys_Workflow_Definition", TableDescription = "系统工作流定义表")]
[SugarIndex("IX_{table}_TeId_CrTi", nameof(TenantId), OrderByType.Asc, nameof(CreatedTime), OrderByType.Desc)]
[SugarIndex("IX_{table}_CrId", nameof(CreatedId), OrderByType.Asc)]
[SugarIndex("IX_{table}_TeId_IsDe", nameof(TenantId), OrderByType.Asc, nameof(IsDeleted), OrderByType.Asc)]
[SugarIndex("UX_{table}_Co_Ve", nameof(Code), OrderByType.Asc, nameof(Version), OrderByType.Asc, nameof(IsDeleted), OrderByType.Asc, true)]
public partial class SysWorkflowDefinition : BasicAppFullAuditedEntity
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public SysWorkflowDefinition()
    {
    }

    /// <summary>
    /// 构造函数（主键 = 引擎定义标识）
    /// </summary>
    /// <param name="basicId"></param>
    public SysWorkflowDefinition(long basicId)
        : base(basicId)
    {
    }

    /// <summary>
    /// 流程编码（同编码多版本）
    /// </summary>
    [SugarColumn(ColumnName = "Code", ColumnDescription = "流程编码", Length = 100, IsNullable = false)]
    public virtual string Code { get; set; } = string.Empty;

    /// <summary>
    /// 流程名称
    /// </summary>
    [SugarColumn(ColumnName = "Name", ColumnDescription = "流程名称", Length = 200, IsNullable = false)]
    public virtual string Name { get; set; } = string.Empty;

    /// <summary>
    /// 版本号
    /// </summary>
    [SugarColumn(ColumnName = "Version", ColumnDescription = "版本号", IsNullable = false)]
    public virtual int Version { get; set; } = 1;

    /// <summary>
    /// 描述
    /// </summary>
    [SugarColumn(ColumnName = "Description", ColumnDescription = "描述", Length = 500, IsNullable = true)]
    public virtual string? Description { get; set; }

    /// <summary>
    /// 分类
    /// </summary>
    [SugarColumn(ColumnName = "Category", ColumnDescription = "分类", Length = 100, IsNullable = true)]
    public virtual string? Category { get; set; }

    /// <summary>
    /// 定义状态（草稿/已发布/已停用/已归档）
    /// </summary>
    [SugarColumn(ColumnName = "Status", ColumnDescription = "定义状态", IsNullable = false)]
    public virtual WorkflowDefinitionStatus Status { get; set; } = WorkflowDefinitionStatus.Draft;

    /// <summary>
    /// 是否启用补偿
    /// </summary>
    [SugarColumn(ColumnName = "Enable_Compensation", ColumnDescription = "是否启用补偿", IsNullable = false)]
    public virtual bool EnableCompensation { get; set; }

    /// <summary>
    /// 发布时间
    /// </summary>
    [SugarColumn(ColumnName = "Publish_Time", ColumnDescription = "发布时间", IsNullable = true)]
    public virtual DateTime? PublishTime { get; set; }

    /// <summary>
    /// 定义完整 JSON（真源，含节点/连线/变量声明）
    /// </summary>
    [SugarColumn(ColumnName = "Definition_Json", ColumnDescription = "定义完整JSON", ColumnDataType = StaticConfig.CodeFirst_BigString, IsNullable = false)]
    public virtual string DefinitionJson { get; set; } = string.Empty;
}
