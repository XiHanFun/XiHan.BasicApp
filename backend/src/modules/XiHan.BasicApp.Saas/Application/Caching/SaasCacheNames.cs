#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SaasCacheNames
// Guid:08ee4016-0b6a-4d44-93ee-52c1c7843ea7
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/06 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Saas.Application.Caching;

/// <summary>
/// SaaS 模块缓存名称。
/// </summary>
public static class SaasCacheNames
{
    /// <summary>
    /// 配置值缓存。
    /// </summary>
    public const string ConfigValue = "basicapp:saas:config:value";

    /// <summary>
    /// 用户授权快照缓存。
    /// </summary>
    public const string AuthorizationSnapshot = "basicapp:saas:auth:snapshot";

    /// <summary>
    /// 菜单路由缓存。
    /// </summary>
    public const string MenuRoutes = "basicapp:saas:navigation:routes";
}
