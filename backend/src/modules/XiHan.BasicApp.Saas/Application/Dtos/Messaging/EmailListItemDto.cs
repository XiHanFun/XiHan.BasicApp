#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:EmailListItemDto
// Guid:6e068c05-5ab4-4068-b4b7-5d3aec7b1aed
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/01 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 系统邮件列表项 DTO
/// </summary>
public class EmailListItemDto : BasicAppDto
{
    /// <summary>
    /// 发送用户主键
    /// </summary>
    public long? SendUserId { get; set; }

    /// <summary>
    /// 接收用户主键
    /// </summary>
    public long? ReceiveUserId { get; set; }

    /// <summary>
    /// 邮件类型
    /// </summary>
    public EmailType EmailType { get; set; }

    /// <summary>
    /// 邮件主题
    /// </summary>
    public string Subject { get; set; } = string.Empty;

    /// <summary>
    /// 是否 HTML 格式
    /// </summary>
    public bool IsHtml { get; set; }

    /// <summary>
    /// 模板主键
    /// </summary>
    public long? TemplateId { get; set; }

    /// <summary>
    /// 邮件状态
    /// </summary>
    public EmailStatus EmailStatus { get; set; }

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
    public int RetryCount { get; set; }

    /// <summary>
    /// 最大重试次数
    /// </summary>
    public int MaxRetryCount { get; set; }

    /// <summary>
    /// 业务类型
    /// </summary>
    public string? BusinessType { get; set; }

    /// <summary>
    /// 业务主键
    /// </summary>
    public long? BusinessId { get; set; }

    /// <summary>
    /// 是否包含发件地址
    /// </summary>
    public bool HasSenderAddress { get; set; }

    /// <summary>
    /// 是否包含收件地址
    /// </summary>
    public bool HasRecipientAddress { get; set; }

    /// <summary>
    /// 是否包含抄送地址
    /// </summary>
    public bool HasCopyRecipient { get; set; }

    /// <summary>
    /// 是否包含密送地址
    /// </summary>
    public bool HasBlindRecipient { get; set; }

    /// <summary>
    /// 是否包含正文
    /// </summary>
    public bool HasBody { get; set; }

    /// <summary>
    /// 是否包含附件
    /// </summary>
    public bool HasAttachment { get; set; }

    /// <summary>
    /// 是否包含模板数据
    /// </summary>
    public bool HasTemplateData { get; set; }

    /// <summary>
    /// 是否包含失败明细
    /// </summary>
    public bool HasFailureDetail { get; set; }

    /// <summary>
    /// 是否包含备注
    /// </summary>
    public bool HasNote { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTimeOffset CreatedTime { get; set; }

    /// <summary>
    /// 修改时间
    /// </summary>
    public DateTimeOffset? ModifiedTime { get; set; }
}
