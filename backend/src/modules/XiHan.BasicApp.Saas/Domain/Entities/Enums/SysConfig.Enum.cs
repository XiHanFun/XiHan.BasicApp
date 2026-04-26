#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysConfig.Enum
// Guid:d95d3775-a4da-46c3-83a1-65db347c87b0
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/26 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using System.ComponentModel;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 配置数据类型枚举
/// </summary>
public enum ConfigDataType
{
    /// <summary>
    /// 字符串
    /// </summary>
    [Description("字符串")]
    String = 0,

    /// <summary>
    /// 数字
    /// </summary>
    [Description("数字")]
    Number = 1,

    /// <summary>
    /// 布尔值
    /// </summary>
    [Description("布尔值")]
    Boolean = 2,

    /// <summary>
    /// JSON对象
    /// </summary>
    [Description("JSON对象")]
    Json = 3,

    /// <summary>
    /// 数组
    /// </summary>
    [Description("数组")]
    Array = 4
}

/// <summary>
/// 配置类型枚举
/// </summary>
public enum ConfigType
{
    /// <summary>
    /// 系统配置
    /// </summary>
    [Description("系统配置")]
    System = 0,

    /// <summary>
    /// 用户配置
    /// </summary>
    [Description("用户配置")]
    User = 1,

    /// <summary>
    /// 应用配置
    /// </summary>
    [Description("应用配置")]
    Application = 2,

    /// <summary>
    /// 业务配置
    /// </summary>
    [Description("业务配置")]
    Business = 3,
}
