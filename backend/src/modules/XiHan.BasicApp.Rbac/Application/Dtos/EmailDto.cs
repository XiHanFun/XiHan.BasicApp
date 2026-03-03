#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:EmailDto
// Guid:2f5021c7-3af5-4d4a-88ec-bf8adacbf0f4
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/03/04 12:28:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using System.ComponentModel.DataAnnotations;
using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Rbac.Domain.Enums;

namespace XiHan.BasicApp.Rbac.Application.Dtos;

/// <summary>
/// 邮件 DTO
/// </summary>
public class EmailDto : BasicAppDto
{
    public long? SendUserId { get; set; }

    public long? ReceiveUserId { get; set; }

    public EmailType EmailType { get; set; } = EmailType.System;

    public string FromEmail { get; set; } = string.Empty;

    public string ToEmail { get; set; } = string.Empty;

    public string Subject { get; set; } = string.Empty;

    public bool IsHtml { get; set; } = true;

    public EmailStatus EmailStatus { get; set; } = EmailStatus.Pending;

    public DateTimeOffset? ScheduledTime { get; set; }

    public DateTimeOffset? SendTime { get; set; }
}

/// <summary>
/// 创建邮件 DTO
/// </summary>
public class EmailCreateDto : BasicAppCDto
{
    public long? SendUserId { get; set; }

    public long? ReceiveUserId { get; set; }

    public EmailType EmailType { get; set; } = EmailType.System;

    [Required(ErrorMessage = "发件邮箱不能为空")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "发件邮箱长度必须在 1～100 之间")]
    public string FromEmail { get; set; } = string.Empty;

    [StringLength(100, ErrorMessage = "发件人名称长度不能超过 100")]
    public string? FromName { get; set; }

    [Required(ErrorMessage = "收件邮箱不能为空")]
    [StringLength(1000, MinimumLength = 1, ErrorMessage = "收件邮箱长度必须在 1～1000 之间")]
    public string ToEmail { get; set; } = string.Empty;

    [StringLength(1000, ErrorMessage = "抄送邮箱长度不能超过 1000")]
    public string? CcEmail { get; set; }

    [StringLength(1000, ErrorMessage = "密送邮箱长度不能超过 1000")]
    public string? BccEmail { get; set; }

    [Required(ErrorMessage = "邮件主题不能为空")]
    [StringLength(200, MinimumLength = 1, ErrorMessage = "邮件主题长度必须在 1～200 之间")]
    public string Subject { get; set; } = string.Empty;

    public string? Content { get; set; }

    public bool IsHtml { get; set; } = true;

    public DateTimeOffset? ScheduledTime { get; set; }

    public long? TenantId { get; set; }

    [StringLength(500, ErrorMessage = "备注长度不能超过 500")]
    public string? Remark { get; set; }
}

/// <summary>
/// 更新邮件 DTO
/// </summary>
public class EmailUpdateDto : BasicAppUDto
{
    public long? SendUserId { get; set; }

    public long? ReceiveUserId { get; set; }

    public EmailType EmailType { get; set; } = EmailType.System;

    [Required(ErrorMessage = "发件邮箱不能为空")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "发件邮箱长度必须在 1～100 之间")]
    public string FromEmail { get; set; } = string.Empty;

    [StringLength(100, ErrorMessage = "发件人名称长度不能超过 100")]
    public string? FromName { get; set; }

    [Required(ErrorMessage = "收件邮箱不能为空")]
    [StringLength(1000, MinimumLength = 1, ErrorMessage = "收件邮箱长度必须在 1～1000 之间")]
    public string ToEmail { get; set; } = string.Empty;

    [StringLength(1000, ErrorMessage = "抄送邮箱长度不能超过 1000")]
    public string? CcEmail { get; set; }

    [StringLength(1000, ErrorMessage = "密送邮箱长度不能超过 1000")]
    public string? BccEmail { get; set; }

    [Required(ErrorMessage = "邮件主题不能为空")]
    [StringLength(200, MinimumLength = 1, ErrorMessage = "邮件主题长度必须在 1～200 之间")]
    public string Subject { get; set; } = string.Empty;

    public string? Content { get; set; }

    public bool IsHtml { get; set; } = true;

    public EmailStatus EmailStatus { get; set; } = EmailStatus.Pending;

    public DateTimeOffset? ScheduledTime { get; set; }

    public DateTimeOffset? SendTime { get; set; }

    [StringLength(500, ErrorMessage = "备注长度不能超过 500")]
    public string? Remark { get; set; }
}
