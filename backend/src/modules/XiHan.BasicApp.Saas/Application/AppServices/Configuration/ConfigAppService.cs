#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ConfigAppService
// Guid:f6e85c17-b95e-4c4b-b7aa-e5561cb8ebd1
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/06 00:00:00
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
/// 系统配置命令应用服务
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "系统配置")]
public sealed class ConfigAppService
    : SaasApplicationService, IConfigAppService
{
    private readonly IConfigDomainService _configDomainService;

    /// <summary>
    /// 构造函数
    /// </summary>
    public ConfigAppService(IConfigDomainService configDomainService)
    {
        _configDomainService = configDomainService;
    }
    /// <summary>
    /// 创建系统配置
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Config.Create)]
    public async Task<ConfigDetailDto> CreateConfigAsync(ConfigCreateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _configDomainService.CreateConfigAsync(ConfigApplicationMapper.ToCreateCommand(input), cancellationToken);
        return ConfigApplicationMapper.ToDetailDto(result.Config);
    }

    /// <summary>
    /// 删除系统配置
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Config.Delete)]
    public async Task DeleteConfigAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _configDomainService.DeleteConfigAsync(id, cancellationToken);
    }

    /// <summary>
    /// 更新系统配置
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Config.Update)]
    public async Task<ConfigDetailDto> UpdateConfigAsync(ConfigUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _configDomainService.UpdateConfigAsync(ConfigApplicationMapper.ToUpdateCommand(input), cancellationToken);
        return ConfigApplicationMapper.ToDetailDto(result.Config);
    }

    /// <summary>
    /// 更新系统配置状态
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Config.Status)]
    public async Task<ConfigDetailDto> UpdateConfigStatusAsync(ConfigStatusUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _configDomainService.UpdateConfigStatusAsync(ConfigApplicationMapper.ToStatusCommand(input), cancellationToken);
        return ConfigApplicationMapper.ToDetailDto(result.Config);
    }
}
