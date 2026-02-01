#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:DeviceType
// Guid:ed28152c-d6e9-4396-addb-b479254bad34
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/08/14 04:45:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Rbac.Enums;

/// <summary>
/// 设备类型枚举
/// </summary>
public enum DeviceType
{
    /// <summary>
    /// 未知
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Web浏览器
    /// </summary>
    Web = 1,

    /// <summary>
    /// iOS移动端
    /// </summary>
    iOS = 2,

    /// <summary>
    /// Android移动端
    /// </summary>
    Android = 3,

    /// <summary>
    /// Windows桌面
    /// </summary>
    Windows = 4,

    /// <summary>
    /// macOS桌面
    /// </summary>
    macOS = 5,

    /// <summary>
    /// Linux桌面
    /// </summary>
    Linux = 6,

    /// <summary>
    /// 平板设备
    /// </summary>
    Tablet = 7,

    /// <summary>
    /// 小程序
    /// </summary>
    MiniProgram = 8,

    /// <summary>
    /// API调用
    /// </summary>
    Api = 9
}
