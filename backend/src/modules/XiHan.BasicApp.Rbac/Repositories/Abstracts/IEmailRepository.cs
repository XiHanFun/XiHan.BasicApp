#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IEmailRepository
// Guid:d2e3f4a5-b6c7-4d5e-8f9a-1b2c3d4e5f6a
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/1/7 0:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Enums;
using XiHan.Framework.Domain.Repositories;

namespace XiHan.BasicApp.Rbac.Repositories.Abstracts;

/// <summary>
/// 邮件仓储接口
/// </summary>
public interface IEmailRepository : IAggregateRootRepository<SysEmail, long>
{
    /// <summary>
    /// 根据收件人邮箱获取邮件列表
    /// </summary>
    /// <param name="toEmail">收件人邮箱</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>邮件列表</returns>
    Task<List<SysEmail>> GetByToEmailAsync(string toEmail, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据发送状态获取邮件列表
    /// </summary>
    /// <param name="smsStatus">发送状态</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>邮件列表</returns>
    Task<List<SysEmail>> GetBySendStatusAsync(SmsStatus smsStatus, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取待发送的邮件列表
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>邮件列表</returns>
    Task<List<SysEmail>> GetPendingEmailsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新邮件发送状态
    /// </summary>
    /// <param name="emailId">邮件ID</param>
    /// <param name="smsStatus">发送状态</param>
    /// <param name="sendTime">发送时间</param>
    /// <param name="sendResult">发送结果</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    Task<bool> UpdateSendStatusAsync(long emailId, SmsStatus smsStatus, DateTimeOffset? sendTime, string? sendResult, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取指定时间段内的邮件列表
    /// </summary>
    /// <param name="startTime">开始时间</param>
    /// <param name="endTime">结束时间</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>邮件列表</returns>
    Task<List<SysEmail>> GetByTimeRangeAsync(DateTimeOffset startTime, DateTimeOffset endTime, CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量插入邮件
    /// </summary>
    /// <param name="emails">邮件列表</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    Task<bool> BatchInsertAsync(List<SysEmail> emails, CancellationToken cancellationToken = default);
}
