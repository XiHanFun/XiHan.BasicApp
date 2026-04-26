#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright (c)2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysPermissionCondition.Enum
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/26 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using System.ComponentModel;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// ABAC 条件操作符枚举
/// </summary>
public enum ConditionOperator
{
    /// <summary>
    /// 等于
    /// </summary>
    [Description("等于")]
    Equals = 0,

    /// <summary>
    /// 不等于
    /// </summary>
    [Description("不等于")]
    NotEquals = 1,

    /// <summary>
    /// 大于
    /// </summary>
    [Description("大于")]
    GreaterThan = 2,

    /// <summary>
    /// 大于等于
    /// </summary>
    [Description("大于等于")]
    GreaterThanOrEquals = 3,

    /// <summary>
    /// 小于
    /// </summary>
    [Description("小于")]
    LessThan = 4,

    /// <summary>
    /// 小于等于
    /// </summary>
    [Description("小于等于")]
    LessThanOrEquals = 5,

    /// <summary>
    /// 包含
    /// </summary>
    [Description("包含")]
    Contains = 6,

    /// <summary>
    /// 不包含
    /// </summary>
    [Description("不包含")]
    NotContains = 7,

    /// <summary>
    /// 在集合中
    /// </summary>
    [Description("在集合中")]
    In = 8,

    /// <summary>
    /// 不在集合中
    /// </summary>
    [Description("不在集合中")]
    NotIn = 9,

    /// <summary>
    /// 在范围内
    /// </summary>
    [Description("在范围内")]
    Between = 10,

    /// <summary>
    /// 以…开头
    /// </summary>
    [Description("以…开头")]
    StartsWith = 11,

    /// <summary>
    /// 以…结尾
    /// </summary>
    [Description("以…结尾")]
    EndsWith = 12,

    /// <summary>
    /// 为空
    /// </summary>
    [Description("为空")]
    IsNull = 13,

    /// <summary>
    /// 不为空
    /// </summary>
    [Description("不为空")]
    IsNotNull = 14
}
