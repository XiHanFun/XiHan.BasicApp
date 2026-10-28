// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Pages;

namespace XiHan.BasicApp.Workbench;

/// <summary>
/// 工作台模块的页面登记。工作台不要求权限（登录即可见，两侧都有），排序 0：菜单树的第一页就是登录后的落点。
/// </summary>
public static class WorkbenchPages
{
    /// <summary>
    /// 工作台（看板）。
    /// </summary>
    public static PageDescriptor Dashboard { get; } = new("workbench.dashboard", "/workbench/dashboard", "workbench.dashboard.title", "lucide:layout-dashboard", 0);

    /// <summary>
    /// 全部顶层页面。
    /// </summary>
    public static IReadOnlyList<PageDescriptor> All { get; } = [Dashboard];
}
