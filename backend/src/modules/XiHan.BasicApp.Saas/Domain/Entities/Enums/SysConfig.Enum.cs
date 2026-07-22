// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

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
    /// 租户配置
    /// </summary>
    [Description("租户配置")]
    Tenant = 0,

    /// <summary>
    /// 组织配置
    /// </summary>
    [Description("组织配置")]
    Organization = 1,

    /// <summary>
    /// 用户配置
    /// </summary>
    [Description("用户配置")]
    User = 2,

    /// <summary>
    /// 应用配置
    /// </summary>
    [Description("应用配置")]
    Application = 3,

    /// <summary>
    /// 环境配置
    /// </summary>
    [Description("环境配置")]
    Environment = 4,

    /// <summary>
    /// 功能配置
    /// </summary>
    [Description("功能配置")]
    Feature = 5
}
