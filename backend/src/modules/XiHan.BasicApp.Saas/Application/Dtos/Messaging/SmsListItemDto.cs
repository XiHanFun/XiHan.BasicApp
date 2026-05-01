#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SmsListItemDto
// Guid:b6a5b33a-7813-4493-817b-7947fb02efc8
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/01 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 系统短信列表项 DTO
/// </summary>
public class SmsListItemDto : BasicAppDto
{
    /// <summary>
    /// 发送用户主键
    /// </summary>
    public long? SenderId { get; set; }

    /// <summary>
    /// 接收用户主键
    /// </summary>
    public long? ReceiverId { get; set; }

    /// <summary>
    /// 短信类型
    /// </summary>
    public SmsType SmsType { get; set; }

    /// <summary>
    /// 模板主键
    /// </summary>
    public long? TemplateId { get; set; }

    /// <summary>
    /// 短信服务商
    /// </summary>
    public string? Provider { get; set; }

    /// <summary>
    /// 短信状态
    /// </summary>
    public SmsStatus SmsStatus { get; set; }

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
    /// 费用
    /// </summary>
    public int? Cost { get; set; }

    /// <summary>
    /// 业务类型
    /// </summary>
    public string? BusinessType { get; set; }

    /// <summary>
    /// 业务主键
    /// </summary>
    public long? BusinessId { get; set; }

    /// <summary>
    /// 是否包含接收号码
    /// </summary>
    public bool HasRecipientPhone { get; set; }

    /// <summary>
    /// 是否包含正文
    /// </summary>
    public bool HasBody { get; set; }

    /// <summary>
    /// 是否包含模板数据
    /// </summary>
    public bool HasTemplateData { get; set; }

    /// <summary>
    /// 是否包含服务商回执
    /// </summary>
    public bool HasProviderReceipt { get; set; }

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
