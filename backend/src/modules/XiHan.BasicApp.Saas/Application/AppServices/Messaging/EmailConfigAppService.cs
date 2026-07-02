#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:EmailConfigAppService
// Guid:6f1c4b83-0d27-4a59-9e36-2b8d5c0a4f71
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
/// 邮件配置命令应用服务
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "邮件配置")]
public sealed class EmailConfigAppService
    : SaasApplicationService, IEmailConfigAppService
{
    private readonly IEmailConfigDomainService _emailConfigDomainService;

    /// <summary>
    /// 构造函数
    /// </summary>
    public EmailConfigAppService(IEmailConfigDomainService emailConfigDomainService)
    {
        _emailConfigDomainService = emailConfigDomainService;
    }

    /// <summary>
    /// 创建邮件配置
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.EmailConfig.Create)]
    public async Task<EmailConfigDetailDto> CreateEmailConfigAsync(EmailConfigCreateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _emailConfigDomainService.CreateEmailConfigAsync(
            EmailConfigApplicationMapper.ToCreateCommand(input),
            cancellationToken);

        return EmailConfigApplicationMapper.ToDetailDto(result.Config);
    }

    /// <summary>
    /// 更新邮件配置
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.EmailConfig.Update)]
    public async Task<EmailConfigDetailDto> UpdateEmailConfigAsync(EmailConfigUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _emailConfigDomainService.UpdateEmailConfigAsync(
            EmailConfigApplicationMapper.ToUpdateCommand(input),
            cancellationToken);

        return EmailConfigApplicationMapper.ToDetailDto(result.Config);
    }

    /// <summary>
    /// 更新邮件配置启停状态
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.EmailConfig.Status)]
    public async Task<EmailConfigDetailDto> UpdateEmailConfigStatusAsync(EmailConfigStatusUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _emailConfigDomainService.UpdateEmailConfigStatusAsync(
            EmailConfigApplicationMapper.ToStatusCommand(input),
            cancellationToken);

        return EmailConfigApplicationMapper.ToDetailDto(result.Config);
    }

    /// <summary>
    /// 设置默认邮件配置
    /// </summary>
    /// <remarks>Set 前缀不在动词约定表，保留完整方法名，默认 POST</remarks>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.EmailConfig.Update)]
    public async Task<EmailConfigDetailDto> SetDefaultEmailConfigAsync(EmailConfigDefaultUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _emailConfigDomainService.SetDefaultEmailConfigAsync(
            EmailConfigApplicationMapper.ToDefaultCommand(input),
            cancellationToken);

        return EmailConfigApplicationMapper.ToDetailDto(result.Config);
    }

    /// <summary>
    /// 删除邮件配置
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.EmailConfig.Delete)]
    public async Task DeleteEmailConfigAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _ = await _emailConfigDomainService.DeleteEmailConfigAsync(id, cancellationToken);
    }
}
