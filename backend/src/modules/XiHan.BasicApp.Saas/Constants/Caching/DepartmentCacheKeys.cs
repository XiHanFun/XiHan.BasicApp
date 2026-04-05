#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:DepartmentCacheKeys
// Guid:abef809c-fdbe-4fc1-a2d3-8e9f0a1b2c3d
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/06 06:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Saas.Constants.Caching;

/// <summary>
/// 部门模块缓存键
/// </summary>
public static class DepartmentCacheKeys
{
    private const string Module = "Saas:Department";

    /// <summary>
    /// 部门树缓存键
    /// </summary>
    public const string Tree = $"{Module}:Tree";
}
