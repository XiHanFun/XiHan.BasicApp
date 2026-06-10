#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:MenuDomainService
// Guid:0968fd7b-6282-48be-a536-1d2760998061
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/18 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using System.Text.Json;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.MultiTenancy.Abstractions;

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 菜单领域服务实现
/// </summary>
public sealed class MenuDomainService
    : IMenuDomainService
{
    private readonly IMenuRepository _menuRepository;

    private readonly IPermissionRepository _permissionRepository;

    private readonly ICurrentTenant _currentTenant;

    /// <summary>
    /// 构造函数
    /// </summary>
    public MenuDomainService(
        IMenuRepository menuRepository,
        IPermissionRepository permissionRepository,
        ICurrentTenant currentTenant)
    {
        _menuRepository = menuRepository;
        _permissionRepository = permissionRepository;
        _currentTenant = currentTenant;
    }

    /// <inheritdoc />
    public async Task<MenuCommandResult> CreateAsync(MenuCreateCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateCreateCommand(command);

        var menuCode = command.MenuCode.Trim();
        if (await _menuRepository.AnyAsync(menu => menu.MenuCode == menuCode, cancellationToken))
        {
            throw new InvalidOperationException("菜单编码已存在。");
        }

        _ = await ValidateParentAsync(command.ParentId, currentMenuId: null, cancellationToken);
        var permission = await ValidatePermissionAsync(command.PermissionId, command.MenuType, cancellationToken);

        var menu = new SysMenu
        {
            ParentId = command.ParentId,
            PermissionId = command.PermissionId,
            MenuName = command.MenuName.Trim(),
            MenuCode = menuCode,
            MenuType = command.MenuType,
            Path = NormalizeNullable(command.Path),
            Component = NormalizeNullable(command.Component),
            RouteName = NormalizeNullable(command.RouteName),
            Redirect = NormalizeNullable(command.Redirect),
            Icon = NormalizeNullable(command.Icon),
            Title = NormalizeNullable(command.Title),
            I18nKey = NormalizeNullable(command.I18nKey),
            IsExternal = command.IsExternal,
            ExternalUrl = command.IsExternal ? NormalizeNullable(command.ExternalUrl) : null,
            IsCache = command.IsCache,
            IsVisible = command.IsVisible,
            IsAffix = command.IsAffix,
            Badge = NormalizeNullable(command.Badge),
            BadgeType = NormalizeNullable(command.BadgeType),
            BadgeDot = command.BadgeDot,
            Metadata = NormalizeMetadata(command.Metadata),
            Status = command.Status,
            Sort = command.Sort,
            Remark = NormalizeNullable(command.Remark)
        };

        var savedMenu = await _menuRepository.AddAsync(menu, cancellationToken);
        return new MenuCommandResult(savedMenu, permission);
    }

    /// <inheritdoc />
    public async Task<MenuCommandResult> UpdateAsync(MenuUpdateCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateUpdateCommand(command);

        var menu = await GetEditableMenuOrThrowAsync(command.BasicId, cancellationToken);
        _ = await ValidateParentAsync(command.ParentId, menu.BasicId, cancellationToken);
        var permission = await ValidatePermissionAsync(command.PermissionId, command.MenuType, cancellationToken);

        menu.ParentId = command.ParentId;
        menu.PermissionId = command.PermissionId;
        menu.MenuName = command.MenuName.Trim();
        menu.MenuType = command.MenuType;
        menu.Path = NormalizeNullable(command.Path);
        menu.Component = NormalizeNullable(command.Component);
        menu.RouteName = NormalizeNullable(command.RouteName);
        menu.Redirect = NormalizeNullable(command.Redirect);
        menu.Icon = NormalizeNullable(command.Icon);
        menu.Title = NormalizeNullable(command.Title);
        menu.I18nKey = NormalizeNullable(command.I18nKey);
        menu.IsExternal = command.IsExternal;
        menu.ExternalUrl = command.IsExternal ? NormalizeNullable(command.ExternalUrl) : null;
        menu.IsCache = command.IsCache;
        menu.IsVisible = command.IsVisible;
        menu.IsAffix = command.IsAffix;
        menu.Badge = NormalizeNullable(command.Badge);
        menu.BadgeType = NormalizeNullable(command.BadgeType);
        menu.BadgeDot = command.BadgeDot;
        menu.Metadata = NormalizeMetadata(command.Metadata);
        menu.Sort = command.Sort;
        menu.Remark = NormalizeNullable(command.Remark);

        var savedMenu = await _menuRepository.UpdateAsync(menu, cancellationToken);
        return new MenuCommandResult(savedMenu, permission);
    }

    /// <inheritdoc />
    public async Task<MenuCommandResult> UpdateStatusAsync(MenuStatusChangeCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        if (command.BasicId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(command), "菜单主键必须大于 0。");
        }

        ValidateEnum(command.Status, nameof(command.Status));

        var menu = await GetEditableMenuOrThrowAsync(command.BasicId, cancellationToken);
        SysPermission? permission = null;
        if (command.Status == EnableStatus.Enabled)
        {
            _ = await ValidateParentAsync(menu.ParentId, menu.BasicId, cancellationToken);
            permission = await ValidatePermissionAsync(menu.PermissionId, menu.MenuType, cancellationToken);
        }
        else if (menu.PermissionId.HasValue)
        {
            permission = await _permissionRepository.GetByIdAsync(menu.PermissionId.Value, cancellationToken);
        }

        menu.Status = command.Status;
        menu.Remark = NormalizeNullable(command.Remark) ?? menu.Remark;

        var savedMenu = await _menuRepository.UpdateAsync(menu, cancellationToken);
        return new MenuCommandResult(savedMenu, permission);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(long id, CancellationToken cancellationToken = default)
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

    private static void ValidateCreateCommand(MenuCreateCommand command)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(command.MenuCode);
        ValidateMenuCode(command.MenuCode);
        ValidateCommonCommand(
            command.MenuName,
            command.MenuType,
            command.Path,
            command.Component,
            command.RouteName,
            command.Redirect,
            command.Icon,
            command.Title,
            command.I18nKey,
            command.IsExternal,
            command.ExternalUrl,
            command.Badge,
            command.BadgeType,
            command.Metadata,
            command.Remark);
        ValidateEnum(command.Status, nameof(command.Status));
    }

    private static void ValidateUpdateCommand(MenuUpdateCommand command)
    {
        if (command.BasicId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(command), "菜单主键必须大于 0。");
        }

        ValidateCommonCommand(
            command.MenuName,
            command.MenuType,
            command.Path,
            command.Component,
            command.RouteName,
            command.Redirect,
            command.Icon,
            command.Title,
            command.I18nKey,
            command.IsExternal,
            command.ExternalUrl,
            command.Badge,
            command.BadgeType,
            command.Metadata,
            command.Remark);
    }

    private static void ValidateCommonCommand(
        string menuName,
        MenuType menuType,
        string? path,
        string? component,
        string? routeName,
        string? redirect,
        string? icon,
        string? title,
        string? i18nKey,
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
        ValidateOptionalLength(i18nKey, 100, nameof(i18nKey), "国际化键不能超过 100 个字符。");
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

    private static bool IsValidMenuCodeChar(char code)
    {
        return code is >= 'a' and <= 'z'
            || code is >= '0' and <= '9'
            || code is ':' or '-' or '_' or '.';
    }

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

    private static void ValidateEnum<TEnum>(TEnum value, string paramName)
        where TEnum : struct, Enum
    {
        if (!Enum.IsDefined(value))
        {
            throw new ArgumentOutOfRangeException(paramName, "枚举值无效。");
        }
    }

    private static void ValidateLength(string value, int maxLength, string paramName, string message)
    {
        if (value.Trim().Length > maxLength)
        {
            throw new ArgumentOutOfRangeException(paramName, message);
        }
    }

    private static void ValidateOptionalLength(string? value, int maxLength, string paramName, string message)
    {
        if (!string.IsNullOrWhiteSpace(value) && value.Trim().Length > maxLength)
        {
            throw new ArgumentOutOfRangeException(paramName, message);
        }
    }

    private static string? NormalizeNullable(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private async Task<SysMenu> GetEditableMenuOrThrowAsync(long id, CancellationToken cancellationToken)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "菜单主键必须大于 0。");
        }

        var menu = await _menuRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("菜单不存在。");

        if (menu.IsGlobal && !_currentTenant.IsPlatformOperation())
        {
            throw new InvalidOperationException("平台级全局菜单仅平台运维态可维护，请切换到平台运维后操作。");
        }

        return menu;
    }

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
}
