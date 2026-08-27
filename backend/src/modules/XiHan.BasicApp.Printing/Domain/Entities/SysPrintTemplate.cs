// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Core.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.Printing.Domain.Entities;

/// <summary>
/// hiprint 打印模板实体，保存租户或平台作用域内的设计 JSON 与运行元数据。
/// </summary>
/// <remarks>
/// <para><c>TenantId == 0</c> 表示平台全局模板；业务租户模板保存在当前租户范围内。</para>
/// <para>模板编码创建后不可修改；数据源编码可为空或调整，仅用于提供字段契约、设计素材与默认样例。</para>
/// <para>管理端更新通过继承的 <c>RowVersion</c> 参与乐观并发控制；模板 JSON 不写入操作日志。</para>
/// </remarks>
[SugarTable(TableName = "Sys_Print_Template", TableDescription = "打印模板表")]
[SugarIndex("IX_{table}_TeId_CrTi", nameof(TenantId), OrderByType.Asc, nameof(CreatedTime), OrderByType.Desc)]
[SugarIndex("IX_{table}_CrId", nameof(CreatedId), OrderByType.Asc)]
[SugarIndex("IX_{table}_TeId_IsDe", nameof(TenantId), OrderByType.Asc, nameof(IsDeleted), OrderByType.Asc)]
[SugarIndex("UX_{table}_TeId_TeCo", nameof(TenantId), OrderByType.Asc, nameof(TemplateCode), OrderByType.Asc, nameof(IsDeleted), OrderByType.Asc, true)]
[SugarIndex("IX_{table}_TeId_St_So", nameof(TenantId), OrderByType.Asc, nameof(Status), OrderByType.Asc, nameof(Sort), OrderByType.Asc)]
public partial class SysPrintTemplate : BasicAppFullAuditedEntity
{
    /// <summary>
    /// 模板编码，在所属租户范围内唯一，创建后不可修改。
    /// </summary>
    [SugarColumn(ColumnName = "Template_Code", ColumnDescription = "模板编码", Length = 100, IsNullable = false)]
    public virtual string TemplateCode { get; set; } = string.Empty;

    /// <summary>
    /// 可选的代码注册数据源编码；为空时模板使用自由字段模式。
    /// </summary>
    [SugarColumn(ColumnName = "Data_Source_Code", ColumnDescription = "可选数据源编码", Length = 100, IsNullable = true)]
    public virtual string? DataSourceCode { get; set; }

    /// <summary>
    /// 模板显示名称。
    /// </summary>
    [SugarColumn(ColumnName = "Template_Name", ColumnDescription = "模板名称", Length = 100, IsNullable = false)]
    public virtual string TemplateName { get; set; } = string.Empty;

    /// <summary>
    /// hiprint 模板 JSON；使用数据库大文本类型且不设置业务专属大小上限。
    /// </summary>
    [SugarColumn(ColumnName = "Template_Json", ColumnDescription = "hiprint模板JSON", ColumnDataType = StaticConfig.CodeFirst_BigString, IsNullable = false)]
    public virtual string TemplateJson { get; set; } = string.Empty;

    /// <summary>
    /// 保存模板时使用的 hiprint 引擎版本。
    /// </summary>
    [SugarColumn(ColumnName = "Engine_Version", ColumnDescription = "hiprint引擎版本", Length = 32, IsNullable = false)]
    public virtual string EngineVersion { get; set; } = "0.0.60";

    /// <summary>
    /// 全局模板是否允许业务租户解析使用；租户私有模板固定为 false。
    /// </summary>
    [SugarColumn(ColumnName = "Allow_Tenant_Use", ColumnDescription = "允许租户使用")]
    public virtual bool AllowTenantUse { get; set; }

    /// <summary>
    /// 启停状态。
    /// </summary>
    [SugarColumn(ColumnName = "Status", ColumnDescription = "状态")]
    public virtual EnableStatus Status { get; set; } = EnableStatus.Enabled;

    /// <summary>
    /// 显示排序。
    /// </summary>
    [SugarColumn(ColumnName = "Sort", ColumnDescription = "排序")]
    public virtual int Sort { get; set; }

    /// <summary>
    /// 模板备注。
    /// </summary>
    [SugarColumn(ColumnName = "Remark", ColumnDescription = "备注", Length = 500, IsNullable = true)]
    public virtual string? Remark { get; set; }
}
