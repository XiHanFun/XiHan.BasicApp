#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysSmsDto
// Guid:b4c5d6e7-f8a9-0123-4567-890b12345678
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/1/8 0:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Dtos.Base;
using XiHan.BasicApp.Rbac.Enums;

namespace XiHan.BasicApp.Rbac.Services.Dtos;

/// <summary>
/// 系统短信创建 DTO
/// </summary>
public class SysSmsCreateDto : RbacCreationDtoBase
{
    /// <summary>
    /// 租户ID
    /// </summary>
    public long? TenantId { get; set; }

    /// <summary>
    /// 发送用户ID
    /// </summary>
    public long? SenderId { get; set; }

    /// <summary>
    /// 接收用户ID
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
    /// 模板ID
    /// </summary>
    public string? TemplateId { get; set; }

    /// <summary>
    /// 模板参数
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
    public long? BusinessId { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}

/// <summary>
/// 系统短信更新 DTO
/// </summary>
public class SysSmsUpdateDto : RbacUpdateDtoBase
{
    /// <summary>
    /// 短信状态
    /// </summary>
    public SmsStatus SmsStatus { get; set; }

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
    public int RetryCount { get; set; }

    /// <summary>
    /// 错误信息
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 费用（分）
    /// </summary>
    public int? Cost { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}

/// <summary>
/// 系统短信查询 DTO
/// </summary>
public class SysSmsGetDto : RbacFullAuditedDtoBase
{
    /// <summary>
    /// 租户ID
    /// </summary>
    public long? TenantId { get; set; }

    /// <summary>
    /// 发送用户ID
    /// </summary>
    public long? SenderId { get; set; }

    /// <summary>
    /// 接收用户ID
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
    /// 模板ID
    /// </summary>
    public string? TemplateId { get; set; }

    /// <summary>
    /// 模板参数
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
    public long? BusinessId { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}
