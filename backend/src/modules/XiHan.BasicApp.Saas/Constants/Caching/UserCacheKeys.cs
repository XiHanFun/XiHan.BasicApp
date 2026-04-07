#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:UserCacheKeys
// Guid:7ebc5d6f-ca8b-4c9e-dfa0-5b6c7d8e9f0a
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/06 06:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Saas.Constants.Caching;

/// <summary>
/// 用户模块缓存键
/// </summary>
public static class UserCacheKeys
{
    private const string Module = "Saas:User";

    /// <summary>
    /// 用户权限缓存键
    /// </summary>
    public static string Permissions(long userId) => $"{Module}:Permissions:{userId}";

    /// <summary>
    /// 用户角色缓存键
    /// </summary>
    public static string Roles(long userId) => $"{Module}:Roles:{userId}";

    /// <summary>
    /// 用户菜单缓存键
    /// </summary>
    public static string Menus(long userId) => $"{Module}:Menus:{userId}";
}
