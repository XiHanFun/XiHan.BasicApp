#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IMessageDeliveryService
// Guid:5eb1332e-94ce-4341-aa2e-7f8292bd2f39
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/18 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

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
