// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.AspNetCore.Authorization;
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
/// Telegram 机器人命令应用服务
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "Telegram机器人")]
public sealed class TelegramBotAppService
    : SaasApplicationService, ITelegramBotAppService
{
    private readonly ITelegramBotDomainService _telegramBotDomainService;

    /// <summary>
    /// 构造函数
    /// </summary>
    public TelegramBotAppService(ITelegramBotDomainService telegramBotDomainService)
    {
        _telegramBotDomainService = telegramBotDomainService;
    }

    /// <summary>
    /// 创建 Telegram 机器人
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.TelegramBot.Create)]
    public async Task<TelegramBotDetailDto> CreateTelegramBotAsync(TelegramBotCreateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _telegramBotDomainService.CreateTelegramBotAsync(
            TelegramBotApplicationMapper.ToCreateCommand(input),
            cancellationToken);

        return TelegramBotApplicationMapper.ToDetailDto(result.Bot);
    }

    /// <summary>
    /// 更新 Telegram 机器人
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.TelegramBot.Update)]
    public async Task<TelegramBotDetailDto> UpdateTelegramBotAsync(TelegramBotUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _telegramBotDomainService.UpdateTelegramBotAsync(
            TelegramBotApplicationMapper.ToUpdateCommand(input),
            cancellationToken);

        return TelegramBotApplicationMapper.ToDetailDto(result.Bot);
    }

    /// <summary>
    /// 更新 Telegram 机器人启停状态
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.TelegramBot.Status)]
    public async Task<TelegramBotDetailDto> UpdateTelegramBotStatusAsync(TelegramBotStatusUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _telegramBotDomainService.UpdateTelegramBotStatusAsync(
            TelegramBotApplicationMapper.ToStatusCommand(input),
            cancellationToken);

        return TelegramBotApplicationMapper.ToDetailDto(result.Bot);
    }

    /// <summary>
    /// 删除 Telegram 机器人
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.TelegramBot.Delete)]
    public async Task DeleteTelegramBotAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _ = await _telegramBotDomainService.DeleteTelegramBotAsync(id, cancellationToken);
    }
}
