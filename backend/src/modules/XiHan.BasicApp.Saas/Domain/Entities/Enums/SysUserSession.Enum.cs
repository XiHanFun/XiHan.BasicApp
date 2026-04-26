#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright (c)2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysUserSession.Enum
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/26 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using System.ComponentModel;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 设备类型枚举
/// </summary>
public enum DeviceType
{
    /// <summary>
    /// 未知
    /// </summary>
    [Description("未知")]
    Unknown = 0,

    /// <summary>
    /// Web浏览器
    /// </summary>
    [Description("Web浏览器")]
    Web = 1,

    /// <summary>
    /// iOS移动端
    /// </summary>
    [Description("iOS移动端")]
    iOS = 2,

    /// <summary>
    /// Android移动端
    /// </summary>
    [Description("Android移动端")]
    Android = 3,

    /// <summary>
    /// Windows桌面
    /// </summary>
    [Description("Windows桌面")]
    Windows = 4,

    /// <summary>
    /// macOS桌面
    /// </summary>
    [Description("macOS桌面")]
    macOS = 5,

    /// <summary>
    /// Linux桌面
    /// </summary>
    [Description("Linux桌面")]
    Linux = 6,

    /// <summary>
    /// 平板设备
    /// </summary>
    [Description("平板设备")]
    Tablet = 7,

    /// <summary>
    /// 小程序
    /// </summary>
    [Description("小程序")]
    MiniProgram = 8,

    /// <summary>
    /// API调用
    /// </summary>
    [Description("API调用")]
    Api = 9
}
