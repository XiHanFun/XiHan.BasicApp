#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:VersionAppService
// Guid:c32f63f3-dda0-4288-a51c-acd6ebd326bd
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
/// 系统版本命令应用服务
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "系统版本")]
public sealed class VersionAppService
    : SaasApplicationService, IVersionAppService
{
    private readonly IVersionDomainService _versionDomainService;

    /// <summary>
    /// 构造函数
    /// </summary>
    public VersionAppService(IVersionDomainService versionDomainService)
    {
        _versionDomainService = versionDomainService;
    }

    /// <summary>
    /// 创建系统版本
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Version.Create)]
    public async Task<VersionDetailDto> CreateVersionAsync(VersionCreateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _versionDomainService.CreateVersionAsync(VersionApplicationMapper.ToCreateCommand(input), cancellationToken);
        return VersionApplicationMapper.ToDetailDto(result.Version);
    }

    /// <summary>
    /// 删除系统版本
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Version.Delete)]
    public async Task DeleteVersionAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _versionDomainService.DeleteVersionAsync(id, cancellationToken);
    }

    /// <summary>
    /// 完成系统升级
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Version.Upgrade)]
    public async Task<VersionDetailDto> FinishVersionUpgradeAsync(VersionUpgradeFinishDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _versionDomainService.FinishVersionUpgradeAsync(VersionApplicationMapper.ToUpgradeFinishCommand(input), cancellationToken);
        return VersionApplicationMapper.ToDetailDto(result.Version);
    }

    /// <summary>
    /// 开始系统升级
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Version.Upgrade)]
    public async Task<VersionDetailDto> StartVersionUpgradeAsync(VersionUpgradeStartDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _versionDomainService.StartVersionUpgradeAsync(VersionApplicationMapper.ToUpgradeStartCommand(input), cancellationToken);
        return VersionApplicationMapper.ToDetailDto(result.Version);
    }

    /// <summary>
    /// 更新系统版本
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Version.Update)]
    public async Task<VersionDetailDto> UpdateVersionAsync(VersionUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _versionDomainService.UpdateVersionAsync(VersionApplicationMapper.ToUpdateCommand(input), cancellationToken);
        return VersionApplicationMapper.ToDetailDto(result.Version);
    }
}
