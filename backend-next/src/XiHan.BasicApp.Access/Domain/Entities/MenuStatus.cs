// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Access.Domain.Entities;

/// <summary>
/// 菜单节点的启停状态
/// </summary>
/// <remarks>
/// 与「在导航里显示」是两回事：隐藏的页面仍有路由、可直接访问；停用的节点连同子树不下发，页面没有路由
/// </remarks>
public enum MenuStatus
{
    /// <summary>
    /// 启用
    /// </summary>
    Active = 0,

    /// <summary>
    /// 停用
    /// </summary>
    Disabled = 1,
}
