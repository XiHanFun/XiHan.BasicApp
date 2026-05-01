#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SmsPageQueryDto
// Guid:01e53e0e-8a7d-4d09-9015-6bd72225197d
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/01 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 系统短信分页查询 DTO
/// </summary>
public sealed class SmsPageQueryDto : BasicAppPRDto
{
    /// <summary>
    /// 关键字
    /// </summary>
    public string? Keyword { get; set; }

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
    public SmsType? SmsType { get; set; }

    /// <summary>
    /// 短信状态
    /// </summary>
    public SmsStatus? SmsStatus { get; set; }

    /// <summary>
    /// 模板主键
    /// </summary>
    public long? TemplateId { get; set; }

    /// <summary>
    /// 短信服务商
    /// </summary>
    public string? Provider { get; set; }

    /// <summary>
    /// 业务类型
    /// </summary>
    public string? BusinessType { get; set; }

    /// <summary>
    /// 业务主键
    /// </summary>
    public long? BusinessId { get; set; }

    /// <summary>
    /// 预定发送时间起始
    /// </summary>
    public DateTimeOffset? ScheduledTimeStart { get; set; }

    /// <summary>
    /// 预定发送时间结束
    /// </summary>
    public DateTimeOffset? ScheduledTimeEnd { get; set; }

    /// <summary>
    /// 实际发送时间起始
    /// </summary>
    public DateTimeOffset? SendTimeStart { get; set; }

    /// <summary>
    /// 实际发送时间结束
    /// </summary>
    public DateTimeOffset? SendTimeEnd { get; set; }
}
