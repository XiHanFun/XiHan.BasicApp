#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:MessageTemplateDtos
// Guid:5b6ad2ff-368b-4ced-ca4f-b2da95a66cda
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/06/11 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 消息模板创建 DTO
/// </summary>
public sealed class MessageTemplateCreateDto : BasicAppCDto
{
    /// <summary>
    /// 模板编码（渠道内唯一）
    /// </summary>
    public string TemplateCode { get; set; } = string.Empty;

    /// <summary>
    /// 消息渠道
    /// </summary>
    public MessageChannel Channel { get; set; } = MessageChannel.Email;

    /// <summary>
    /// 模板名称
    /// </summary>
    public string TemplateName { get; set; } = string.Empty;

    /// <summary>
    /// 主题模板（邮件主题/通知标题）
    /// </summary>
    public string? Subject { get; set; }

    /// <summary>
    /// 内容模板（Scriban 语法）
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 内容是否 HTML
    /// </summary>
    public bool IsHtml { get; set; }

    /// <summary>
    /// 模板描述（可用变量说明）
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    public EnableStatus Status { get; set; } = EnableStatus.Enabled;

    /// <summary>
    /// 排序
    /// </summary>
    public int Sort { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}

/// <summary>
/// 消息模板更新 DTO（编码与渠道为标识，不可变更）
/// </summary>
public sealed class MessageTemplateUpdateDto : BasicAppUDto
{
    /// <summary>
    /// 模板名称
    /// </summary>
    public string TemplateName { get; set; } = string.Empty;

    /// <summary>
    /// 主题模板
    /// </summary>
    public string? Subject { get; set; }

    /// <summary>
    /// 内容模板（Scriban 语法）
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 内容是否 HTML
    /// </summary>
    public bool IsHtml { get; set; }

    /// <summary>
    /// 模板描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 排序
    /// </summary>
    public int Sort { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}

/// <summary>
/// 消息模板状态更新 DTO
/// </summary>
public sealed class MessageTemplateStatusUpdateDto : BasicAppUDto
{
    /// <summary>
    /// 状态
    /// </summary>
    public EnableStatus Status { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}

/// <summary>
/// 消息模板列表项 DTO
/// </summary>
public class MessageTemplateListItemDto : BasicAppDto
{
    /// <summary>
    /// 模板编码
    /// </summary>
    public string TemplateCode { get; set; } = string.Empty;

    /// <summary>
    /// 消息渠道
    /// </summary>
    public MessageChannel Channel { get; set; }

    /// <summary>
    /// 模板名称
    /// </summary>
    public string TemplateName { get; set; } = string.Empty;

    /// <summary>
    /// 主题模板
    /// </summary>
    public string? Subject { get; set; }

    /// <summary>
    /// 内容是否 HTML
    /// </summary>
    public bool IsHtml { get; set; }

    /// <summary>
    /// 是否平台级全局模板
    /// </summary>
    public bool IsGlobal { get; set; }

    /// <summary>
    /// 模板描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    public EnableStatus Status { get; set; }

    /// <summary>
    /// 排序
    /// </summary>
    public int Sort { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}

/// <summary>
/// 消息模板详情 DTO
/// </summary>
public sealed class MessageTemplateDetailDto : MessageTemplateListItemDto
{
    /// <summary>
    /// 内容模板（Scriban 语法）
    /// </summary>
    public string Content { get; set; } = string.Empty;
}

/// <summary>
/// 消息模板分页查询 DTO
/// </summary>
public sealed class MessageTemplatePageQueryDto : BasicAppPRDto
{
    /// <summary>
    /// 关键词（编码/名称/描述）
    /// </summary>
    public string? Keyword { get; set; }

    /// <summary>
    /// 消息渠道
    /// </summary>
    public MessageChannel? Channel { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    public EnableStatus? Status { get; set; }
}
