#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:EmailRepository
// Guid:d2e3f4a5-b6c7-4d5e-8f9a-1b2c3d4e5f6a
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/1/8 0:00:00
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
/// 邮件仓储实现
/// </summary>
public class EmailRepository : SqlSugarAggregateRepository<SysEmail, long>, IEmailRepository
{
    private readonly ISqlSugarClient _dbClient;

    /// <summary>
    /// 构造函数
    /// </summary>
    public EmailRepository(ISqlSugarDbContext dbContext, IUnitOfWorkManager unitOfWorkManager)
        : base(dbContext, unitOfWorkManager)
    {
        _dbClient = dbContext.GetClient();
    }

    /// <summary>
    /// 根据收件人邮箱获取邮件列表
    /// </summary>
    public async Task<List<SysEmail>> GetByToEmailAsync(string toEmail, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await _dbClient.Queryable<SysEmail>()
            .Where(e => e.ToEmail == toEmail)
            .OrderBy(e => e.CreatedTime, OrderByType.Desc)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 根据发送状态获取邮件列表
    /// </summary>
    public async Task<List<SysEmail>> GetBySendStatusAsync(EmailStatus emailStatus, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await _dbClient.Queryable<SysEmail>()
            .Where(e => e.EmailStatus == emailStatus)
            .OrderBy(e => e.CreatedTime, OrderByType.Desc)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 获取待发送的邮件列表
    /// </summary>
    public async Task<List<SysEmail>> GetPendingEmailsAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await _dbClient.Queryable<SysEmail>()
            .Where(e => e.EmailStatus == EmailStatus.Pending)
            .OrderBy(e => e.CreatedTime)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 更新邮件发送状态
    /// </summary>
    public async Task<bool> UpdateSendStatusAsync(long emailId, EmailStatus emailStatus, DateTimeOffset? sendTime, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var affectedRows = await _dbClient.Updateable<SysEmail>()
            .SetColumns(e => new SysEmail
            {
                EmailStatus = emailStatus,
                SendTime = sendTime
            })
            .Where(e => e.BasicId == emailId)
            .ExecuteCommandAsync(cancellationToken);

        return affectedRows > 0;
    }

    /// <summary>
    /// 获取指定时间段内的邮件列表
    /// </summary>
    public async Task<List<SysEmail>> GetByTimeRangeAsync(DateTimeOffset startTime, DateTimeOffset endTime, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await _dbClient.Queryable<SysEmail>()
            .Where(e => e.CreatedTime >= startTime && e.CreatedTime <= endTime)
            .OrderBy(e => e.CreatedTime, OrderByType.Desc)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 批量插入邮件
    /// </summary>
    public async Task<bool> BatchInsertAsync(List<SysEmail> emails, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (emails == null || emails.Count == 0)
        {
            return false;
        }

        var affectedRows = await _dbClient.Insertable(emails)
            .ExecuteCommandAsync(cancellationToken);

        return affectedRows > 0;
    }
}
