// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// OAuth应用类型枚举
/// </summary>
public enum OAuthAppType
{
    /// <summary>
    /// Web应用
    /// </summary>
    [Description("Web")]
    Web = 0,

    /// <summary>
    /// 移动应用
    /// </summary>
    [Description("移动")]
    Mobile = 1,

    /// <summary>
    /// 桌面应用
    /// </summary>
    [Description("桌面")]
    Desktop = 2,

    /// <summary>
    /// 服务应用
    /// </summary>
    [Description("服务")]
    Service = 3
}
