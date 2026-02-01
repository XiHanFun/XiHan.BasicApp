#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:OAuthAppType
// Guid:ed28152c-d6e9-4396-addb-b479254bad34
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/08/14 04:45:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Rbac.Enums;

/// <summary>
/// OAuth应用类型枚举
/// </summary>
public enum OAuthAppType
{
    /// <summary>
    /// Web应用
    /// </summary>
    Web = 0,

    /// <summary>
    /// 移动应用
    /// </summary>
    Mobile = 1,

    /// <summary>
    /// 桌面应用
    /// </summary>
    Desktop = 2,

    /// <summary>
    /// 服务应用
    /// </summary>
    Service = 3
}
