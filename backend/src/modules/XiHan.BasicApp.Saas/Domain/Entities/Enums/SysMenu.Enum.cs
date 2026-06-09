#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysMenu.Enum
// Guid:095c01b1-ed34-46c1-994e-3e2d937445df
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/26 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

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
