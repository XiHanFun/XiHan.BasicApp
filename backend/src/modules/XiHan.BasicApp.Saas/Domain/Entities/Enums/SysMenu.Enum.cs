#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysMenu.Enum
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
    Directory = 0,

    /// <summary>
    /// 菜单
    /// </summary>
    Menu = 1,

    /// <summary>
    /// 按钮
    /// </summary>
    Button = 2
}

