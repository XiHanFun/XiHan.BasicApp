// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.CodeGeneration.Domain.Entities;
using XiHan.BasicApp.CodeGeneration.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.CodeGeneration.Domain.DomainServices;

/// <summary>
/// 代码生成模板创建命令
/// </summary>
public sealed record CodeGenTemplateCreateCommand(
    string TemplateCode,
    string TemplateName,
    string? TemplateDescription,
    string? TemplateGroup,
    TemplateType? TemplateType,
    TemplateEngine TemplateEngine,
    ArtifactWriteMode WriteMode,
    string? TemplateContent,
    string? FileNameExpression,
    string? FilePathExpression,
    string? FileExtension,
    EnableStatus Status,
    int Sort,
    string? Remark);

/// <summary>
/// 代码生成模板更新命令
/// </summary>
public sealed record CodeGenTemplateUpdateCommand(
    long BasicId,
    string TemplateName,
    string? TemplateDescription,
    string? TemplateGroup,
    TemplateType? TemplateType,
    TemplateEngine TemplateEngine,
    ArtifactWriteMode WriteMode,
    string? TemplateContent,
    string? FileNameExpression,
    string? FilePathExpression,
    string? FileExtension,
    bool IsEnabled,
    int Sort,
    string? Remark);

/// <summary>
/// 代码生成模板状态变更命令
/// </summary>
public sealed record CodeGenTemplateStatusChangeCommand(long BasicId, EnableStatus Status, string? Remark);

/// <summary>
/// 代码生成模板命令结果
/// </summary>
public sealed record CodeGenTemplateCommandResult(SysCodeGenTemplate Template);
