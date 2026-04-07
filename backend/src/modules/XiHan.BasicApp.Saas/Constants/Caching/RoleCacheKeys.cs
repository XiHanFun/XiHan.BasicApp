#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:RoleCacheKeys
// Guid:8fcd6e7a-db9c-4daf-e0b1-6c7d8e9f0a1b
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/06 06:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Saas.Constants.Caching;

/// <summary>
/// 角色模块缓存键
/// </summary>
public static class RoleCacheKeys
{
    private const string Module = "Saas:Role";

    /// <summary>
    /// 角色菜单缓存键
    /// </summary>
    public static string Menus(long roleId) => $"{Module}:Menus:{roleId}";

    /// <summary>
    /// 角色权限缓存键
    /// </summary>
    public static string Permissions(long roleId) => $"{Module}:Permissions:{roleId}";
}
