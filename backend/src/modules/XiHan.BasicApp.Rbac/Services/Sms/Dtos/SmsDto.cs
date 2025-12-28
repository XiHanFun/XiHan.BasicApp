#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SmsDto
// Guid:w1x2y3z4-a5b6-7890-abcd-ef1234567890
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/12/28 17:45:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core;
using XiHan.BasicApp.Rbac.Enums;
using XiHan.BasicApp.Rbac.Services.Base.Dtos;

namespace XiHan.BasicApp.Rbac.Services.Sms.Dtos;

/// <summary>
/// 短信 DTO
/// </summary>
public class SmsDto : RbacFullAuditedDtoBase
{
    /// <summary>
    /// 租户ID
    /// </summary>
    public XiHanBasicAppIdType? TenantId { get; set; }

    /// <summary>
    /// 发送用户ID
    /// </summary>
    public XiHanBasicAppIdType? SenderId { get; set; }

    /// <summary>
    /// 接收用户ID
    /// </summary>
    public XiHanBasicAppIdType? ReceiverId { get; set; }

    /// <summary>
    /// 短信类型
    /// </summary>
    public SmsType SmsType { get; set; } = SmsType.Notification;

    /// <summary>
    /// 接收手机号（多个用逗号分隔）
    /// </summary>
    public string ToPhone { get; set; } = string.Empty;

    /// <summary>
    /// 短信内容
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 模板ID
    /// </summary>
    public string? TemplateId { get; set; }

    /// <summary>
    /// 模板参数（JSON格式）
    /// </summary>
    public string? TemplateParams { get; set; }

    /// <summary>
    /// 短信服务商
    /// </summary>
    public string? Provider { get; set; }

    /// <summary>
    /// 短信状态
    /// </summary>
    public SmsStatus SmsStatus { get; set; } = SmsStatus.Pending;

    /// <summary>
    /// 预定发送时间
    /// </summary>
    public DateTimeOffset? ScheduledTime { get; set; }

    /// <summary>
    /// 实际发送时间
    /// </summary>
    public DateTimeOffset? SendTime { get; set; }

    /// <summary>
    /// 服务商返回ID
    /// </summary>
    public string? ProviderMessageId { get; set; }

    /// <summary>
    /// 重试次数
    /// </summary>
    public int RetryCount { get; set; } = 0;

    /// <summary>
    /// 最大重试次数
    /// </summary>
    public int MaxRetryCount { get; set; } = 3;

    /// <summary>
    /// 错误信息
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 费用（分）
    /// </summary>
    public int? Cost { get; set; }

    /// <summary>
    /// 业务类型
    /// </summary>
    public string? BusinessType { get; set; }

    /// <summary>
    /// 业务ID
    /// </summary>
    public XiHanBasicAppIdType? BusinessId { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}

/// <summary>
/// 创建短信 DTO
/// </summary>
public class CreateSmsDto : RbacCreationDtoBase
{
    /// <summary>
    /// 租户ID
    /// </summary>
    public XiHanBasicAppIdType? TenantId { get; set; }

    /// <summary>
    /// 发送用户ID
    /// </summary>
    public XiHanBasicAppIdType? SenderId { get; set; }

    /// <summary>
    /// 接收用户ID
    /// </summary>
    public XiHanBasicAppIdType? ReceiverId { get; set; }

    /// <summary>
    /// 短信类型
    /// </summary>
    public SmsType SmsType { get; set; } = SmsType.Notification;

    /// <summary>
    /// 接收手机号（多个用逗号分隔）
    /// </summary>
    public string ToPhone { get; set; } = string.Empty;

    /// <summary>
    /// 短信内容
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 模板ID
    /// </summary>
    public string? TemplateId { get; set; }

    /// <summary>
    /// 模板参数（JSON格式）
    /// </summary>
    public string? TemplateParams { get; set; }

    /// <summary>
    /// 短信服务商
    /// </summary>
    public string? Provider { get; set; }

    /// <summary>
    /// 预定发送时间
    /// </summary>
    public DateTimeOffset? ScheduledTime { get; set; }

    /// <summary>
    /// 最大重试次数
    /// </summary>
    public int MaxRetryCount { get; set; } = 3;

    /// <summary>
    /// 业务类型
    /// </summary>
    public string? BusinessType { get; set; }

    /// <summary>
    /// 业务ID
    /// </summary>
    public XiHanBasicAppIdType? BusinessId { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}

/// <summary>
/// 更新短信 DTO
/// </summary>
public class UpdateSmsDto : RbacUpdateDtoBase
{
    /// <summary>
    /// 短信状态
    /// </summary>
    public SmsStatus? SmsStatus { get; set; }

    /// <summary>
    /// 预定发送时间
    /// </summary>
    public DateTimeOffset? ScheduledTime { get; set; }

    /// <summary>
    /// 错误信息
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}

