#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysOAuthApp.Enum
// Guid:f6a0dbdd-cd65-4173-9758-c026e094aece
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/26 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

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
    [Description("Web应用")]
    Web = 0,

    /// <summary>
    /// 移动应用
    /// </summary>
    [Description("移动应用")]
    Mobile = 1,

    /// <summary>
    /// 桌面应用
    /// </summary>
    [Description("桌面应用")]
    Desktop = 2,

    /// <summary>
    /// 服务应用
    /// </summary>
    [Description("服务应用")]
    Service = 3
}
