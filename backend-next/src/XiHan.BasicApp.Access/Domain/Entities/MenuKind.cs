// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Access.Domain.Entities;

/// <summary>
/// 菜单节点的种类。
/// </summary>
/// <remarks>
/// 按钮不是菜单节点（方案第 10 节第 3 项：按钮即操作，门控用操作的权限码）。
/// </remarks>
public enum MenuKind
{
    /// <summary>
    /// 页面：由各包的页面登记生成（种子对齐），路由、组件与权限归登记，运营只能调展示。
    /// </summary>
    Page = 0,

    /// <summary>
    /// 目录：运营建的分组节点。
    /// </summary>
    /// <remarks>
    /// 只在顶层，没有路由；裁剪后没有子节点即不下发。
    /// </remarks>
    Directory = 1,

    /// <summary>
    /// 外链：运营建的外部地址。
    /// </summary>
    /// <remarks>
    /// 新标签打开，不生成路由；可在顶层或目录下。
    /// </remarks>
    Link = 2,
}
