#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:OAuthAppAppService
// Guid:f7d26b52-dfde-4cb5-b358-54d76f64f12c
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
/// OAuth 应用命令应用服务
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "OAuth 应用")]
public sealed class OAuthAppAppService
    : SaasApplicationService, IOAuthAppAppService
{
    private readonly IOAuthAppDomainService _oauthAppDomainService;

    /// <summary>
    /// 构造函数
    /// </summary>
    public OAuthAppAppService(IOAuthAppDomainService oauthAppDomainService)
    {
        _oauthAppDomainService = oauthAppDomainService;
    }

    /// <summary>
    /// 创建 OAuth 应用
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.OAuthApp.Create)]
    public async Task<OAuthAppSecretDto> CreateOAuthAppAsync(OAuthAppCreateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _oauthAppDomainService.CreateOAuthAppAsync(OAuthAppApplicationMapper.ToCreateCommand(input), cancellationToken);
        return OAuthAppApplicationMapper.ToSecretDto(result.App, result.PlaintextSecret ?? string.Empty);
    }

    /// <summary>
    /// 删除 OAuth 应用
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.OAuthApp.Delete)]
    public async Task DeleteOAuthAppAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _oauthAppDomainService.DeleteOAuthAppAsync(id, cancellationToken);
    }

    /// <summary>
    /// 重置 OAuth 应用密钥
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.OAuthApp.Secret)]
    public async Task<OAuthAppSecretDto> RegenerateOAuthAppSecretAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _oauthAppDomainService.RegenerateOAuthAppSecretAsync(id, cancellationToken);
        return OAuthAppApplicationMapper.ToSecretDto(result.App, result.PlaintextSecret ?? string.Empty);
    }

    /// <summary>
    /// 更新 OAuth 应用
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.OAuthApp.Update)]
    public async Task<OAuthAppDetailDto> UpdateOAuthAppAsync(OAuthAppUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _oauthAppDomainService.UpdateOAuthAppAsync(OAuthAppApplicationMapper.ToUpdateCommand(input), cancellationToken);
        return OAuthAppApplicationMapper.ToDetailDto(result.App);
    }

    /// <summary>
    /// 更新 OAuth 应用状态
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.OAuthApp.Status)]
    public async Task<OAuthAppDetailDto> UpdateOAuthAppStatusAsync(OAuthAppStatusUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _oauthAppDomainService.UpdateOAuthAppStatusAsync(
            OAuthAppApplicationMapper.ToStatusCommand(input),
            cancellationToken);
        return OAuthAppApplicationMapper.ToDetailDto(result.App);
    }
}
