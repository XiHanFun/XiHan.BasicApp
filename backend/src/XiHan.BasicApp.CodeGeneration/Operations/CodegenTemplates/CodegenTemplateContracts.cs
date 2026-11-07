// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using XiHan.BasicApp.CodeGeneration.Domain.Entities;

namespace XiHan.BasicApp.CodeGeneration.Operations.CodegenTemplates;

/// <summary>
/// 模板的列表项
/// </summary>
/// <param name="Id">模板 ID</param>
/// <param name="TemplateCode">编码</param>
/// <param name="TemplateName">名称</param>
/// <param name="TemplateDescription">描述</param>
/// <param name="TemplateGroup">分组（含 backend / frontend 即属那一侧；<c>*-package</c> 只在新建包时产出）</param>
/// <param name="TemplateType">适用的生成模式（通用即全部）</param>
/// <param name="TemplateEngine">模板引擎</param>
/// <param name="WriteMode">写入方式</param>
/// <param name="FileExtension">扩展名</param>
/// <param name="IsBuiltIn">是否内置</param>
/// <param name="Status">状态</param>
/// <param name="Order">排序号</param>
/// <param name="ConcurrencyStamp">并发戳：修改与启停时原样交回</param>
/// <param name="CreatedAt">创建时间</param>
/// <param name="ModifiedAt">修改时间</param>
public sealed record CodegenTemplateResponse(
    Guid Id,
    string TemplateCode,
    string TemplateName,
    string? TemplateDescription,
    string? TemplateGroup,
    CodeGenTemplateType TemplateType,
    CodeGenTemplateEngine TemplateEngine,
    CodeGenWriteMode WriteMode,
    string? FileExtension,
    bool IsBuiltIn,
    CodeGenStatus Status,
    int Order,
    string ConcurrencyStamp,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ModifiedAt)
{
    /// <summary>
    /// 从实体投影
    /// </summary>
    /// <param name="template">模板</param>
    public static CodegenTemplateResponse From(SysCodeGenTemplate template)
    {
        ArgumentNullException.ThrowIfNull(template);
        return new CodegenTemplateResponse(
            template.Id,
            template.TemplateCode,
            template.TemplateName,
            template.TemplateDescription,
            template.TemplateGroup,
            template.TemplateType,
            template.TemplateEngine,
            template.WriteMode,
            template.FileExtension,
            template.IsBuiltIn,
            template.Status,
            template.Order,
            template.ConcurrencyStamp,
            template.CreatedAt,
            template.ModifiedAt);
    }
}

/// <summary>
/// 模板详情：列表项加正文、路径表达式与备注
/// </summary>
/// <param name="Template">列表项的内容</param>
/// <param name="TemplateContent">正文（Scriban）</param>
/// <param name="FileNameExpression">文件名表达式</param>
/// <param name="FilePathExpression">目录表达式（相对那一侧的根目录）</param>
/// <param name="Remark">备注</param>
public sealed record CodegenTemplateDetailResponse(CodegenTemplateResponse Template, string? TemplateContent, string? FileNameExpression, string? FilePathExpression, string? Remark)
{
    /// <summary>
    /// 从实体投影
    /// </summary>
    /// <param name="template">模板</param>
    public static CodegenTemplateDetailResponse From(SysCodeGenTemplate template)
    {
        ArgumentNullException.ThrowIfNull(template);
        return new CodegenTemplateDetailResponse(CodegenTemplateResponse.From(template), template.TemplateContent, template.FileNameExpression, template.FilePathExpression, template.Remark);
    }
}

/// <summary>
/// <c>codegen-templates.get</c> 的请求
/// </summary>
/// <param name="Id">模板 ID</param>
public sealed record CodegenTemplateIdRequest(Guid Id);

