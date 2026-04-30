#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:MenuApplicationMapper
// Guid:8c13eaab-2a74-4f35-b664-f7a58f36241a
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/30 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Mappers;

/// <summary>
/// 菜单应用层映射器
/// </summary>
public static class MenuApplicationMapper
{
    /// <summary>
    /// 映射菜单列表项
    /// </summary>
    /// <param name="menu">菜单定义</param>
    /// <param name="permission">权限定义</param>
    /// <returns>菜单列表项 DTO</returns>
    public static MenuListItemDto ToListItemDto(SysMenu menu, SysPermission? permission)
    {
        ArgumentNullException.ThrowIfNull(menu);

        return new MenuListItemDto
        {
            BasicId = menu.BasicId,
            ParentId = menu.ParentId,
            PermissionId = menu.PermissionId,
            PermissionCode = permission?.PermissionCode,
            PermissionName = permission?.PermissionName,
            IsGlobal = menu.IsGlobal,
            MenuName = menu.MenuName,
            MenuCode = menu.MenuCode,
            MenuType = menu.MenuType,
            Path = menu.Path,
            Component = menu.Component,
            RouteName = menu.RouteName,
            Icon = menu.Icon,
            Title = menu.Title,
            IsExternal = menu.IsExternal,
            IsCache = menu.IsCache,
            IsVisible = menu.IsVisible,
            IsAffix = menu.IsAffix,
            Status = menu.Status,
            Sort = menu.Sort,
            CreatedTime = menu.CreatedTime,
            ModifiedTime = menu.ModifiedTime
        };
    }

    /// <summary>
    /// 映射菜单详情
    /// </summary>
    /// <param name="menu">菜单定义</param>
    /// <param name="permission">权限定义</param>
    /// <returns>菜单详情 DTO</returns>
    public static MenuDetailDto ToDetailDto(SysMenu menu, SysPermission? permission)
    {
        ArgumentNullException.ThrowIfNull(menu);

        return new MenuDetailDto
        {
            BasicId = menu.BasicId,
            ParentId = menu.ParentId,
            PermissionId = menu.PermissionId,
            PermissionCode = permission?.PermissionCode,
            PermissionName = permission?.PermissionName,
            IsGlobal = menu.IsGlobal,
            MenuName = menu.MenuName,
            MenuCode = menu.MenuCode,
            MenuType = menu.MenuType,
            Path = menu.Path,
            Component = menu.Component,
            RouteName = menu.RouteName,
            Redirect = menu.Redirect,
            Icon = menu.Icon,
            Title = menu.Title,
            IsExternal = menu.IsExternal,
            ExternalUrl = menu.ExternalUrl,
            IsCache = menu.IsCache,
            IsVisible = menu.IsVisible,
            IsAffix = menu.IsAffix,
            Badge = menu.Badge,
            BadgeType = menu.BadgeType,
            BadgeDot = menu.BadgeDot,
            Metadata = menu.Metadata,
            Status = menu.Status,
            Sort = menu.Sort,
            Remark = menu.Remark,
            CreatedTime = menu.CreatedTime,
            CreatedId = menu.CreatedId,
            CreatedBy = menu.CreatedBy,
            ModifiedTime = menu.ModifiedTime,
            ModifiedId = menu.ModifiedId,
            ModifiedBy = menu.ModifiedBy
        };
    }

    /// <summary>
    /// 映射菜单树节点
    /// </summary>
    /// <param name="menu">菜单定义</param>
    /// <param name="permission">权限定义</param>
    /// <returns>菜单树节点 DTO</returns>
    public static MenuTreeNodeDto ToTreeNodeDto(SysMenu menu, SysPermission? permission)
    {
        ArgumentNullException.ThrowIfNull(menu);

        return new MenuTreeNodeDto
        {
            BasicId = menu.BasicId,
            ParentId = menu.ParentId,
            PermissionId = menu.PermissionId,
            PermissionCode = permission?.PermissionCode,
            PermissionName = permission?.PermissionName,
            MenuName = menu.MenuName,
            MenuCode = menu.MenuCode,
            MenuType = menu.MenuType,
            Path = menu.Path,
            Component = menu.Component,
            RouteName = menu.RouteName,
            Redirect = menu.Redirect,
            Icon = menu.Icon,
            Title = menu.Title,
            IsExternal = menu.IsExternal,
            ExternalUrl = menu.ExternalUrl,
            IsCache = menu.IsCache,
            IsVisible = menu.IsVisible,
            IsAffix = menu.IsAffix,
            Status = menu.Status,
            Sort = menu.Sort
        };
    }
}
