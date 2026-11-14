// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Contracts.Entities;
using XiHan.Framework.Persistence.Abstractions;

namespace XiHan.BasicApp.CodeGeneration.Domain.Entities;

/// <summary>
/// 代码生成的模板
/// </summary>
/// <remarks>
/// 平台库实体、只在平台一侧。内置模板由种子每次启动对齐正文与路径、不能修改与删除，启停归运营；自定义模板随意维护
/// 编码唯一，软删除后可以重建
/// </remarks>
[SugarTable(TableName = "sys_code_gen_template", TableDescription = "代码生成的模板")]
[ActiveUniqueIndex(nameof(TemplateCode), Name = "ux_sys_code_gen_template_template_code")]
[SugarIndex("ix_sys_code_gen_template_template_group", nameof(TemplateGroup), OrderByType.Asc)]
public sealed class SysCodeGenTemplate : AppAuditedEntity
{
    /// <summary>
    /// 编码
    /// </summary>
    [SugarColumn(ColumnName = "template_code", Length = 100, IsNullable = false, ColumnDescription = "编码")]
    public string TemplateCode { get; set; } = string.Empty;

    /// <summary>
    /// 名称
    /// </summary>
    [SugarColumn(ColumnName = "template_name", Length = 200, IsNullable = false, ColumnDescription = "名称")]
    public string TemplateName { get; set; } = string.Empty;

    /// <summary>
    /// 描述
    /// </summary>
    [SugarColumn(ColumnName = "template_description", Length = 500, IsNullable = true, ColumnDescription = "描述")]
    public string? TemplateDescription { get; set; }

    /// <summary>
    /// 分组
    /// </summary>
    /// <remarks>
    /// 前缀决定前后端归属（<c>backend-*</c> / <c>frontend-*</c>）：<c>*-crud</c> 是资源文件，<c>*-package</c> 只在新建包时产出
    /// </remarks>
    [SugarColumn(ColumnName = "template_group", Length = 100, IsNullable = true, ColumnDescription = "分组")]
    public string? TemplateGroup { get; set; }

    /// <summary>
    /// 适用的生成模式
    /// </summary>
    [SugarColumn(ColumnName = "template_type", IsNullable = false, ColumnDescription = "适用的生成模式")]
    public CodeGenTemplateType TemplateType { get; set; } = CodeGenTemplateType.Universal;

    /// <summary>
    /// 模板引擎
    /// </summary>
    [SugarColumn(ColumnName = "template_engine", IsNullable = false, ColumnDescription = "模板引擎")]
    public CodeGenTemplateEngine TemplateEngine { get; set; } = CodeGenTemplateEngine.Scriban;

    /// <summary>
    /// 产物的写入方式
    /// </summary>
    [SugarColumn(ColumnName = "write_mode", IsNullable = false, ColumnDescription = "产物的写入方式")]
    public CodeGenWriteMode WriteMode { get; set; } = CodeGenWriteMode.AlwaysOverwrite;

    /// <summary>
    /// 模板正文
    /// </summary>
    [SugarColumn(ColumnName = "template_content", IsNullable = true, ColumnDescription = "模板正文")]
    [LongText]
    public string? TemplateContent { get; set; }

    /// <summary>
    /// 内置模板的资源路径
    /// </summary>
    [SugarColumn(ColumnName = "template_path", Length = 500, IsNullable = true, ColumnDescription = "内置模板的资源路径")]
    public string? TemplatePath { get; set; }

    /// <summary>
    /// 产物文件名表达式
    /// </summary>
    [SugarColumn(ColumnName = "file_name_expression", Length = 500, IsNullable = true, ColumnDescription = "产物文件名表达式")]
    public string? FileNameExpression { get; set; }

    /// <summary>
    /// 产物目录表达式
    /// </summary>
    [SugarColumn(ColumnName = "file_path_expression", Length = 500, IsNullable = true, ColumnDescription = "产物目录表达式")]
    public string? FilePathExpression { get; set; }

    /// <summary>
    /// 产物扩展名
    /// </summary>
    [SugarColumn(ColumnName = "file_extension", Length = 20, IsNullable = true, ColumnDescription = "产物扩展名")]
    public string? FileExtension { get; set; }

    /// <summary>
    /// 是否内置
    /// </summary>
    [SugarColumn(ColumnName = "is_built_in", IsNullable = false, ColumnDescription = "是否内置")]
    public bool IsBuiltIn { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    [SugarColumn(ColumnName = "status", IsNullable = false, ColumnDescription = "状态")]
    public CodeGenStatus Status { get; set; } = CodeGenStatus.Active;

    /// <summary>
    /// 排序号
    /// </summary>
    [SugarColumn(ColumnName = "order", IsNullable = false, ColumnDescription = "排序号")]
    public int Order { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [SugarColumn(ColumnName = "remark", Length = 500, IsNullable = true, ColumnDescription = "备注")]
    public string? Remark { get; set; }
}

/// <summary>
/// 模板引擎
/// </summary>
/// <remarks>
/// 目前只有 Scriban，字段保留给以后扩展
/// </remarks>
public enum CodeGenTemplateEngine
{
    /// <summary>
    /// Scriban
    /// </summary>
    Scriban = 1,
}

/// <summary>
/// 产物的写入方式
/// </summary>
/// <remarks>
/// 自动文件由表结构推导、重新生成时覆盖；手写文件只在第一次生成时创建，此后不再碰
/// </remarks>
public enum CodeGenWriteMode
{
    /// <summary>
    /// 每次覆盖
    /// </summary>
    AlwaysOverwrite = 0,

    /// <summary>
    /// 只在第一次写
    /// </summary>
    WriteOnce = 1,
}
