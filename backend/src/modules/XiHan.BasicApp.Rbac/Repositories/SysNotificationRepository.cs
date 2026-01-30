#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysNotificationRepository
// Guid:c3d4e5f6-a7b8-9012-3456-789012c78901
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/1/30 0:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Enums;
using XiHan.BasicApp.Rbac.Repositories.Abstracts;
using XiHan.Framework.Data.SqlSugar;
using XiHan.Framework.Data.SqlSugar.Repository;
using XiHan.Framework.Uow;

namespace XiHan.BasicApp.Rbac.Repositories;

/// <summary>
/// 通知仓储实现
/// </summary>
public class SysNotificationRepository : SqlSugarAggregateRepository<SysNotification, long>, ISysNotificationRepository
{
    private readonly ISqlSugarDbContext _dbContext;

    /// <summary>
    /// 构造函数
    /// </summary>
    public SysNotificationRepository(ISqlSugarDbContext dbContext, IUnitOfWorkManager unitOfWorkManager)
        : base(dbContext, unitOfWorkManager)
    {
        _dbContext = dbContext;
    }

    // ========== 通知 ==========

    /// <summary>
    /// 获取用户通知列表
    /// </summary>
    public async Task<List<SysNotification>> GetByUserIdAsync(long userId, int pageIndex, int pageSize, CancellationToken cancellationToken = default)
    {
        return await _dbContext.GetClient().Queryable<SysNotification>()
            .Where(n => n.RecipientUserId == userId)
            .OrderByDescending(n => n.CreatedTime)
            .ToPageListAsync(pageIndex, pageSize, cancellationToken);
    }

    /// <summary>
    /// 获取未读通知数
    /// </summary>
    public async Task<int> GetUnreadCountAsync(long userId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.GetClient().Queryable<SysNotification>()
            .Where(n => n.RecipientUserId == userId && n.NotificationStatus == NotificationStatus.Unread)
            .CountAsync(cancellationToken);
    }

    /// <summary>
    /// 标记为已读
    /// </summary>
    public async Task MarkAsReadAsync(IEnumerable<long> notificationIds, CancellationToken cancellationToken = default)
    {
        await _dbContext.GetClient().Updateable<SysNotification>()
            .SetColumns(n => new SysNotification { NotificationStatus = NotificationStatus.Read, ReadTime = DateTimeOffset.UtcNow })
            .Where(n => notificationIds.Contains(n.BasicId))
            .ExecuteCommandAsync(cancellationToken);
    }

    // ========== 邮件 ==========

    /// <summary>
    /// 添加邮件
    /// </summary>
    public async Task<SysEmail> AddEmailAsync(SysEmail email, CancellationToken cancellationToken = default)
    {
        return await _dbContext.GetClient().Insertable(email).ExecuteReturnEntityAsync();
    }

    /// <summary>
    /// 批量添加邮件
    /// </summary>
    public async Task AddEmailsAsync(IEnumerable<SysEmail> emails, CancellationToken cancellationToken = default)
    {
        await _dbContext.GetClient().Insertable(emails.ToArray()).ExecuteCommandAsync(cancellationToken);
    }

    /// <summary>
    /// 获取待发送邮件
    /// </summary>
    public async Task<List<SysEmail>> GetPendingEmailsAsync(int count, CancellationToken cancellationToken = default)
    {
        return await _dbContext.GetClient().Queryable<SysEmail>()
            .Where(e => e.EmailStatus == EmailStatus.Pending)
            .OrderBy(e => e.CreatedTime)
            .Take(count)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 更新邮件状态
    /// </summary>
    public async Task UpdateEmailStatusAsync(long emailId, EmailStatus status, string? errorMessage = null, CancellationToken cancellationToken = default)
    {
        await _dbContext.GetClient().Updateable<SysEmail>()
            .SetColumns(e => new SysEmail
            {
                EmailStatus = status,
                ErrorMessage = errorMessage,
                SendTime = status == EmailStatus.Success ? DateTimeOffset.UtcNow : null
            })
            .Where(e => e.BasicId == emailId)
            .ExecuteCommandAsync(cancellationToken);
    }

    // ========== 短信 ==========

    /// <summary>
    /// 添加短信
    /// </summary>
    public async Task<SysSms> AddSmsAsync(SysSms sms, CancellationToken cancellationToken = default)
    {
        return await _dbContext.GetClient().Insertable(sms).ExecuteReturnEntityAsync();
    }

    /// <summary>
    /// 批量添加短信
    /// </summary>
    public async Task AddSmsListAsync(IEnumerable<SysSms> smsList, CancellationToken cancellationToken = default)
    {
        await _dbContext.GetClient().Insertable(smsList.ToArray()).ExecuteCommandAsync(cancellationToken);
    }

    /// <summary>
    /// 获取待发送短信
    /// </summary>
    public async Task<List<SysSms>> GetPendingSmsListAsync(int count, CancellationToken cancellationToken = default)
    {
        return await _dbContext.GetClient().Queryable<SysSms>()
            .Where(s => s.SmsStatus == SmsStatus.Pending)
            .OrderBy(s => s.CreatedTime)
            .Take(count)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 更新短信状态
    /// </summary>
    public async Task UpdateSmsStatusAsync(long smsId, SmsStatus status, string? errorMessage = null, CancellationToken cancellationToken = default)
    {
        await _dbContext.GetClient().Updateable<SysSms>()
            .SetColumns(s => new SysSms
            {
                SmsStatus = status,
                ErrorMessage = errorMessage,
                SendTime = status == SmsStatus.Success ? DateTimeOffset.UtcNow : null
            })
            .Where(s => s.BasicId == smsId)
            .ExecuteCommandAsync(cancellationToken);
    }
}
