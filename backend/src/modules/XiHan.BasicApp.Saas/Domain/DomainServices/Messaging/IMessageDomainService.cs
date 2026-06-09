#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IMessageDomainService
// Guid:ce2be87c-084f-4c47-bb80-55f5a21b6f0d
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/18 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 系统消息领域服务
/// </summary>
public interface IMessageDomainService
{
    /// <summary>
    /// 创建发件箱邮件
    /// </summary>
    Task<EmailCommandResult> CreateOutboxEmailAsync(EmailCreateCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除邮件
    /// </summary>
    Task DeleteEmailAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新邮件
    /// </summary>
    Task<EmailCommandResult> UpdateEmailAsync(EmailUpdateCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新邮件状态
    /// </summary>
    Task<EmailCommandResult> UpdateEmailStatusAsync(EmailStatusUpdateCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 创建发件箱短信
    /// </summary>
    Task<SmsCommandResult> CreateOutboxSmsAsync(SmsCreateCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除短信
    /// </summary>
    Task DeleteSmsAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新短信
    /// </summary>
    Task<SmsCommandResult> UpdateSmsAsync(SmsUpdateCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新短信状态
    /// </summary>
    Task<SmsCommandResult> UpdateSmsStatusAsync(SmsStatusUpdateCommand command, CancellationToken cancellationToken = default);
}
