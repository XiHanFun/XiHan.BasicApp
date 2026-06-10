#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:MessageTemplateCommandModels
// Guid:2e37afcc-035e-49ba-f71c-efad62d33fa7
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/06/11 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 消息模板创建命令
/// </summary>
public sealed record MessageTemplateCreateCommand(
    string TemplateCode,
    MessageChannel Channel,
    string TemplateName,
    string? Subject,
    string Content,
    bool IsHtml,
    string? Description,
    EnableStatus Status,
    int Sort,
    string? Remark);

/// <summary>
/// 消息模板更新命令
/// </summary>
public sealed record MessageTemplateUpdateCommand(
    long BasicId,
    string TemplateName,
    string? Subject,
    string Content,
    bool IsHtml,
    string? Description,
    int Sort,
    string? Remark);

/// <summary>
/// 消息模板状态变更命令
/// </summary>
public sealed record MessageTemplateStatusChangeCommand(long BasicId, EnableStatus Status, string? Remark);

/// <summary>
/// 消息模板命令结果
/// </summary>
public sealed record MessageTemplateCommandResult(SysMessageTemplate Template);
