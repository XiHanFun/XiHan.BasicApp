#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright (c)2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysUserStatistics.Enum
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/26 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using System.ComponentModel;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 统计时间范围枚举
/// </summary>
public enum StatisticsPeriod
{
    /// <summary>
    /// 今日
    /// </summary>
    [Description("今日")]
    Today = 0,

    /// <summary>
    /// 昨日
    /// </summary>
    [Description("昨日")]
    Yesterday = 1,

    /// <summary>
    /// 本周
    /// </summary>
    [Description("本周")]
    ThisWeek = 2,

    /// <summary>
    /// 上周
    /// </summary>
    [Description("上周")]
    LastWeek = 3,

    /// <summary>
    /// 本月
    /// </summary>
    [Description("本月")]
    ThisMonth = 4,

    /// <summary>
    /// 上月
    /// </summary>
    [Description("上月")]
    LastMonth = 5,

    /// <summary>
    /// 本年
    /// </summary>
    [Description("本年")]
    ThisYear = 6,

    /// <summary>
    /// 去年
    /// </summary>
    [Description("去年")]
    LastYear = 7,

    /// <summary>
    /// 自定义
    /// </summary>
    [Description("自定义")]
    Custom = 99
}
