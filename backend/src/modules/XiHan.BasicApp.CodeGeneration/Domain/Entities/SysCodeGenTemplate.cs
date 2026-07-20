#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysCodeGenTemplate
// Guid:a1b2c3d4-e5f6-7890-abcd-ef1234567012
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/12/23 10:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.CodeGeneration.Domain.Enums;
using XiHan.BasicApp.Core.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.CodeGeneration.Domain.Entities;

/// <summary>
/// 系统代码生成模板实体
/// 代码生成引擎使用的模板元信息（Scriban），承载模板分组、写入策略、目标文件名规则、输出路径
/// </summary>
/// <remarks>
/// 关联：
/// - 无 FK；模板分组（TemplateGroup）用于批量应用
///
/// 写入：
/// - TemplateCode 全局唯一（UX_TeCo）
/// - 模板正文（Content）建议独立 BigString 字段存储
/// - 模板变更建议经审核流程（影响所有生成结果）
///
/// 查询：
/// - 生成时按 TemplateGroup 批量加载：IX_TeGr
/// - 模板管理后台列表：IX_TeId_St
///
/// 删除：
/// - 仅软删；删除前须确认无正在使用的生成任务
///
/// 状态：
/// - Status: Yes/No
///
/// 场景：
/// - 全栈代码生成（Entity/DTO/Service/Vue Page）
/// - 多风格模板（CRUD 简化版 / 完整版 / 只读展示版）
/// - 机器/人类产物分离（WriteMode：机器文件总是覆盖，人类文件仅首次创建）
/// </remarks>
[SugarTable(TableName = "Sys_CodeGen_Template", TableDescription = "系统代码生成模板表")]
[SugarIndex("IX_{table}_TeId_CrTi", nameof(TenantId), OrderByType.Asc, nameof(CreatedTime), OrderByType.Desc)]
[SugarIndex("IX_{table}_CrId", nameof(CreatedId), OrderByType.Asc)]
[SugarIndex("IX_{table}_TeId_IsDe", nameof(TenantId), OrderByType.Asc, nameof(IsDeleted), OrderByType.Asc)]
[SugarIndex("UX_{table}_TeCo", nameof(TemplateCode), OrderByType.Asc, true)]
[SugarIndex("IX_{table}_TeGr", nameof(TemplateGroup), OrderByType.Asc)]
[SugarIndex("IX_{table}_TeId_St", nameof(TenantId), OrderByType.Asc, nameof(Status), OrderByType.Asc)]
public partial class SysCodeGenTemplate : BasicAppFullAuditedEntity
{
    /// <summary>
    /// 模板编码
    /// </summary>
    [SugarColumn(ColumnName = "Template_Code", ColumnDescription = "模板编码", Length = 100, IsNullable = false)]
    public virtual string TemplateCode { get; set; } = string.Empty;

    /// <summary>
    /// 模板名称
    /// </summary>
    [SugarColumn(ColumnName = "Template_Name", ColumnDescription = "模板名称", Length = 200, IsNullable = false)]
    public virtual string TemplateName { get; set; } = string.Empty;

    /// <summary>
    /// 模板描述
    /// </summary>
    [SugarColumn(ColumnName = "Template_Description", ColumnDescription = "模板描述", Length = 500, IsNullable = true)]
    public virtual string? TemplateDescription { get; set; }

    /// <summary>
    /// 模板分组
    /// </summary>
    [SugarColumn(ColumnName = "Template_Group", ColumnDescription = "模板分组", Length = 100, IsNullable = true)]
    public virtual string? TemplateGroup { get; set; }

    /// <summary>
    /// 模板类型
    /// 为空表示通用模板，适用于全部模板类型（单表/树表/主子表）；非空则仅在该类型下生效
    /// </summary>
    [SugarColumn(ColumnName = "Template_Type", ColumnDescription = "模板类型", IsNullable = true)]
    public virtual TemplateType? TemplateType { get; set; }

    /// <summary>
    /// 模板引擎
    /// </summary>
    [SugarColumn(ColumnName = "Template_Engine", ColumnDescription = "模板引擎")]
    public virtual TemplateEngine TemplateEngine { get; set; } = TemplateEngine.Scriban;

    /// <summary>
    /// 写入策略
    /// 机器文件（AlwaysOverwrite）重新生成时总是覆盖；人类文件（WriteOnce）仅在目标不存在时创建，此后永不触碰
    /// </summary>
    [SugarColumn(ColumnName = "Write_Mode", ColumnDescription = "写入策略")]
    public virtual ArtifactWriteMode WriteMode { get; set; } = ArtifactWriteMode.AlwaysOverwrite;

    /// <summary>
    /// 模板内容
    /// </summary>
    [SugarColumn(ColumnName = "Template_Content", ColumnDescription = "模板内容", ColumnDataType = StaticConfig.CodeFirst_BigString, IsNullable = true)]
    public virtual string? TemplateContent { get; set; }

    /// <summary>
    /// 模板路径
    /// </summary>
    [SugarColumn(ColumnName = "Template_Path", ColumnDescription = "模板路径", Length = 500, IsNullable = true)]
    public virtual string? TemplatePath { get; set; }

    /// <summary>
    /// 生成文件名表达式
    /// </summary>
    [SugarColumn(ColumnName = "File_Name_Expression", ColumnDescription = "生成文件名表达式", Length = 500, IsNullable = true)]
    public virtual string? FileNameExpression { get; set; }

    /// <summary>
    /// 生成文件路径表达式
    /// </summary>
    [SugarColumn(ColumnName = "File_Path_Expression", ColumnDescription = "生成文件路径表达式", Length = 500, IsNullable = true)]
    public virtual string? FilePathExpression { get; set; }

    /// <summary>
    /// 文件扩展名
    /// </summary>
    [SugarColumn(ColumnName = "File_Extension", ColumnDescription = "文件扩展名", Length = 20, IsNullable = true)]
    public virtual string? FileExtension { get; set; }

    /// <summary>
    /// 是否内置
    /// </summary>
    [SugarColumn(ColumnName = "Is_Built_In", ColumnDescription = "是否内置")]
    public virtual bool IsBuiltIn { get; set; } = false;

    /// <summary>
    /// 是否启用
    /// </summary>
    [SugarColumn(ColumnName = "Is_Enabled", ColumnDescription = "是否启用")]
    public virtual bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 排序
    /// </summary>
    [SugarColumn(ColumnName = "Sort", ColumnDescription = "排序")]
    public virtual int Sort { get; set; } = 0;

    /// <summary>
    /// 状态
    /// </summary>
    [SugarColumn(ColumnName = "Status", ColumnDescription = "状态")]
    public virtual EnableStatus Status { get; set; } = EnableStatus.Enabled;

    /// <summary>
    /// 备注
    /// </summary>
    [SugarColumn(ColumnName = "Remark", ColumnDescription = "备注", Length = 500, IsNullable = true)]
    public virtual string? Remark { get; set; }
}
