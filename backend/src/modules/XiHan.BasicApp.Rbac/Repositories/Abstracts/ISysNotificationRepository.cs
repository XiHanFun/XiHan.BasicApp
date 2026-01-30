#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ISysNotificationRepository
// Guid:c5d6e7f8-a9b0-1234-5678-901234c90123
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/1/30 0:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Enums;
using XiHan.Framework.Domain.Repositories;

namespace XiHan.BasicApp.Rbac.Repositories.Abstracts;

/// <summary>
/// 通知仓储接口
/// </summary>
/// <remarks>
/// 聚合范围：SysNotification + SysEmail + SysSms
/// </remarks>
public interface ISysNotificationRepository : IAggregateRootRepository<SysNotification, long>
{
    // ========== 通知 ==========

    /// <summary>
    /// 获取用户通知列表
    /// </summary>
    Task<List<SysNotification>> GetByUserIdAsync(long userId, int pageIndex, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取未读通知数
    /// </summary>
    Task<int> GetUnreadCountAsync(long userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 标记为已读
    /// </summary>
    Task MarkAsReadAsync(IEnumerable<long> notificationIds, CancellationToken cancellationToken = default);

    // ========== 邮件 ==========

    /// <summary>
    /// 添加邮件
    /// </summary>
    Task<SysEmail> AddEmailAsync(SysEmail email, CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量添加邮件
    /// </summary>
    Task AddEmailsAsync(IEnumerable<SysEmail> emails, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取待发送邮件
    /// </summary>
    Task<List<SysEmail>> GetPendingEmailsAsync(int count, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新邮件状态
    /// </summary>
    Task UpdateEmailStatusAsync(long emailId, EmailStatus status, string? errorMessage = null, CancellationToken cancellationToken = default);

    // ========== 短信 ==========

    /// <summary>
    /// 添加短信
    /// </summary>
    Task<SysSms> AddSmsAsync(SysSms sms, CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量添加短信
    /// </summary>
    Task AddSmsListAsync(IEnumerable<SysSms> smsList, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取待发送短信
    /// </summary>
    Task<List<SysSms>> GetPendingSmsListAsync(int count, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新短信状态
    /// </summary>
    Task UpdateSmsStatusAsync(long smsId, SmsStatus status, string? errorMessage = null, CancellationToken cancellationToken = default);
}
