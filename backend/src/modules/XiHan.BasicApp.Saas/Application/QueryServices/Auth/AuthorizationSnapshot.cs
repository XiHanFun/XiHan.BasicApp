// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Saas.Application.QueryServices;

/// <summary>
/// 授权快照
/// </summary>
/// <param name="Roles">当前上下文生效的角色编码</param>
/// <param name="Permissions">当前上下文生效的权限编码（平台超管首位带通配 *）</param>
/// <param name="PermissionIds">当前上下文生效的权限主键</param>
/// <param name="ContextDeniedCodes">
/// 权限目录里作用侧不含当前上下文的权限编码：鉴权、菜单与按钮在判断通配之前先按它拒绝，
/// 平台超管的 * 因此也放不出租户侧权限
/// </param>
public sealed record AuthorizationSnapshot(
    List<string> Roles,
    List<string> Permissions,
    HashSet<long> PermissionIds,
    HashSet<string> ContextDeniedCodes);
