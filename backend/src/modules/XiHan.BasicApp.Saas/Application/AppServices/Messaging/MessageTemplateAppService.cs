// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.AspNetCore.Authorization;
using XiHan.BasicApp.Saas.Application.Caching;
using XiHan.BasicApp.Saas.Application.Contracts;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.Mappers;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Permissions;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Authorization.AspNetCore;
using XiHan.Framework.Uow.Attributes;

namespace XiHan.BasicApp.Saas.Application.AppServices;

/// <summary>
/// 消息模板命令应用服务
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "消息模板")]
public sealed class MessageTemplateAppService
    : SaasApplicationService, IMessageTemplateAppService
{
    private readonly IMessageTemplateDomainService _messageTemplateDomainService;

    private readonly ISaasCacheInvalidator _cacheInvalidator;

    /// <summary>
    /// 构造函数
    /// </summary>
    public MessageTemplateAppService(
        IMessageTemplateDomainService messageTemplateDomainService,
        ISaasCacheInvalidator cacheInvalidator)
    {
        _messageTemplateDomainService = messageTemplateDomainService;
        _cacheInvalidator = cacheInvalidator;
    }

    /// <summary>
    /// 创建消息模板
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.MessageTemplate.Create)]
    public async Task<MessageTemplateDetailDto> CreateMessageTemplateAsync(MessageTemplateCreateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _messageTemplateDomainService.CreateAsync(MessageTemplateApplicationMapper.ToCreateCommand(input), cancellationToken);
        await _cacheInvalidator.InvalidateMessageTemplateAsync(cancellationToken);
        return MessageTemplateApplicationMapper.ToDetailDto(result.Template);
    }

    /// <summary>
    /// 更新消息模板
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.MessageTemplate.Update)]
    public async Task<MessageTemplateDetailDto> UpdateMessageTemplateAsync(MessageTemplateUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _messageTemplateDomainService.UpdateAsync(MessageTemplateApplicationMapper.ToUpdateCommand(input), cancellationToken);
        await _cacheInvalidator.InvalidateMessageTemplateAsync(cancellationToken);
        return MessageTemplateApplicationMapper.ToDetailDto(result.Template);
    }

    /// <summary>
    /// 更新消息模板状态
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.MessageTemplate.Status)]
    public async Task<MessageTemplateDetailDto> UpdateMessageTemplateStatusAsync(MessageTemplateStatusUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _messageTemplateDomainService.UpdateStatusAsync(MessageTemplateApplicationMapper.ToStatusCommand(input), cancellationToken);
        await _cacheInvalidator.InvalidateMessageTemplateAsync(cancellationToken);
        return MessageTemplateApplicationMapper.ToDetailDto(result.Template);
    }

    /// <summary>
    /// 删除消息模板
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.MessageTemplate.Delete)]
    public async Task DeleteMessageTemplateAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _messageTemplateDomainService.DeleteAsync(id, cancellationToken);
        await _cacheInvalidator.InvalidateMessageTemplateAsync(cancellationToken);
    }
}
