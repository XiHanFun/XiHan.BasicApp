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
    /// <summary>
    /// 发送者标识
    /// </summary>
    public long? SenderId { get; set; }

    /// <summary>
    /// 接收者标识
    /// </summary>
    public long? ReceiverId { get; set; }

    /// <summary>
    /// 短信类型
    /// </summary>
    public SmsType SmsType { get; set; } = SmsType.Notification;

    /// <summary>
    /// 接收手机号
    /// </summary>
    public string ToPhone { get; set; } = string.Empty;

    /// <summary>
    /// 短信内容
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 短信状态
    /// </summary>
    public SmsStatus SmsStatus { get; set; } = SmsStatus.Pending;

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
/// 创建短信 DTO
/// </summary>
public class SmsCreateDto : BasicAppCDto
{
    /// <summary>
    /// 发送者标识
    /// </summary>
    public long? SenderId { get; set; }

    /// <summary>
    /// 接收者标识
    /// </summary>
    public long? ReceiverId { get; set; }

    /// <summary>
    /// 短信类型
    /// </summary>
    public SmsType SmsType { get; set; } = SmsType.Notification;

    /// <summary>
    /// 接收手机号
    /// </summary>
    [Required(ErrorMessage = "接收手机号不能为空")]
    [StringLength(500, MinimumLength = 1, ErrorMessage = "接收手机号长度必须在 1～500 之间")]
    public string ToPhone { get; set; } = string.Empty;

    /// <summary>
    /// 短信内容
    /// </summary>
    [Required(ErrorMessage = "短信内容不能为空")]
    [StringLength(1000, MinimumLength = 1, ErrorMessage = "短信内容长度必须在 1～1000 之间")]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 模板标识
    /// </summary>
    [StringLength(50, ErrorMessage = "模板ID长度不能超过 50")]
    public long? TemplateId { get; set; }

    /// <summary>
    /// 模板参数
    /// </summary>
    [StringLength(1000, ErrorMessage = "模板参数长度不能超过 1000")]
    public string? TemplateParams { get; set; }

    /// <summary>
    /// 短信服务商
    /// </summary>
    [StringLength(50, ErrorMessage = "短信服务商长度不能超过 50")]
    public string? Provider { get; set; }

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
/// 更新短信 DTO
/// </summary>
public class SmsUpdateDto : BasicAppUDto
{
    /// <summary>
    /// 发送者标识
    /// </summary>
    public long? SenderId { get; set; }

    /// <summary>
    /// 接收者标识
    /// </summary>
    public long? ReceiverId { get; set; }

    /// <summary>
    /// 短信类型
    /// </summary>
    public SmsType SmsType { get; set; } = SmsType.Notification;

    /// <summary>
    /// 接收手机号
    /// </summary>
    [Required(ErrorMessage = "接收手机号不能为空")]
    [StringLength(500, MinimumLength = 1, ErrorMessage = "接收手机号长度必须在 1～500 之间")]
    public string ToPhone { get; set; } = string.Empty;

    /// <summary>
    /// 短信内容
    /// </summary>
    [Required(ErrorMessage = "短信内容不能为空")]
    [StringLength(1000, MinimumLength = 1, ErrorMessage = "短信内容长度必须在 1～1000 之间")]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 模板标识
    /// </summary>
    [StringLength(50, ErrorMessage = "模板ID长度不能超过 50")]
    public long? TemplateId { get; set; }

    /// <summary>
    /// 模板参数
    /// </summary>
    [StringLength(1000, ErrorMessage = "模板参数长度不能超过 1000")]
    public string? TemplateParams { get; set; }

    /// <summary>
    /// 短信服务商
    /// </summary>
    [StringLength(50, ErrorMessage = "短信服务商长度不能超过 50")]
    public string? Provider { get; set; }

    /// <summary>
    /// 短信状态
    /// </summary>
    public SmsStatus SmsStatus { get; set; } = SmsStatus.Pending;

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
