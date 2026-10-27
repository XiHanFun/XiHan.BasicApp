// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Contracts.Seeding;

/// <summary>
/// 种子的阶段号段（方案 5.14，沿用 4.x）：各包在本阶段号段内取 <c>+1</c>、<c>+2</c>…，可选模块按 <see cref="ModuleOffset"/> 偏移。
/// </summary>
public static class SeedOrders
{
    /// <summary>
    /// 平台身份：超级管理员等初始账号。
    /// </summary>
    public const int PlatformIdentity = 100;

    /// <summary>
    /// 操作字典（从操作登记表派生）。
    /// </summary>
    public const int Operations = 200;

    /// <summary>
    /// 权限目录（从权限注册表与描述派生）。
    /// </summary>
    public const int PermissionCatalog = 300;

    /// <summary>
    /// 菜单（从各包页面登记派生）。
    /// </summary>
    public const int Menus = 400;

    /// <summary>
    /// 租户套餐。
    /// </summary>
    public const int Editions = 500;

    /// <summary>
    /// 平台数据（参数、字典、模板等目录类数据）。
    /// </summary>
    public const int PlatformData = 600;

    /// <summary>
    /// 演示数据，只在 <c>BasicApp:Seed:EnableDemoData</c> 打开时执行。
    /// </summary>
    public const int Demo = 900;

    /// <summary>
    /// 可选模块在各阶段内的偏移步长：第一个模块 +10、第二个 +20…。
    /// </summary>
    public const int ModuleOffset = 10;
}
