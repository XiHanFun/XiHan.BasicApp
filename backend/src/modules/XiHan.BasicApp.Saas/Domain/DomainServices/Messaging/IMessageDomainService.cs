// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

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
