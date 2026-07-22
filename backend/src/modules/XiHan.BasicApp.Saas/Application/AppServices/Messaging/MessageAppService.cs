// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.AspNetCore.Authorization;
using XiHan.BasicApp.Saas.Application.Contracts;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.Mappers;
using XiHan.BasicApp.Saas.Application.Services;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Messaging;
using XiHan.BasicApp.Saas.Domain.Permissions;
using XiHan.BasicApp.Saas.Infrastructure.Messaging;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Authorization.AspNetCore;
using XiHan.Framework.Uow.Attributes;

namespace XiHan.BasicApp.Saas.Application.AppServices;

/// <summary>
/// 系统消息命令应用服务
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "系统消息")]
public sealed class MessageAppService : SaasApplicationService, IMessageAppService
{
    private readonly IMessageDeliveryService _messageDeliveryService;

    private readonly IMessageDomainService _messageDomainService;

    private readonly DbMessageOutbox _messageOutbox;

    /// <summary>
    /// 构造函数
    /// </summary>
    public MessageAppService(
        IMessageDomainService messageDomainService,
        IMessageDeliveryService messageDeliveryService,
        DbMessageOutbox messageOutbox)
    {
        _messageDomainService = messageDomainService;
        _messageDeliveryService = messageDeliveryService;
        _messageOutbox = messageOutbox;
    }

    #region 系统邮件

    /// <summary>
    /// 创建系统邮件
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Message.Create)]
    public async Task<EmailDetailDto> CreateEmailAsync(EmailCreateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _messageDeliveryService.CreateEmailAsync(MessageApplicationMapper.ToCreateCommand(input), cancellationToken);
        return MessageApplicationMapper.ToEmailDetailDto(result.Email);
    }

    /// <summary>
    /// 删除系统邮件
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Message.Delete)]
    public async Task DeleteEmailAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _messageDomainService.DeleteEmailAsync(id, cancellationToken);
    }

    /// <summary>
    /// 更新系统邮件
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Message.Update)]
    public async Task<EmailDetailDto> UpdateEmailAsync(EmailUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _messageDomainService.UpdateEmailAsync(MessageApplicationMapper.ToUpdateCommand(input), cancellationToken);
        return MessageApplicationMapper.ToEmailDetailDto(result.Email);
    }

    /// <summary>
    /// 更新系统邮件状态
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Message.Status)]
    public async Task<EmailDetailDto> UpdateEmailStatusAsync(EmailStatusUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _messageDomainService.UpdateEmailStatusAsync(MessageApplicationMapper.ToStatusCommand(input), cancellationToken);

        // 重发（状态置回 Pending）后必须重新入发件箱队列，否则 Pending 行只有服务重启恢复时才会被重投；
        // EnqueueAsync 在 UoW 提交后才真正入队，保证后台拉到时状态行已可见
        if (result.Email.EmailStatus == EmailStatus.Pending)
        {
            await _messageOutbox.EnqueueAsync(SaasMessageChannelNames.Email, result.Email.BasicId, cancellationToken);
        }

        return MessageApplicationMapper.ToEmailDetailDto(result.Email);
    }

    #endregion

    #region 系统短信

    /// <summary>
    /// 创建系统短信
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Message.Create)]
    public async Task<SmsDetailDto> CreateSmsAsync(SmsCreateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _messageDeliveryService.CreateSmsAsync(MessageApplicationMapper.ToCreateCommand(input), cancellationToken);
        return MessageApplicationMapper.ToSmsDetailDto(result.Sms);
    }

    /// <summary>
    /// 删除系统短信
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Message.Delete)]
    public async Task DeleteSmsAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _messageDomainService.DeleteSmsAsync(id, cancellationToken);
    }

    /// <summary>
    /// 更新系统短信
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Message.Update)]
    public async Task<SmsDetailDto> UpdateSmsAsync(SmsUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _messageDomainService.UpdateSmsAsync(MessageApplicationMapper.ToUpdateCommand(input), cancellationToken);
        return MessageApplicationMapper.ToSmsDetailDto(result.Sms);
    }

    /// <summary>
    /// 更新系统短信状态
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Message.Status)]
    public async Task<SmsDetailDto> UpdateSmsStatusAsync(SmsStatusUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _messageDomainService.UpdateSmsStatusAsync(MessageApplicationMapper.ToStatusCommand(input), cancellationToken);

        // 重发（状态置回 Pending）后必须重新入发件箱队列，否则 Pending 行只有服务重启恢复时才会被重投；
        // EnqueueAsync 在 UoW 提交后才真正入队，保证后台拉到时状态行已可见
        if (result.Sms.SmsStatus == SmsStatus.Pending)
        {
            await _messageOutbox.EnqueueAsync(SaasMessageChannelNames.Sms, result.Sms.BasicId, cancellationToken);
        }

        return MessageApplicationMapper.ToSmsDetailDto(result.Sms);
    }

    #endregion
}
