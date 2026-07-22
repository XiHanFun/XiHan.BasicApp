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
/// 机器人配置命令应用服务（Webhook 型：钉钉/飞书/企业微信）
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "机器人配置")]
public sealed class BotConfigAppService
    : SaasApplicationService, IBotConfigAppService
{
    private readonly IBotConfigDomainService _botConfigDomainService;

    /// <summary>
    /// 构造函数
    /// </summary>
    public BotConfigAppService(IBotConfigDomainService botConfigDomainService)
    {
        _botConfigDomainService = botConfigDomainService;
    }

    /// <summary>
    /// 创建机器人配置
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.BotConfig.Create)]
    public async Task<BotConfigDetailDto> CreateBotConfigAsync(BotConfigCreateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _botConfigDomainService.CreateBotConfigAsync(
            BotConfigApplicationMapper.ToCreateCommand(input),
            cancellationToken);

        return BotConfigApplicationMapper.ToDetailDto(result.Config);
    }

    /// <summary>
    /// 更新机器人配置
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.BotConfig.Update)]
    public async Task<BotConfigDetailDto> UpdateBotConfigAsync(BotConfigUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _botConfigDomainService.UpdateBotConfigAsync(
            BotConfigApplicationMapper.ToUpdateCommand(input),
            cancellationToken);

        return BotConfigApplicationMapper.ToDetailDto(result.Config);
    }

    /// <summary>
    /// 更新机器人配置启停状态
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.BotConfig.Status)]
    public async Task<BotConfigDetailDto> UpdateBotConfigStatusAsync(BotConfigStatusUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _botConfigDomainService.UpdateBotConfigStatusAsync(
            BotConfigApplicationMapper.ToStatusCommand(input),
            cancellationToken);

        return BotConfigApplicationMapper.ToDetailDto(result.Config);
    }

    /// <summary>
    /// 设置默认机器人配置（同租户同服务商内互斥）
    /// </summary>
    /// <remarks>Set 前缀不在动词约定表，保留完整方法名，默认 POST</remarks>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.BotConfig.Update)]
    public async Task<BotConfigDetailDto> SetDefaultBotConfigAsync(BotConfigDefaultUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _botConfigDomainService.SetDefaultBotConfigAsync(
            BotConfigApplicationMapper.ToDefaultCommand(input),
            cancellationToken);

        return BotConfigApplicationMapper.ToDetailDto(result.Config);
    }

    /// <summary>
    /// 删除机器人配置
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.BotConfig.Delete)]
    public async Task DeleteBotConfigAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _ = await _botConfigDomainService.DeleteBotConfigAsync(id, cancellationToken);
    }
}
