#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:MenuCacheKeys
// Guid:9ade7f8b-ecad-4eb0-f1c2-7d8e9f0a1b2c
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/06 06:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Saas.Constants.Caching;

/// <summary>
/// 菜单模块缓存键
/// </summary>
public static class MenuCacheKeys
{
    private const string Module = "Saas:Menu";

    /// <summary>
    /// 菜单树缓存键
    /// </summary>
    public const string Tree = $"{Module}:Tree";
}
