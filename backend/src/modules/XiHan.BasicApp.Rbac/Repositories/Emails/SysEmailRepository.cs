#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysEmailRepository
// Guid:bab2c3d4-e5f6-7890-abcd-ef1234567899
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/12/28 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Enums;
using XiHan.Framework.Data.SqlSugar;
using XiHan.Framework.Data.SqlSugar.Repository;

namespace XiHan.BasicApp.Rbac.Repositories.Emails;

/// <summary>
/// 系统邮件仓储实现
/// </summary>
public class SysEmailRepository : SqlSugarRepositoryBase<SysEmail, long>, ISysEmailRepository
{
    private readonly ISqlSugarDbContext _dbContext;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="dbContext">数据库上下文</param>
    public SysEmailRepository(ISqlSugarDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// 根据邮件状态获取邮件列表
    /// </summary>
    public async Task<List<SysEmail>> GetByStatusAsync(EmailStatus emailStatus)
    {
        var result = await GetListAsync(email => email.EmailStatus == emailStatus);
        return [.. result.OrderByDescending(email => email.CreatedTime)];
    }

    /// <summary>
    /// 根据邮件类型获取邮件列表
    /// </summary>
    public async Task<List<SysEmail>> GetByTypeAsync(EmailType emailType)
    {
        var result = await GetListAsync(email => email.EmailType == emailType);
        return [.. result.OrderByDescending(email => email.CreatedTime)];
    }

    /// <summary>
    /// 根据接收邮箱获取邮件列表
    /// </summary>
    public async Task<List<SysEmail>> GetByToEmailAsync(string toEmail)
    {
        var result = await GetListAsync(email => email.ToEmail.Contains(toEmail));
        return [.. result.OrderByDescending(email => email.SendTime ?? email.CreatedTime)];
    }

    /// <summary>
    /// 根据发送者ID获取邮件列表
    /// </summary>
    public async Task<List<SysEmail>> GetBySenderIdAsync(long senderId)
    {
        var result = await GetListAsync(email => email.SenderId == senderId);
        return [.. result.OrderByDescending(email => email.SendTime ?? email.CreatedTime)];
    }

    /// <summary>
    /// 根据接收者ID获取邮件列表
    /// </summary>
    public async Task<List<SysEmail>> GetByReceiverIdAsync(long receiverId)
    {
        var result = await GetListAsync(email => email.ReceiverId == receiverId);
        return [.. result.OrderByDescending(email => email.SendTime ?? email.CreatedTime)];
    }

    /// <summary>
    /// 获取待发送的邮件列表
    /// </summary>
    public async Task<List<SysEmail>> GetPendingEmailsAsync(int count = 100)
    {
        return await _dbContext.GetClient()
            .Queryable<SysEmail>()
            .Where(email => email.EmailStatus == EmailStatus.Pending)
            .OrderBy(email => email.ScheduledTime ?? email.CreatedTime)
            .Take(count)
            .ToListAsync();
    }
}
