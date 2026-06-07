#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:PermissionAppService
// Guid:c71b9028-19a3-4c87-9ad7-2f1211906dcc
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/30 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

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
/// 权限定义命令应用服务
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "权限定义")]
public sealed class PermissionAppService
    : SaasApplicationService, IPermissionAppService
{
    /// <summary>
    /// 权限目录领域服务
    /// </summary>
    private readonly IPermissionCatalogDomainService _permissionCatalogDomainService;

    /// <summary>
    /// 权限查询服务
    /// </summary>
    private readonly IPermissionQueryService _permissionQueryService;

    /// <summary>
    /// 缓存失效器
    /// </summary>
    private readonly ISaasCacheInvalidator _cacheInvalidator;

    /// <summary>
    /// 构造函数
    /// </summary>
    public PermissionAppService(
        IPermissionCatalogDomainService permissionCatalogDomainService,
        IPermissionQueryService permissionQueryService,
        ISaasCacheInvalidator cacheInvalidator)
    {
        _permissionCatalogDomainService = permissionCatalogDomainService;
        _permissionQueryService = permissionQueryService;
        _cacheInvalidator = cacheInvalidator;
    }

    /// <summary>
    /// 创建权限定义
    /// </summary>
    /// <param name="input">创建参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>权限详情</returns>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Permission.Create)]
    public async Task<PermissionDetailDto> CreatePermissionAsync(PermissionCreateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _permissionCatalogDomainService.CreatePermissionAsync(PermissionApplicationMapper.ToCreateCommand(input), cancellationToken);

        // 权限新增影响授权快照与菜单可见性，统一全失效
        await _cacheInvalidator.InvalidateAuthorizationAsync(cancellationToken: cancellationToken);
        await _cacheInvalidator.InvalidateNavigationAsync(cancellationToken);

        return await _permissionQueryService.GetPermissionDetailAsync(result.PermissionId, cancellationToken)
            ?? throw new InvalidOperationException("权限定义不存在。");
    }

    /// <summary>
    /// 删除权限定义
    /// </summary>
    /// <param name="id">权限主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Permission.Delete)]
    public async Task DeletePermissionAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await _permissionCatalogDomainService.DeletePermissionAsync(id, cancellationToken);

        // 权限删除影响授权快照与菜单可见性，统一全失效
        await _cacheInvalidator.InvalidateAuthorizationAsync(cancellationToken: cancellationToken);
        await _cacheInvalidator.InvalidateNavigationAsync(cancellationToken);
    }

    /// <summary>
    /// 更新权限定义
    /// </summary>
    /// <param name="input">更新参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>权限详情</returns>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Permission.Update)]
    public async Task<PermissionDetailDto> UpdatePermissionAsync(PermissionUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _permissionCatalogDomainService.UpdatePermissionAsync(PermissionApplicationMapper.ToUpdateCommand(input), cancellationToken);

        // 权限更新影响授权快照与菜单可见性，统一全失效
        await _cacheInvalidator.InvalidateAuthorizationAsync(cancellationToken: cancellationToken);
        await _cacheInvalidator.InvalidateNavigationAsync(cancellationToken);

        return await _permissionQueryService.GetPermissionDetailAsync(result.PermissionId, cancellationToken)
            ?? throw new InvalidOperationException("权限定义不存在。");
    }

    /// <summary>
    /// 更新权限定义状态
    /// </summary>
    /// <param name="input">状态更新参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>权限详情</returns>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Permission.Status)]
    public async Task<PermissionDetailDto> UpdatePermissionStatusAsync(PermissionStatusUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _permissionCatalogDomainService.UpdatePermissionStatusAsync(PermissionApplicationMapper.ToStatusCommand(input), cancellationToken);

        // 权限启停影响授权快照与菜单可见性，统一全失效
        await _cacheInvalidator.InvalidateAuthorizationAsync(cancellationToken: cancellationToken);
        await _cacheInvalidator.InvalidateNavigationAsync(cancellationToken);

        return await _permissionQueryService.GetPermissionDetailAsync(result.PermissionId, cancellationToken)
            ?? throw new InvalidOperationException("权限定义不存在。");
    }
}
