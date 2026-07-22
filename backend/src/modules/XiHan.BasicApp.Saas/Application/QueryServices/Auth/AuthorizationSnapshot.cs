// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Saas.Application.QueryServices;

/// <summary>
/// 授权快照
/// </summary>
public sealed record AuthorizationSnapshot(
    List<string> Roles,
    List<string> Permissions,
    HashSet<long> PermissionIds);
