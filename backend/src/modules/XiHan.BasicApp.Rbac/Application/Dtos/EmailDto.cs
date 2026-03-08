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
    /// <summary>
    /// 发送用户标识
    /// </summary>
    public long? SendUserId { get; set; }

    /// <summary>
    /// 接收用户标识
    /// </summary>
    public long? ReceiveUserId { get; set; }

    /// <summary>
    /// 邮件类型
    /// </summary>
    public EmailType EmailType { get; set; } = EmailType.System;

    /// <summary>
    /// 发件邮箱
    /// </summary>
    public string FromEmail { get; set; } = string.Empty;

    /// <summary>
    /// 收件邮箱
    /// </summary>
    public string ToEmail { get; set; } = string.Empty;

    /// <summary>
    /// 邮件主题
    /// </summary>
    public string Subject { get; set; } = string.Empty;

    /// <summary>
    /// 是否 HTML 正文
    /// </summary>
    public bool IsHtml { get; set; } = true;

    /// <summary>
    /// 邮件状态
    /// </summary>
    public EmailStatus EmailStatus { get; set; } = EmailStatus.Pending;

    /// <summary>
    /// 计划发送时间
    /// </summary>
    public DateTimeOffset? ScheduledTime { get; set; }

    /// <summary>
    /// 实际发送时间
    /// </summary>
    public DateTimeOffset? SendTime { get; set; }
}

/// <summary>
/// 创建邮件 DTO
/// </summary>
public class EmailCreateDto : BasicAppCDto
{
    /// <summary>
    /// 发送用户标识
    /// </summary>
    public long? SendUserId { get; set; }

    /// <summary>
    /// 接收用户标识
    /// </summary>
    public long? ReceiveUserId { get; set; }

    /// <summary>
    /// 邮件类型
    /// </summary>
    public EmailType EmailType { get; set; } = EmailType.System;

    /// <summary>
    /// 发件邮箱
    /// </summary>
    [Required(ErrorMessage = "发件邮箱不能为空")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "发件邮箱长度必须在 1～100 之间")]
    public string FromEmail { get; set; } = string.Empty;

    /// <summary>
    /// 发件人名称
    /// </summary>
    [StringLength(100, ErrorMessage = "发件人名称长度不能超过 100")]
    public string? FromName { get; set; }

    /// <summary>
    /// 收件邮箱
    /// </summary>
    [Required(ErrorMessage = "收件邮箱不能为空")]
    [StringLength(1000, MinimumLength = 1, ErrorMessage = "收件邮箱长度必须在 1～1000 之间")]
    public string ToEmail { get; set; } = string.Empty;

    /// <summary>
    /// 抄送邮箱
    /// </summary>
    [StringLength(1000, ErrorMessage = "抄送邮箱长度不能超过 1000")]
    public string? CcEmail { get; set; }

    /// <summary>
    /// 密送邮箱
    /// </summary>
    [StringLength(1000, ErrorMessage = "密送邮箱长度不能超过 1000")]
    public string? BccEmail { get; set; }

    /// <summary>
    /// 邮件主题
    /// </summary>
    [Required(ErrorMessage = "邮件主题不能为空")]
    [StringLength(200, MinimumLength = 1, ErrorMessage = "邮件主题长度必须在 1～200 之间")]
    public string Subject { get; set; } = string.Empty;

    /// <summary>
    /// 邮件正文
    /// </summary>
    public string? Content { get; set; }

    /// <summary>
    /// 是否 HTML 正文
    /// </summary>
    public bool IsHtml { get; set; } = true;

    /// <summary>
    /// 计划发送时间
    /// </summary>
    public DateTimeOffset? ScheduledTime { get; set; }

    /// <summary>
    /// 租户标识
    /// </summary>
    public long? TenantId { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [StringLength(500, ErrorMessage = "备注长度不能超过 500")]
    public string? Remark { get; set; }
}

/// <summary>
/// 更新邮件 DTO
/// </summary>
public class EmailUpdateDto : BasicAppUDto
{
    /// <summary>
    /// 发送用户标识
    /// </summary>
    public long? SendUserId { get; set; }

    /// <summary>
    /// 接收用户标识
    /// </summary>
    public long? ReceiveUserId { get; set; }

    /// <summary>
    /// 邮件类型
    /// </summary>
    public EmailType EmailType { get; set; } = EmailType.System;

    /// <summary>
    /// 发件邮箱
    /// </summary>
    [Required(ErrorMessage = "发件邮箱不能为空")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "发件邮箱长度必须在 1～100 之间")]
    public string FromEmail { get; set; } = string.Empty;

    /// <summary>
    /// 发件人名称
    /// </summary>
    [StringLength(100, ErrorMessage = "发件人名称长度不能超过 100")]
    public string? FromName { get; set; }

    /// <summary>
    /// 收件邮箱
    /// </summary>
    [Required(ErrorMessage = "收件邮箱不能为空")]
    [StringLength(1000, MinimumLength = 1, ErrorMessage = "收件邮箱长度必须在 1～1000 之间")]
    public string ToEmail { get; set; } = string.Empty;

    /// <summary>
    /// 抄送邮箱
    /// </summary>
    [StringLength(1000, ErrorMessage = "抄送邮箱长度不能超过 1000")]
    public string? CcEmail { get; set; }

    /// <summary>
    /// 密送邮箱
    /// </summary>
    [StringLength(1000, ErrorMessage = "密送邮箱长度不能超过 1000")]
    public string? BccEmail { get; set; }

    /// <summary>
    /// 邮件主题
    /// </summary>
    [Required(ErrorMessage = "邮件主题不能为空")]
    [StringLength(200, MinimumLength = 1, ErrorMessage = "邮件主题长度必须在 1～200 之间")]
    public string Subject { get; set; } = string.Empty;

    /// <summary>
    /// 邮件正文
    /// </summary>
    public string? Content { get; set; }

    /// <summary>
    /// 是否 HTML 正文
    /// </summary>
    public bool IsHtml { get; set; } = true;

    /// <summary>
    /// 邮件状态
    /// </summary>
    public EmailStatus EmailStatus { get; set; } = EmailStatus.Pending;

    /// <summary>
    /// 计划发送时间
    /// </summary>
    public DateTimeOffset? ScheduledTime { get; set; }

    /// <summary>
    /// 实际发送时间
    /// </summary>
    public DateTimeOffset? SendTime { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [StringLength(500, ErrorMessage = "备注长度不能超过 500")]
    public string? Remark { get; set; }
}
