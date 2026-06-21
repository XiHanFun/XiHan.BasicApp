#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:CodeGenTemplateCommandModels
// Guid:c0de9e00-0a04-4a00-9000-000000000a04
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/06/20 10:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

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
    TemplateType TemplateType,
    TemplateEngine TemplateEngine,
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
    TemplateType TemplateType,
    TemplateEngine TemplateEngine,
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
