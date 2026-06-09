#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:MessageAppService
// Guid:c530bd34-c2b1-4ad5-9b8c-651d807a5593
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/06 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.AspNetCore.Authorization;
using XiHan.BasicApp.Saas.Application.Contracts;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.Mappers;
using XiHan.BasicApp.Saas.Application.Services;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Permissions;
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

    /// <summary>
    /// 构造函数
    /// </summary>
    public MessageAppService(
        IMessageDomainService messageDomainService,
        IMessageDeliveryService messageDeliveryService)
    {
        _messageDomainService = messageDomainService;
        _messageDeliveryService = messageDeliveryService;
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
        return MessageApplicationMapper.ToSmsDetailDto(result.Sms);
    }

    #endregion

}
