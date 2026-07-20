#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:CodeGenTemplateDtos
// Guid:c0de9e00-0404-4a00-9000-000000000404
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/06/20 10:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

#pragma warning disable CS1591

using XiHan.BasicApp.CodeGeneration.Domain.Enums;
using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.CodeGeneration.Application.Dtos;

/// <summary>
/// 代码生成模板创建 DTO
/// </summary>
public sealed class CodeGenTemplateCreateDto
{
    public string TemplateCode { get; set; } = string.Empty;
    public string TemplateName { get; set; } = string.Empty;
    public string? TemplateDescription { get; set; }
    public string? TemplateGroup { get; set; }
    /// <summary>模板类型；为空表示通用模板，适用于全部类型（单表/树表/主子表）</summary>
    public TemplateType? TemplateType { get; set; }

    /// <summary>模板引擎（决策 D1：当前仅 Scriban 可渲染）</summary>
    public TemplateEngine TemplateEngine { get; set; } = TemplateEngine.Scriban;

    /// <summary>写入策略：机器文件总是覆盖；人类文件仅在目标不存在时创建，用于承载手写代码</summary>
    public ArtifactWriteMode WriteMode { get; set; } = ArtifactWriteMode.AlwaysOverwrite;

    public string? TemplateContent { get; set; }
    public string? FileNameExpression { get; set; }
    public string? FilePathExpression { get; set; }
    public string? FileExtension { get; set; }
    public int Sort { get; set; }
    public EnableStatus Status { get; set; } = EnableStatus.Enabled;
    public string? Remark { get; set; }
}

/// <summary>
/// 代码生成模板更新 DTO
/// </summary>
public sealed class CodeGenTemplateUpdateDto : BasicAppUDto
{
    public string TemplateName { get; set; } = string.Empty;
    public string? TemplateDescription { get; set; }
    public string? TemplateGroup { get; set; }
    /// <summary>模板类型；为空表示通用模板，适用于全部类型（单表/树表/主子表）</summary>
    public TemplateType? TemplateType { get; set; }
    public TemplateEngine TemplateEngine { get; set; } = TemplateEngine.Scriban;

    /// <summary>写入策略：机器文件总是覆盖；人类文件仅在目标不存在时创建，用于承载手写代码</summary>
    public ArtifactWriteMode WriteMode { get; set; } = ArtifactWriteMode.AlwaysOverwrite;

    public string? TemplateContent { get; set; }
    public string? FileNameExpression { get; set; }
    public string? FilePathExpression { get; set; }
    public string? FileExtension { get; set; }
    public bool IsEnabled { get; set; } = true;
    public int Sort { get; set; }
    public string? Remark { get; set; }
}

/// <summary>
/// 代码生成模板状态更新 DTO
/// </summary>
public sealed class CodeGenTemplateStatusUpdateDto : BasicAppDto
{
    public EnableStatus Status { get; set; } = EnableStatus.Enabled;
    public string? Remark { get; set; }
}

/// <summary>
/// 代码生成模板分页查询 DTO
/// </summary>
public sealed class CodeGenTemplatePageQueryDto : BasicAppPRDto
{
    public string? Keyword { get; set; }
    public string? TemplateGroup { get; set; }
    public TemplateType? TemplateType { get; set; }
    public TemplateEngine? TemplateEngine { get; set; }
    public ArtifactWriteMode? WriteMode { get; set; }
    public bool? IsBuiltIn { get; set; }
    public bool? IsEnabled { get; set; }
    public EnableStatus? Status { get; set; }
}

/// <summary>
/// 代码生成模板列表项 DTO
/// </summary>
public class CodeGenTemplateListItemDto : BasicAppDto
{
    public string TemplateCode { get; set; } = string.Empty;
    public string TemplateName { get; set; } = string.Empty;
    public string? TemplateDescription { get; set; }
    public string? TemplateGroup { get; set; }
    /// <summary>模板类型；为空表示通用模板</summary>
    public TemplateType? TemplateType { get; set; }

    public TemplateEngine TemplateEngine { get; set; }

    /// <summary>写入策略：机器文件总是覆盖；人类文件仅在目标不存在时创建</summary>
    public ArtifactWriteMode WriteMode { get; set; }

    public string? FileExtension { get; set; }
    public bool IsBuiltIn { get; set; }
    public bool IsEnabled { get; set; }
    public int Sort { get; set; }
    public EnableStatus Status { get; set; }
    public DateTimeOffset CreatedTime { get; set; }
    public DateTimeOffset? ModifiedTime { get; set; }
}

/// <summary>
/// 代码生成模板详情 DTO（含模板正文）
/// </summary>
public sealed class CodeGenTemplateDetailDto : CodeGenTemplateListItemDto
{
    public string? TemplateContent { get; set; }
    public string? TemplatePath { get; set; }
    public string? FileNameExpression { get; set; }
    public string? FilePathExpression { get; set; }
    public string? Remark { get; set; }
    public long? CreatedId { get; set; }
    public string? CreatedBy { get; set; }
    public long? ModifiedId { get; set; }
    public string? ModifiedBy { get; set; }
}

/// <summary>
/// 模板语法校验请求 DTO
/// </summary>
public sealed class CodeGenTemplateValidateDto
{
    public TemplateEngine TemplateEngine { get; set; } = TemplateEngine.Scriban;
    public string TemplateContent { get; set; } = string.Empty;
}

/// <summary>
/// 模板语法校验结果 DTO
/// </summary>
public sealed class CodeGenTemplateValidateResultDto
{
    public bool IsValid { get; set; }
    public IReadOnlyList<string> Errors { get; set; } = [];
}
