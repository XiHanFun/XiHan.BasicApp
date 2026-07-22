// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.DomainServices;

namespace XiHan.BasicApp.Saas.Application.Services;

/// <summary>
/// 系统消息投递服务
/// </summary>
public interface IMessageDeliveryService
{
    /// <summary>
    /// 创建并投递邮件
    /// </summary>
    Task<EmailCommandResult> CreateEmailAsync(EmailCreateCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 创建并投递短信
    /// </summary>
    Task<SmsCommandResult> CreateSmsAsync(SmsCreateCommand command, CancellationToken cancellationToken = default);
}
