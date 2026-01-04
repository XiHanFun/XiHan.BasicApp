#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ISysEmailRepository
// Guid:aab2c3d4-e5f6-7890-abcd-ef1234567899
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/12/28 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Enums;
using XiHan.Framework.Domain.Repositories;

namespace XiHan.BasicApp.Rbac.Repositories.Emails;

/// <summary>
/// 系统邮件仓储接口
/// </summary>
public interface ISysEmailRepository : IRepositoryBase<SysEmail, long>
{
    /// <summary>
    /// 根据邮件状态获取邮件列表
    /// </summary>
    /// <param name="emailStatus">邮件状态</param>
    /// <returns></returns>
    Task<List<SysEmail>> GetByStatusAsync(EmailStatus emailStatus);

    /// <summary>
    /// 根据邮件类型获取邮件列表
    /// </summary>
    /// <param name="emailType">邮件类型</param>
    /// <returns></returns>
    Task<List<SysEmail>> GetByTypeAsync(EmailType emailType);

    /// <summary>
    /// 根据接收邮箱获取邮件列表
    /// </summary>
    /// <param name="toEmail">接收邮箱</param>
    /// <returns></returns>
    Task<List<SysEmail>> GetByToEmailAsync(string toEmail);

    /// <summary>
    /// 根据发送者ID获取邮件列表
    /// </summary>
    /// <param name="senderId">发送者ID</param>
    /// <returns></returns>
    Task<List<SysEmail>> GetBySenderIdAsync(long senderId);

    /// <summary>
    /// 根据接收者ID获取邮件列表
    /// </summary>
    /// <param name="receiverId">接收者ID</param>
    /// <returns></returns>
    Task<List<SysEmail>> GetByReceiverIdAsync(long receiverId);

    /// <summary>
    /// 获取待发送的邮件列表
    /// </summary>
    /// <param name="count">数量</param>
    /// <returns></returns>
    Task<List<SysEmail>> GetPendingEmailsAsync(int count = 100);
}
