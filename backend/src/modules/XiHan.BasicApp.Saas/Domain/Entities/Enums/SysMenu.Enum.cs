// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 菜单类型枚举
/// </summary>
public enum MenuType
{
    /// <summary>
    /// 目录
    /// </summary>
    [Description("目录")]
    Directory = 0,

    /// <summary>
    /// 菜单
    /// </summary>
    [Description("菜单")]
    Menu = 1,

    /// <summary>
    /// 按钮
    /// </summary>
    [Description("按钮")]
    Button = 2
}
