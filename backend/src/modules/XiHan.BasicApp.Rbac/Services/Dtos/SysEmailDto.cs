#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysEmailDto
// Guid:a3b4c5d6-e7f8-9012-3456-7890a1234567
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/1/8 0:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Dtos.Base;
using XiHan.BasicApp.Rbac.Enums;

namespace XiHan.BasicApp.Rbac.Services.Dtos;

/// <summary>
/// 系统邮件创建 DTO
/// </summary>
public class SysEmailCreateDto : RbacCreationDtoBase
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
    /// 邮件类型
    /// </summary>
    public EmailType EmailType { get; set; } = EmailType.System;

    /// <summary>
    /// 发件人邮箱
    /// </summary>
    public string FromEmail { get; set; } = string.Empty;

    /// <summary>
    /// 发件人姓名
    /// </summary>
    public string? FromName { get; set; }

    /// <summary>
    /// 收件人邮箱
    /// </summary>
    public string ToEmail { get; set; } = string.Empty;

    /// <summary>
    /// 抄送邮箱
    /// </summary>
    public string? CcEmail { get; set; }

    /// <summary>
    /// 密送邮箱
    /// </summary>
    public string? BccEmail { get; set; }

    /// <summary>
    /// 邮件主题
    /// </summary>
    public string Subject { get; set; } = string.Empty;

    /// <summary>
    /// 邮件内容
    /// </summary>
    public string? Content { get; set; }

    /// <summary>
    /// 是否HTML格式
    /// </summary>
    public bool IsHtml { get; set; } = true;

    /// <summary>
    /// 附件路径
    /// </summary>
    public string? Attachments { get; set; }

    /// <summary>
    /// 模板ID
    /// </summary>
    public long? TemplateId { get; set; }

    /// <summary>
    /// 模板参数
    /// </summary>
    public string? TemplateParams { get; set; }

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
/// 系统邮件更新 DTO
/// </summary>
public class SysEmailUpdateDto : RbacUpdateDtoBase
{
    /// <summary>
    /// 邮件状态
    /// </summary>
    public EmailStatus EmailStatus { get; set; }

    /// <summary>
    /// 实际发送时间
    /// </summary>
    public DateTimeOffset? SendTime { get; set; }

    /// <summary>
    /// 重试次数
    /// </summary>
    public int RetryCount { get; set; }

    /// <summary>
    /// 错误信息
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}

/// <summary>
/// 系统邮件查询 DTO
/// </summary>
public class SysEmailGetDto : RbacFullAuditedDtoBase
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
    /// 邮件类型
    /// </summary>
    public EmailType EmailType { get; set; } = EmailType.System;

    /// <summary>
    /// 发件人邮箱
    /// </summary>
    public string FromEmail { get; set; } = string.Empty;

    /// <summary>
    /// 发件人姓名
    /// </summary>
    public string? FromName { get; set; }

    /// <summary>
    /// 收件人邮箱
    /// </summary>
    public string ToEmail { get; set; } = string.Empty;

    /// <summary>
    /// 抄送邮箱
    /// </summary>
    public string? CcEmail { get; set; }

    /// <summary>
    /// 密送邮箱
    /// </summary>
    public string? BccEmail { get; set; }

    /// <summary>
    /// 邮件主题
    /// </summary>
    public string Subject { get; set; } = string.Empty;

    /// <summary>
    /// 邮件内容
    /// </summary>
    public string? Content { get; set; }

    /// <summary>
    /// 是否HTML格式
    /// </summary>
    public bool IsHtml { get; set; } = true;

    /// <summary>
    /// 附件路径
    /// </summary>
    public string? Attachments { get; set; }

    /// <summary>
    /// 模板ID
    /// </summary>
    public long? TemplateId { get; set; }

    /// <summary>
    /// 模板参数
    /// </summary>
    public string? TemplateParams { get; set; }

    /// <summary>
    /// 邮件状态
    /// </summary>
    public EmailStatus EmailStatus { get; set; } = EmailStatus.Pending;

    /// <summary>
    /// 预定发送时间
    /// </summary>
    public DateTimeOffset? ScheduledTime { get; set; }

    /// <summary>
    /// 实际发送时间
    /// </summary>
    public DateTimeOffset? SendTime { get; set; }

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
