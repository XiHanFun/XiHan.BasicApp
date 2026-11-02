// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using SqlSugar;
using XiHan.BasicApp.Contracts.Entities;
using XiHan.Framework.Persistence.Abstractions;

namespace XiHan.BasicApp.CodeGeneration.Domain.Entities;

/// <summary>
/// 代码生成的模板。
/// </summary>
/// <remarks>
/// 平台库实体、只在平台一侧。内置模板由种子每次启动对齐正文与路径、不能修改与删除，启停归运营（照 4.x）；自定义模板随意维护。
/// 4.x 的 <c>IsEnabled</c> 与 <c>Status</c> 合成一个启停（方案 C2）。编码唯一，软删除后可以重建。
/// </remarks>
[ActiveUniqueIndex(nameof(TemplateCode))]
[SugarIndex("ix_{table}_template_group", nameof(TemplateGroup), OrderByType.Asc)]
public sealed class SysCodeGenTemplate : AppAuditedEntity
{
    /// <summary>
    /// 编码。
    /// </summary>
    [StringLength(100)]
    public string TemplateCode { get; set; } = string.Empty;

    /// <summary>
    /// 名称。
    /// </summary>
    [StringLength(200)]
    public string TemplateName { get; set; } = string.Empty;

    /// <summary>
    /// 描述。
    /// </summary>
    [StringLength(500)]
    public string? TemplateDescription { get; set; }

    /// <summary>
    /// 分组。
    /// </summary>
    /// <remarks>
    /// 前缀决定前后端归属（<c>backend-*</c> / <c>frontend-*</c>）：<c>*-crud</c> 是资源文件，<c>*-package</c> 只在新建包时产出。
    /// </remarks>
    [StringLength(100)]
    public string? TemplateGroup { get; set; }

    /// <summary>
    /// 适用的生成模式。
    /// </summary>
    public CodeGenTemplateType TemplateType { get; set; } = CodeGenTemplateType.Universal;

    /// <summary>
    /// 模板引擎。
    /// </summary>
    public CodeGenTemplateEngine TemplateEngine { get; set; } = CodeGenTemplateEngine.Scriban;

    /// <summary>
    /// 产物的写入方式。
    /// </summary>
    public CodeGenWriteMode WriteMode { get; set; } = CodeGenWriteMode.AlwaysOverwrite;

    /// <summary>
    /// 模板正文。
    /// </summary>
    [LongText]
    public string? TemplateContent { get; set; }

    /// <summary>
    /// 内置模板的资源路径。
    /// </summary>
    [StringLength(500)]
    public string? TemplatePath { get; set; }

    /// <summary>
    /// 产物文件名表达式。
    /// </summary>
    [StringLength(500)]
    public string? FileNameExpression { get; set; }

    /// <summary>
    /// 产物目录表达式。
    /// </summary>
    [StringLength(500)]
    public string? FilePathExpression { get; set; }

    /// <summary>
    /// 产物扩展名。
    /// </summary>
    [StringLength(20)]
    public string? FileExtension { get; set; }

    /// <summary>
    /// 是否内置。
    /// </summary>
    public bool IsBuiltIn { get; set; }

    /// <summary>
    /// 状态。
    /// </summary>
    public CodeGenStatus Status { get; set; } = CodeGenStatus.Active;

    /// <summary>
    /// 排序号。
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// 备注。
    /// </summary>
    [StringLength(500)]
    public string? Remark { get; set; }
}
