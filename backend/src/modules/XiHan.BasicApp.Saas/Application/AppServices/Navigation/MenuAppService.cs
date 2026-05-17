#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:MenuAppService
// Guid:9cdd51c1-1b3d-4ff7-94fd-1099fc8a5962
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/30 00:00:00
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
/// 菜单命令应用服务
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "菜单")]
public sealed class MenuAppService
    : SaasApplicationService, IMenuAppService
{
    private readonly IMenuDomainService _menuDomainService;

    /// <summary>
    /// 构造函数
    /// </summary>
    public MenuAppService(IMenuDomainService menuDomainService)
    {
        _menuDomainService = menuDomainService;
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

        var result = await _menuDomainService.CreateAsync(ToCreateCommand(input), cancellationToken);
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

        var result = await _menuDomainService.UpdateAsync(ToUpdateCommand(input), cancellationToken);
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

        var result = await _menuDomainService.UpdateStatusAsync(ToStatusCommand(input), cancellationToken);
        return MenuApplicationMapper.ToDetailDto(result.Menu, result.Permission);
    }

    private static MenuCreateCommand ToCreateCommand(MenuCreateDto input)
    {
        return new MenuCreateCommand(
            input.ParentId,
            input.PermissionId,
            input.MenuName,
            input.MenuCode,
            input.MenuType,
            input.Path,
            input.Component,
            input.RouteName,
            input.Redirect,
            input.Icon,
            input.Title,
            input.IsExternal,
            input.ExternalUrl,
            input.IsCache,
            input.IsVisible,
            input.IsAffix,
            input.Badge,
            input.BadgeType,
            input.BadgeDot,
            input.Metadata,
            input.Status,
            input.Sort,
            input.Remark);
    }

    private static MenuStatusChangeCommand ToStatusCommand(MenuStatusUpdateDto input)
    {
        return new MenuStatusChangeCommand(input.BasicId, input.Status, input.Remark);
    }

    private static MenuUpdateCommand ToUpdateCommand(MenuUpdateDto input)
    {
        return new MenuUpdateCommand(
            input.BasicId,
            input.ParentId,
            input.PermissionId,
            input.MenuName,
            input.MenuType,
            input.Path,
            input.Component,
            input.RouteName,
            input.Redirect,
            input.Icon,
            input.Title,
            input.IsExternal,
            input.ExternalUrl,
            input.IsCache,
            input.IsVisible,
            input.IsAffix,
            input.Badge,
            input.BadgeType,
            input.BadgeDot,
            input.Metadata,
            input.Sort,
            input.Remark);
    }
}
