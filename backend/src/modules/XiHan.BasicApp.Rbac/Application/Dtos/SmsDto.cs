#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SmsDto
// Guid:995f61b0-84cf-4b4e-9770-98c5f0e9548e
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/03/04 12:29:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using System.ComponentModel.DataAnnotations;
using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Rbac.Domain.Enums;

namespace XiHan.BasicApp.Rbac.Application.Dtos;

/// <summary>
/// 短信 DTO
/// </summary>
public class SmsDto : BasicAppDto
{
    public long? SenderId { get; set; }

    public long? ReceiverId { get; set; }

    public SmsType SmsType { get; set; } = SmsType.Notification;

    public string ToPhone { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public SmsStatus SmsStatus { get; set; } = SmsStatus.Pending;

    public DateTimeOffset? ScheduledTime { get; set; }

    public DateTimeOffset? SendTime { get; set; }
}

/// <summary>
/// 创建短信 DTO
/// </summary>
public class SmsCreateDto : BasicAppCDto
{
    public long? SenderId { get; set; }

    public long? ReceiverId { get; set; }

    public SmsType SmsType { get; set; } = SmsType.Notification;

    [Required(ErrorMessage = "接收手机号不能为空")]
    [StringLength(500, MinimumLength = 1, ErrorMessage = "接收手机号长度必须在 1～500 之间")]
    public string ToPhone { get; set; } = string.Empty;

    [Required(ErrorMessage = "短信内容不能为空")]
    [StringLength(1000, MinimumLength = 1, ErrorMessage = "短信内容长度必须在 1～1000 之间")]
    public string Content { get; set; } = string.Empty;

    [StringLength(50, ErrorMessage = "模板ID长度不能超过 50")]
    public string? TemplateId { get; set; }

    [StringLength(1000, ErrorMessage = "模板参数长度不能超过 1000")]
    public string? TemplateParams { get; set; }

    [StringLength(50, ErrorMessage = "短信服务商长度不能超过 50")]
    public string? Provider { get; set; }

    public DateTimeOffset? ScheduledTime { get; set; }

    public long? TenantId { get; set; }

    [StringLength(500, ErrorMessage = "备注长度不能超过 500")]
    public string? Remark { get; set; }
}

/// <summary>
/// 更新短信 DTO
/// </summary>
public class SmsUpdateDto : BasicAppUDto
{
    public long? SenderId { get; set; }

    public long? ReceiverId { get; set; }

    public SmsType SmsType { get; set; } = SmsType.Notification;

    [Required(ErrorMessage = "接收手机号不能为空")]
    [StringLength(500, MinimumLength = 1, ErrorMessage = "接收手机号长度必须在 1～500 之间")]
    public string ToPhone { get; set; } = string.Empty;

    [Required(ErrorMessage = "短信内容不能为空")]
    [StringLength(1000, MinimumLength = 1, ErrorMessage = "短信内容长度必须在 1～1000 之间")]
    public string Content { get; set; } = string.Empty;

    [StringLength(50, ErrorMessage = "模板ID长度不能超过 50")]
    public string? TemplateId { get; set; }

    [StringLength(1000, ErrorMessage = "模板参数长度不能超过 1000")]
    public string? TemplateParams { get; set; }

    [StringLength(50, ErrorMessage = "短信服务商长度不能超过 50")]
    public string? Provider { get; set; }

    public SmsStatus SmsStatus { get; set; } = SmsStatus.Pending;

    public DateTimeOffset? ScheduledTime { get; set; }

    public DateTimeOffset? SendTime { get; set; }

    [StringLength(500, ErrorMessage = "备注长度不能超过 500")]
    public string? Remark { get; set; }
}
