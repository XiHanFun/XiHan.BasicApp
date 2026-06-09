#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:EmailPageQueryDto
// Guid:e0b448b9-638f-4096-838b-004907dd55b8
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/01 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 系统邮件分页查询 DTO
/// </summary>
public sealed class EmailPageQueryDto : BasicAppPRDto
{
    /// <summary>
    /// 关键字
    /// </summary>
    public string? Keyword { get; set; }

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
    public EmailType? EmailType { get; set; }

    /// <summary>
    /// 邮件状态
    /// </summary>
    public EmailStatus? EmailStatus { get; set; }

    /// <summary>
    /// 模板主键
    /// </summary>
    public long? TemplateId { get; set; }

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
