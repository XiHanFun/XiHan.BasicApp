#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:AuthorizationSnapshot
// Guid:4a9fd3bc-285d-4f20-860a-89db0988309f
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/18 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Saas.Application.QueryServices;

/// <summary>
/// 授权快照
/// </summary>
public sealed record AuthorizationSnapshot(
    List<string> Roles,
    List<string> Permissions,
    HashSet<long> PermissionIds);
