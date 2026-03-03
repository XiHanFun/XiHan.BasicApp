#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IMessageAppService
// Guid:a85d2935-6a52-4cb2-bda8-626fb9f05d6a
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/03/04 14:32:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Application.Dtos;
using XiHan.BasicApp.Rbac.Application.UseCases.Commands;
using XiHan.Framework.Application.Contracts.Services;

namespace XiHan.BasicApp.Rbac.Application.AppServices;

/// <summary>
/// 统一消息服务
/// </summary>
public interface IMessageAppService : IApplicationService
{
    /// <summary>
    /// 发送统一消息（站内通知/邮件/短信）
    /// </summary>
    Task<MessageDispatchResultDto> SendAsync(SendMessageCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取待分发邮件
    /// </summary>
    Task<IReadOnlyList<EmailDto>> GetPendingEmailsAsync(int maxCount = 100, long? tenantId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取待分发短信
    /// </summary>
    Task<IReadOnlyList<SmsDto>> GetPendingSmsAsync(int maxCount = 100, long? tenantId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新邮件分发状态
    /// </summary>
    Task UpdateEmailDispatchStatusAsync(UpdateEmailDispatchStatusCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新短信分发状态
    /// </summary>
    Task UpdateSmsDispatchStatusAsync(UpdateSmsDispatchStatusCommand command, CancellationToken cancellationToken = default);
}
