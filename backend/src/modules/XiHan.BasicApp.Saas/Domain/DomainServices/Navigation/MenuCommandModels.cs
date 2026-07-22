// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 菜单创建命令
/// </summary>
public sealed record MenuCreateCommand(
    long? ParentId,
    long? PermissionId,
    string MenuName,
    string MenuCode,
    MenuType MenuType,
    string? Path,
    string? Component,
    string? RouteName,
    string? Redirect,
    string? Icon,
    string? Title,
    string? I18nKey,
    bool IsExternal,
    string? ExternalUrl,
    bool IsCache,
    bool IsVisible,
    bool IsAffix,
    string? Badge,
    string? BadgeType,
    bool BadgeDot,
    string? Metadata,
    EnableStatus Status,
    int Sort,
    string? Remark);

/// <summary>
/// 菜单更新命令
/// </summary>
public sealed record MenuUpdateCommand(
    long BasicId,
    long? ParentId,
    long? PermissionId,
    string MenuName,
    MenuType MenuType,
    string? Path,
    string? Component,
    string? RouteName,
    string? Redirect,
    string? Icon,
    string? Title,
    string? I18nKey,
    bool IsExternal,
    string? ExternalUrl,
    bool IsCache,
    bool IsVisible,
    bool IsAffix,
    string? Badge,
    string? BadgeType,
    bool BadgeDot,
    string? Metadata,
    int Sort,
    string? Remark);

/// <summary>
/// 菜单状态变更命令
/// </summary>
public sealed record MenuStatusChangeCommand(long BasicId, EnableStatus Status, string? Remark);

/// <summary>
/// 菜单命令结果
/// </summary>
public sealed record MenuCommandResult(SysMenu Menu, SysPermission? Permission);