/// <summary>
/// <c>codegen-templates.create</c> 的请求
/// </summary>
/// <param name="TemplateCode">编码：唯一、不含空白，建好后不能改（照 4.x）</param>
/// <param name="TemplateName">名称</param>
/// <param name="TemplateType">适用的生成模式</param>
/// <param name="TemplateEngine">模板引擎（只有 Scriban）</param>
/// <param name="WriteMode">写入方式</param>
/// <param name="TemplateContent">正文</param>
/// <param name="TemplateDescription">描述</param>
/// <param name="TemplateGroup">分组</param>
/// <param name="FileNameExpression">文件名表达式</param>
/// <param name="FilePathExpression">目录表达式</param>
/// <param name="FileExtension">扩展名</param>
/// <param name="Status">状态</param>
/// <param name="Order">排序号</param>
/// <param name="Remark">备注</param>
public sealed record CreateCodegenTemplateRequest(
    [Required, StringLength(100, MinimumLength = 1), RegularExpression(@"^\S+$")] string TemplateCode,
    [Required, StringLength(200, MinimumLength = 1)] string TemplateName,
    CodeGenTemplateType TemplateType,
    CodeGenTemplateEngine TemplateEngine,
    CodeGenWriteMode WriteMode,
    string? TemplateContent,
    [StringLength(500)] string? TemplateDescription = null,
    [StringLength(100)] string? TemplateGroup = null,
    [StringLength(500)] string? FileNameExpression = null,
    [StringLength(500)] string? FilePathExpression = null,
    [StringLength(20)] string? FileExtension = null,
    CodeGenStatus Status = CodeGenStatus.Active,
    [Range(0, int.MaxValue)] int Order = 0,
    [StringLength(500)] string? Remark = null);

/// <summary>
/// <c>codegen-templates.update</c> 的请求：编码不在这里改
/// </summary>
/// <param name="Id">模板 ID</param>
/// <param name="ConcurrencyStamp">读到的并发戳</param>
/// <param name="TemplateName">名称</param>
/// <param name="TemplateType">适用的生成模式</param>
/// <param name="TemplateEngine">模板引擎（只有 Scriban）</param>
/// <param name="WriteMode">写入方式</param>
/// <param name="TemplateContent">正文</param>
/// <param name="TemplateDescription">描述</param>
/// <param name="TemplateGroup">分组</param>
/// <param name="FileNameExpression">文件名表达式</param>
/// <param name="FilePathExpression">目录表达式</param>
/// <param name="FileExtension">扩展名</param>
/// <param name="Status">状态</param>
/// <param name="Order">排序号</param>
/// <param name="Remark">备注</param>
public sealed record UpdateCodegenTemplateRequest(
    Guid Id,
    [Required, StringLength(64, MinimumLength = 1)] string ConcurrencyStamp,
    [Required, StringLength(200, MinimumLength = 1)] string TemplateName,
    CodeGenTemplateType TemplateType,
    CodeGenTemplateEngine TemplateEngine,
    CodeGenWriteMode WriteMode,
    string? TemplateContent,
    [StringLength(500)] string? TemplateDescription = null,
    [StringLength(100)] string? TemplateGroup = null,
    [StringLength(500)] string? FileNameExpression = null,
    [StringLength(500)] string? FilePathExpression = null,
    [StringLength(20)] string? FileExtension = null,
    CodeGenStatus Status = CodeGenStatus.Active,
    [Range(0, int.MaxValue)] int Order = 0,
    [StringLength(500)] string? Remark = null);

/// <summary>
/// <c>codegen-templates.set-status</c> 的请求
/// </summary>
/// <param name="Id">模板 ID</param>
/// <param name="ConcurrencyStamp">读到的并发戳</param>
/// <param name="Status">目标状态</param>
/// <param name="Remark">操作备注：给了即替换备注，不给保留原备注</param>
public sealed record SetCodegenTemplateStatusRequest(
    Guid Id,
    [Required, StringLength(64, MinimumLength = 1)] string ConcurrencyStamp,
    CodeGenStatus Status,
    [StringLength(500)] string? Remark = null);

/// <summary>
/// <c>codegen-templates.delete</c> 的请求：集合入参
/// </summary>
/// <param name="Ids">模板 ID，1 到 100 个</param>
public sealed record CodegenTemplateIdsRequest([Required, MinLength(1), MaxLength(100)] IReadOnlyList<Guid> Ids);

/// <summary>
/// <c>codegen-templates.validate</c> 的请求
/// </summary>
/// <param name="TemplateContent">要检查的正文</param>
public sealed record ValidateCodegenTemplateRequest([Required, MinLength(1)] string TemplateContent);

/// <summary>
/// 模板语法检查的结果
/// </summary>
/// <param name="Valid">是否没有语法错误</param>
/// <param name="Errors">错误（Scriban 给的原文，带行列）</param>
public sealed record CodegenTemplateValidationResponse(bool Valid, IReadOnlyList<string> Errors);
