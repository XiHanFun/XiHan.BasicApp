// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Notification.Domain.Entities;

/// <summary>
/// 邮件与短信发送记录共有的状态字段（重发、取消与发送循环按它统一处理）。
/// </summary>
public interface IMessageRecord
{
    /// <summary>
    /// 记录 ID。
    /// </summary>
    Guid Id { get; }

    /// <summary>
    /// 所属侧。
    /// </summary>
    string OwnerTenantId { get; }

    /// <summary>
    /// 状态。
    /// </summary>
    MessageSendStatus Status { get; set; }

    /// <summary>
    /// 已尝试的次数。
    /// </summary>
    int Attempts { get; set; }

    /// <summary>
    /// 最多尝试的次数。
    /// </summary>
    int MaxAttempts { get; set; }

    /// <summary>
    /// 下次可以尝试的时间。
    /// </summary>
    DateTimeOffset? NextAttemptAt { get; set; }

    /// <summary>
    /// 发出时间。
    /// </summary>
    DateTimeOffset? SentAt { get; set; }

    /// <summary>
    /// 最近一次失败的原因。
    /// </summary>
    string? ErrorMessage { get; set; }

    /// <summary>
    /// 发出时用的配置。
    /// </summary>
    Guid? ConfigId { get; set; }
}
