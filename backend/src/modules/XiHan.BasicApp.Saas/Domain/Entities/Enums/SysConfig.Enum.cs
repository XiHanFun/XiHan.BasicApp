#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysConfig.Enum
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/26 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using System.ComponentModel;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 配置类型枚举
/// </summary>
public enum ConfigType
{
    /// <summary>
    /// 系统配置
    /// </summary>
    System = 0,

    /// <summary>
    /// 用户配置
    /// </summary>
    User = 1,

    /// <summary>
    /// 应用配置
    /// </summary>
    Application = 2,

    /// <summary>
    /// 业务配置
    /// </summary>
    Business = 3,
}

/// <summary>
/// 配置数据类型枚举
/// </summary>
public enum ConfigDataType
{
    /// <summary>
    /// 字符串
    /// </summary>
    String = 0,

    /// <summary>
    /// 数字
    /// </summary>
    Number = 1,

    /// <summary>
    /// 布尔值
    /// </summary>
    Boolean = 2,

    /// <summary>
    /// JSON对象
    /// </summary>
    Json = 3,

    /// <summary>
    /// 数组
    /// </summary>
    Array = 4
}

