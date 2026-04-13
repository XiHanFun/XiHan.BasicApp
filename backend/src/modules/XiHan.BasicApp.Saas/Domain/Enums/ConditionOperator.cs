#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ConditionOperator
// Guid:b2c3d4e5-f6a7-8901-bcde-f12345678902
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/14 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Saas.Domain.Enums;

/// <summary>
/// ABAC 条件操作符枚举
/// </summary>
public enum ConditionOperator
{
    /// <summary>
    /// 等于
    /// </summary>
    Equals = 0,

    /// <summary>
    /// 不等于
    /// </summary>
    NotEquals = 1,

    /// <summary>
    /// 大于
    /// </summary>
    GreaterThan = 2,

    /// <summary>
    /// 大于等于
    /// </summary>
    GreaterThanOrEquals = 3,

    /// <summary>
    /// 小于
    /// </summary>
    LessThan = 4,

    /// <summary>
    /// 小于等于
    /// </summary>
    LessThanOrEquals = 5,

    /// <summary>
    /// 包含
    /// </summary>
    Contains = 6,

    /// <summary>
    /// 不包含
    /// </summary>
    NotContains = 7,

    /// <summary>
    /// 在集合中
    /// </summary>
    In = 8,

    /// <summary>
    /// 不在集合中
    /// </summary>
    NotIn = 9,

    /// <summary>
    /// 在范围内
    /// </summary>
    Between = 10,

    /// <summary>
    /// 以…开头
    /// </summary>
    StartsWith = 11,

    /// <summary>
    /// 以…结尾
    /// </summary>
    EndsWith = 12,

    /// <summary>
    /// 为空
    /// </summary>
    IsNull = 13,

    /// <summary>
    /// 不为空
    /// </summary>
    IsNotNull = 14
}
