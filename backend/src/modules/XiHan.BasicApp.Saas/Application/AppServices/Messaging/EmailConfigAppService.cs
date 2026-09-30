// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.AspNetCore.Authorization;
using XiHan.BasicApp.Saas.Application.Contracts;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.Mappers;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Permissions;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Authorization.AspNetCore;
using XiHan.Framework.Uow.Attributes;
using XiHan.BasicApp.Saas.Application.Services;

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

    private readonly IFieldSecurityService _fieldSecurity;

    /// <summary>
    /// 构造函数
    /// </summary>
    public EmailConfigAppService(IEmailConfigDomainService emailConfigDomainService, IFieldSecurityService fieldSecurity)
    {
        _emailConfigDomainService = emailConfigDomainService;
        _fieldSecurity = fieldSecurity;
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

        // 字段安全：只读字段不能填写
        await _fieldSecurity.EnsureCreatableAsync(typeof(SysEmailConfig), input, cancellationToken);

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

        // 字段安全：只读字段不能改，表单交回的脱敏值还原为原值
        await _fieldSecurity.EnsureUpdatableAsync(typeof(SysEmailConfig), input.BasicId, input, cancellationToken);

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

        // 字段安全：只读字段不能改，表单交回的脱敏值还原为原值
        await _fieldSecurity.EnsureUpdatableAsync(typeof(SysEmailConfig), input.BasicId, input, cancellationToken);

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
