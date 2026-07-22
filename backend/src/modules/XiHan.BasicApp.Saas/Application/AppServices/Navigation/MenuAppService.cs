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
/// 菜单命令应用服务
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "菜单")]
public sealed class MenuAppService
    : SaasApplicationService, IMenuAppService
{
    private readonly IMenuDomainService _menuDomainService;

    private readonly ISaasCacheInvalidator _cacheInvalidator;

    /// <summary>
    /// 构造函数
    /// </summary>
    public MenuAppService(IMenuDomainService menuDomainService, ISaasCacheInvalidator cacheInvalidator)
    {
        _menuDomainService = menuDomainService;
        _cacheInvalidator = cacheInvalidator;
    }

    /// <summary>
    /// 创建菜单
    /// </summary>
    /// <param name="input">创建参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>菜单详情</returns>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Menu.Create)]
    public async Task<MenuDetailDto> CreateMenuAsync(MenuCreateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _menuDomainService.CreateAsync(MenuApplicationMapper.ToCreateCommand(input), cancellationToken);
        await _cacheInvalidator.InvalidateNavigationAsync(cancellationToken);
        return MenuApplicationMapper.ToDetailDto(result.Menu, result.Permission);
    }

    /// <summary>
    /// 删除菜单
    /// </summary>
    /// <param name="id">菜单主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Menu.Delete)]
    public async Task DeleteMenuAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _menuDomainService.DeleteAsync(id, cancellationToken);
        await _cacheInvalidator.InvalidateNavigationAsync(cancellationToken);
    }

    /// <summary>
    /// 更新菜单
    /// </summary>
    /// <param name="input">更新参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>菜单详情</returns>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Menu.Update)]
    public async Task<MenuDetailDto> UpdateMenuAsync(MenuUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _menuDomainService.UpdateAsync(MenuApplicationMapper.ToUpdateCommand(input), cancellationToken);
        await _cacheInvalidator.InvalidateNavigationAsync(cancellationToken);
        return MenuApplicationMapper.ToDetailDto(result.Menu, result.Permission);
    }

    /// <summary>
    /// 更新菜单状态
    /// </summary>
    /// <param name="input">状态更新参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>菜单详情</returns>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Menu.Status)]
    public async Task<MenuDetailDto> UpdateMenuStatusAsync(MenuStatusUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _menuDomainService.UpdateStatusAsync(MenuApplicationMapper.ToStatusCommand(input), cancellationToken);
        await _cacheInvalidator.InvalidateNavigationAsync(cancellationToken);
        return MenuApplicationMapper.ToDetailDto(result.Menu, result.Permission);
    }
}
