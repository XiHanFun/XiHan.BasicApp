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

using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using XiHan.BasicApp.Saas.Application.Contracts;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.Mappers;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Permissions;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Authorization.AspNetCore;
using XiHan.Framework.Uow.Attributes;

namespace XiHan.BasicApp.Saas.Application.AppServices;

/// <summary>
/// 菜单命令应用服务
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "菜单")]
public sealed class MenuAppService(
    IMenuRepository menuRepository,
    IPermissionRepository permissionRepository)
    : SaasApplicationService, IMenuAppService
{
    /// <summary>
    /// 菜单仓储
    /// </summary>
    private readonly IMenuRepository _menuRepository = menuRepository;

    /// <summary>
    /// 权限仓储
    /// </summary>
    private readonly IPermissionRepository _permissionRepository = permissionRepository;

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

        ValidateCreateInput(input);

        var menuCode = input.MenuCode.Trim();
        if (await _menuRepository.AnyAsync(menu => menu.MenuCode == menuCode, cancellationToken))
        {
            throw new InvalidOperationException("菜单编码已存在。");
        }

        _ = await ValidateParentAsync(input.ParentId, currentMenuId: null, cancellationToken);
        var permission = await ValidatePermissionAsync(input.PermissionId, input.MenuType, cancellationToken);

        var menu = new SysMenu
        {
            ParentId = input.ParentId,
            PermissionId = input.PermissionId,
            IsGlobal = false,
            MenuName = input.MenuName.Trim(),
            MenuCode = menuCode,
            MenuType = input.MenuType,
            Path = NormalizeNullable(input.Path),
            Component = NormalizeNullable(input.Component),
            RouteName = NormalizeNullable(input.RouteName),
            Redirect = NormalizeNullable(input.Redirect),
            Icon = NormalizeNullable(input.Icon),
            Title = NormalizeNullable(input.Title),
            IsExternal = input.IsExternal,
            ExternalUrl = input.IsExternal ? NormalizeNullable(input.ExternalUrl) : null,
            IsCache = input.IsCache,
            IsVisible = input.IsVisible,
            IsAffix = input.IsAffix,
            Badge = NormalizeNullable(input.Badge),
            BadgeType = NormalizeNullable(input.BadgeType),
            BadgeDot = input.BadgeDot,
            Metadata = NormalizeMetadata(input.Metadata),
            Status = input.Status,
            Sort = input.Sort,
            Remark = NormalizeNullable(input.Remark)
        };

        var savedMenu = await _menuRepository.AddAsync(menu, cancellationToken);
        return MenuApplicationMapper.ToDetailDto(savedMenu, permission);
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

        ValidateUpdateInput(input);

        var menu = await GetEditableMenuOrThrowAsync(input.BasicId, cancellationToken);
        _ = await ValidateParentAsync(input.ParentId, menu.BasicId, cancellationToken);
        var permission = await ValidatePermissionAsync(input.PermissionId, input.MenuType, cancellationToken);

        menu.ParentId = input.ParentId;
        menu.PermissionId = input.PermissionId;
        menu.MenuName = input.MenuName.Trim();
        menu.MenuType = input.MenuType;
        menu.Path = NormalizeNullable(input.Path);
        menu.Component = NormalizeNullable(input.Component);
        menu.RouteName = NormalizeNullable(input.RouteName);
        menu.Redirect = NormalizeNullable(input.Redirect);
        menu.Icon = NormalizeNullable(input.Icon);
        menu.Title = NormalizeNullable(input.Title);
        menu.IsExternal = input.IsExternal;
        menu.ExternalUrl = input.IsExternal ? NormalizeNullable(input.ExternalUrl) : null;
        menu.IsCache = input.IsCache;
        menu.IsVisible = input.IsVisible;
        menu.IsAffix = input.IsAffix;
        menu.Badge = NormalizeNullable(input.Badge);
        menu.BadgeType = NormalizeNullable(input.BadgeType);
        menu.BadgeDot = input.BadgeDot;
        menu.Metadata = NormalizeMetadata(input.Metadata);
        menu.Sort = input.Sort;
        menu.Remark = NormalizeNullable(input.Remark);

        var savedMenu = await _menuRepository.UpdateAsync(menu, cancellationToken);
        return MenuApplicationMapper.ToDetailDto(savedMenu, permission);
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

        if (input.BasicId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(input), "菜单主键必须大于 0。");
        }

        ValidateEnum(input.Status, nameof(input.Status));

        var menu = await GetEditableMenuOrThrowAsync(input.BasicId, cancellationToken);
        if (input.Status == EnableStatus.Enabled)
        {
            _ = await ValidateParentAsync(menu.ParentId, menu.BasicId, cancellationToken);
            _ = await ValidatePermissionAsync(menu.PermissionId, menu.MenuType, cancellationToken);
        }

        menu.Status = input.Status;
        menu.Remark = NormalizeNullable(input.Remark) ?? menu.Remark;

        var savedMenu = await _menuRepository.UpdateAsync(menu, cancellationToken);
        return await ToDetailDtoAsync(savedMenu, cancellationToken);
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

        var menu = await GetEditableMenuOrThrowAsync(id, cancellationToken);
        if (await _menuRepository.AnyAsync(child => child.ParentId == menu.BasicId, cancellationToken))
        {
            throw new InvalidOperationException("菜单存在子节点，不能直接删除。");
        }

        if (!await _menuRepository.DeleteAsync(menu, cancellationToken))
        {
            throw new InvalidOperationException("菜单删除失败。");
        }
    }

    /// <summary>
    /// 获取可维护菜单
    /// </summary>
    private async Task<SysMenu> GetEditableMenuOrThrowAsync(long id, CancellationToken cancellationToken)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "菜单主键必须大于 0。");
        }

        var menu = await _menuRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("菜单不存在。");

        if (menu.IsGlobal)
        {
            throw new InvalidOperationException("平台级全局菜单必须通过平台运维流程维护。");
        }

        return menu;
    }

    /// <summary>
    /// 校验父级菜单
    /// </summary>
    private async Task<SysMenu?> ValidateParentAsync(long? parentId, long? currentMenuId, CancellationToken cancellationToken)
    {
        if (!parentId.HasValue)
        {
            return null;
        }

        if (parentId.Value <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(parentId), "父级菜单主键必须大于 0。");
        }

        if (currentMenuId.HasValue && parentId.Value == currentMenuId.Value)
        {
            throw new InvalidOperationException("菜单不能选择自身作为父级。");
        }

        var parent = await _menuRepository.GetByIdAsync(parentId.Value, cancellationToken)
            ?? throw new InvalidOperationException("父级菜单不存在。");
        if (parent.MenuType == MenuType.Button)
        {
            throw new InvalidOperationException("按钮不能作为父级菜单。");
        }

        await EnsureNoParentCycleAsync(parent, currentMenuId, cancellationToken);
        return parent;
    }

    /// <summary>
    /// 校验父级菜单不存在环路
    /// </summary>
    private async Task EnsureNoParentCycleAsync(SysMenu parent, long? currentMenuId, CancellationToken cancellationToken)
    {
        if (!currentMenuId.HasValue)
        {
            return;
        }

        var visited = new HashSet<long> { currentMenuId.Value };
        var cursor = parent;
        while (cursor.ParentId.HasValue)
        {
            if (cursor.ParentId.Value == currentMenuId.Value)
            {
                throw new InvalidOperationException("菜单父子关系不能形成环路。");
            }

            if (!visited.Add(cursor.BasicId))
            {
                throw new InvalidOperationException("菜单父子关系存在环路。");
            }

            var next = await _menuRepository.GetByIdAsync(cursor.ParentId.Value, cancellationToken);
            if (next is null)
            {
                return;
            }

            cursor = next;
        }
    }

    /// <summary>
    /// 校验菜单权限
    /// </summary>
    private async Task<SysPermission?> ValidatePermissionAsync(long? permissionId, MenuType menuType, CancellationToken cancellationToken)
    {
        if (menuType == MenuType.Button && (!permissionId.HasValue || permissionId.Value <= 0))
        {
            throw new InvalidOperationException("按钮菜单必须绑定权限。");
        }

        if (!permissionId.HasValue)
        {
            return null;
        }

        if (permissionId.Value <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(permissionId), "权限主键必须大于 0。");
        }

        var permission = await _permissionRepository.GetByIdAsync(permissionId.Value, cancellationToken)
            ?? throw new InvalidOperationException("权限定义不存在。");
        if (permission.Status != EnableStatus.Enabled)
        {
            throw new InvalidOperationException("权限定义未启用。");
        }

        return permission;
    }

    /// <summary>
    /// 转换菜单详情
    /// </summary>
    private async Task<MenuDetailDto> ToDetailDtoAsync(SysMenu menu, CancellationToken cancellationToken)
    {
        var permission = menu.PermissionId.HasValue
            ? await _permissionRepository.GetByIdAsync(menu.PermissionId.Value, cancellationToken)
            : null;

        return MenuApplicationMapper.ToDetailDto(menu, permission);
    }

    /// <summary>
    /// 校验创建参数
    /// </summary>
    private static void ValidateCreateInput(MenuCreateDto input)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(input.MenuCode);
        ValidateMenuCode(input.MenuCode);
        ValidateCommonInput(
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
            input.Badge,
            input.BadgeType,
            input.Metadata,
            input.Remark);
        ValidateEnum(input.Status, nameof(input.Status));
    }

    /// <summary>
    /// 校验更新参数
    /// </summary>
    private static void ValidateUpdateInput(MenuUpdateDto input)
    {
        if (input.BasicId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(input), "菜单主键必须大于 0。");
        }

        ValidateCommonInput(
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
            input.Badge,
            input.BadgeType,
            input.Metadata,
            input.Remark);
    }

    /// <summary>
    /// 校验通用参数
    /// </summary>
    private static void ValidateCommonInput(
        string menuName,
        MenuType menuType,
        string? path,
        string? component,
        string? routeName,
        string? redirect,
        string? icon,
        string? title,
        bool isExternal,
        string? externalUrl,
        string? badge,
        string? badgeType,
        string? metadata,
        string? remark)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(menuName);
        ValidateEnum(menuType, nameof(menuType));
        ValidateLength(menuName, 100, nameof(menuName), "菜单名称不能超过 100 个字符。");
        ValidateOptionalLength(path, 200, nameof(path), "路由地址不能超过 200 个字符。");
        ValidateOptionalLength(component, 200, nameof(component), "组件路径不能超过 200 个字符。");
        ValidateOptionalLength(routeName, 100, nameof(routeName), "路由名称不能超过 100 个字符。");
        ValidateOptionalLength(redirect, 200, nameof(redirect), "重定向地址不能超过 200 个字符。");
        ValidateOptionalLength(icon, 100, nameof(icon), "菜单图标不能超过 100 个字符。");
        ValidateOptionalLength(title, 100, nameof(title), "菜单标题不能超过 100 个字符。");
        ValidateOptionalLength(externalUrl, 500, nameof(externalUrl), "外链地址不能超过 500 个字符。");
        ValidateOptionalLength(badge, 50, nameof(badge), "标签内容不能超过 50 个字符。");
        ValidateOptionalLength(badgeType, 20, nameof(badgeType), "标签类型不能超过 20 个字符。");
        ValidateOptionalLength(remark, 500, nameof(remark), "备注不能超过 500 个字符。");
        _ = NormalizeMetadata(metadata);

        if (isExternal && string.IsNullOrWhiteSpace(externalUrl))
        {
            throw new InvalidOperationException("外链菜单必须填写外链地址。");
        }

        if (menuType == MenuType.Menu && !isExternal && string.IsNullOrWhiteSpace(component))
        {
            throw new InvalidOperationException("非外链菜单必须填写组件路径。");
        }
    }

    /// <summary>
    /// 校验菜单编码
    /// </summary>
    private static void ValidateMenuCode(string menuCode)
    {
        var normalizedMenuCode = menuCode.Trim();
        ValidateLength(normalizedMenuCode, 100, nameof(menuCode), "菜单编码不能超过 100 个字符。");
        if (normalizedMenuCode.Any(char.IsWhiteSpace))
        {
            throw new InvalidOperationException("菜单编码不能包含空白字符。");
        }

        if (normalizedMenuCode.Any(static code => !IsValidMenuCodeChar(code)))
        {
            throw new InvalidOperationException("菜单编码只能包含小写英文、数字、冒号、连字符、下划线或点。");
        }
    }

    /// <summary>
    /// 判断菜单编码字符是否合法
    /// </summary>
    private static bool IsValidMenuCodeChar(char code)
    {
        return code is >= 'a' and <= 'z'
            || code is >= '0' and <= '9'
            || code is ':' or '-' or '_' or '.';
    }

    /// <summary>
    /// 规范化菜单元数据
    /// </summary>
    private static string? NormalizeMetadata(string? metadata)
    {
        var normalized = NormalizeNullable(metadata);
        if (normalized is null)
        {
            return null;
        }

        try
        {
            using var _ = JsonDocument.Parse(normalized);
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException("菜单元数据必须是合法 JSON。", exception);
        }

        return normalized;
    }

    /// <summary>
    /// 校验枚举值
    /// </summary>
    private static void ValidateEnum<TEnum>(TEnum value, string paramName)
        where TEnum : struct, Enum
    {
        if (!Enum.IsDefined(value))
        {
            throw new ArgumentOutOfRangeException(paramName, "枚举值无效。");
        }
    }

    /// <summary>
    /// 校验字符串长度
    /// </summary>
    private static void ValidateLength(string value, int maxLength, string paramName, string message)
    {
        if (value.Trim().Length > maxLength)
        {
            throw new ArgumentOutOfRangeException(paramName, message);
        }
    }

    /// <summary>
    /// 校验可空字符串长度
    /// </summary>
    private static void ValidateOptionalLength(string? value, int maxLength, string paramName, string message)
    {
        if (!string.IsNullOrWhiteSpace(value) && value.Trim().Length > maxLength)
        {
            throw new ArgumentOutOfRangeException(paramName, message);
        }
    }

    /// <summary>
    /// 规范化可空字符串
    /// </summary>
    private static string? NormalizeNullable(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
