#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SmsConfigAppService
// Guid:7a2d5c94-1e38-4b60-8f47-3c9e6d1b5a82
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/02 16:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

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
/// 短信配置命令应用服务
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "短信配置")]
public sealed class SmsConfigAppService
    : SaasApplicationService, ISmsConfigAppService
{
    private readonly ISmsConfigDomainService _smsConfigDomainService;

    /// <summary>
    /// 构造函数
    /// </summary>
    public SmsConfigAppService(ISmsConfigDomainService smsConfigDomainService)
    {
        _smsConfigDomainService = smsConfigDomainService;
    }

    /// <summary>
    /// 创建短信配置
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.SmsConfig.Create)]
    public async Task<SmsConfigDetailDto> CreateSmsConfigAsync(SmsConfigCreateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _smsConfigDomainService.CreateSmsConfigAsync(
            SmsConfigApplicationMapper.ToCreateCommand(input),
            cancellationToken);

        return SmsConfigApplicationMapper.ToDetailDto(result.Config);
    }

    /// <summary>
    /// 更新短信配置
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.SmsConfig.Update)]
    public async Task<SmsConfigDetailDto> UpdateSmsConfigAsync(SmsConfigUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _smsConfigDomainService.UpdateSmsConfigAsync(
            SmsConfigApplicationMapper.ToUpdateCommand(input),
            cancellationToken);

        return SmsConfigApplicationMapper.ToDetailDto(result.Config);
    }

    /// <summary>
    /// 更新短信配置启停状态
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.SmsConfig.Status)]
    public async Task<SmsConfigDetailDto> UpdateSmsConfigStatusAsync(SmsConfigStatusUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _smsConfigDomainService.UpdateSmsConfigStatusAsync(
            SmsConfigApplicationMapper.ToStatusCommand(input),
            cancellationToken);

        return SmsConfigApplicationMapper.ToDetailDto(result.Config);
    }

    /// <summary>
    /// 设置默认短信配置
    /// </summary>
    /// <remarks>Set 前缀不在动词约定表，保留完整方法名，默认 POST</remarks>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.SmsConfig.Update)]
    public async Task<SmsConfigDetailDto> SetDefaultSmsConfigAsync(SmsConfigDefaultUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _smsConfigDomainService.SetDefaultSmsConfigAsync(
            SmsConfigApplicationMapper.ToDefaultCommand(input),
            cancellationToken);

        return SmsConfigApplicationMapper.ToDetailDto(result.Config);
    }

    /// <summary>
    /// 删除短信配置
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.SmsConfig.Delete)]
    public async Task DeleteSmsConfigAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _ = await _smsConfigDomainService.DeleteSmsConfigAsync(id, cancellationToken);
    }
}
