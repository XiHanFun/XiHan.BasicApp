#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysGenTemplate
// Guid:a1b2c3d4-e5f6-7890-abcd-ef1234567012
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/12/23 10:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.CodeGeneration.Enums;
using XiHan.BasicApp.Rbac.Entities.Base;
using XiHan.BasicApp.Rbac.Enums;

namespace XiHan.BasicApp.CodeGeneration.Entities;

/// <summary>
/// 系统代码生成模板实体
/// </summary>
[SugarTable("Sys_Gen_Template", "系统代码生成模板表")]
[SugarIndex("IX_SysGenTemplate_TemplateCode", nameof(TemplateCode), OrderByType.Asc, true)]
[SugarIndex("IX_SysGenTemplate_TemplateGroup", nameof(TemplateGroup), OrderByType.Asc)]
public partial class SysGenTemplate : RbacFullAuditedEntity<long>
{
    /// <summary>
    /// 模板编码
    /// </summary>
    [SugarColumn(ColumnDescription = "模板编码", Length = 100, IsNullable = false)]
    public virtual string TemplateCode { get; set; } = string.Empty;

    /// <summary>
    /// 模板名称
    /// </summary>
    [SugarColumn(ColumnDescription = "模板名称", Length = 200, IsNullable = false)]
    public virtual string TemplateName { get; set; } = string.Empty;

    /// <summary>
    /// 模板描述
    /// </summary>
    [SugarColumn(ColumnDescription = "模板描述", Length = 500, IsNullable = true)]
    public virtual string? TemplateDescription { get; set; }

    /// <summary>
    /// 模板分组
    /// </summary>
    [SugarColumn(ColumnDescription = "模板分组", Length = 100, IsNullable = true)]
    public virtual string? TemplateGroup { get; set; }

    /// <summary>
    /// 模板类型
    /// </summary>
    [SugarColumn(ColumnDescription = "模板类型")]
    public virtual TemplateType TemplateType { get; set; } = TemplateType.Single;

    /// <summary>
    /// 模板引擎
    /// </summary>
    [SugarColumn(ColumnDescription = "模板引擎")]
    public virtual TemplateEngine TemplateEngine { get; set; } = TemplateEngine.Razor;

    /// <summary>
    /// 模板内容
    /// </summary>
    [SugarColumn(ColumnDescription = "模板内容", ColumnDataType = StaticConfig.CodeFirst_BigString, IsNullable = true)]
    public virtual string? TemplateContent { get; set; }

    /// <summary>
    /// 模板路径
    /// </summary>
    [SugarColumn(ColumnDescription = "模板路径", Length = 500, IsNullable = true)]
    public virtual string? TemplatePath { get; set; }

    /// <summary>
    /// 生成文件名表达式
    /// </summary>
    [SugarColumn(ColumnDescription = "生成文件名表达式", Length = 500, IsNullable = true)]
    public virtual string? FileNameExpression { get; set; }

    /// <summary>
    /// 生成文件路径表达式
    /// </summary>
    [SugarColumn(ColumnDescription = "生成文件路径表达式", Length = 500, IsNullable = true)]
    public virtual string? FilePathExpression { get; set; }

    /// <summary>
    /// 文件扩展名
    /// </summary>
    [SugarColumn(ColumnDescription = "文件扩展名", Length = 20, IsNullable = true)]
    public virtual string? FileExtension { get; set; }

    /// <summary>
    /// 是否内置
    /// </summary>
    [SugarColumn(ColumnDescription = "是否内置")]
    public virtual bool IsBuiltIn { get; set; } = false;

    /// <summary>
    /// 是否启用
    /// </summary>
    [SugarColumn(ColumnDescription = "是否启用")]
    public virtual bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 排序
    /// </summary>
    [SugarColumn(ColumnDescription = "排序")]
    public virtual int Sort { get; set; } = 0;

    /// <summary>
    /// 状态
    /// </summary>
    [SugarColumn(ColumnDescription = "状态")]
    public virtual YesOrNo Status { get; set; } = YesOrNo.Yes;

    /// <summary>
    /// 备注
    /// </summary>
    [SugarColumn(ColumnDescription = "备注", Length = 500, IsNullable = true)]
    public virtual string? Remark { get; set; }
}
