#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:StatisticsPeriod
// Guid:ed28152c-d6e9-4396-addb-b479254bad34
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/08/14 04:45:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Rbac.Enums;

/// <summary>
/// 统计时间范围枚举
/// </summary>
public enum StatisticsPeriod
{
    /// <summary>
    /// 今日
    /// </summary>
    Today = 0,

    /// <summary>
    /// 昨日
    /// </summary>
    Yesterday = 1,

    /// <summary>
    /// 本周
    /// </summary>
    ThisWeek = 2,

    /// <summary>
    /// 上周
    /// </summary>
    LastWeek = 3,

    /// <summary>
    /// 本月
    /// </summary>
    ThisMonth = 4,

    /// <summary>
    /// 上月
    /// </summary>
    LastMonth = 5,

    /// <summary>
    /// 本年
    /// </summary>
    ThisYear = 6,

    /// <summary>
    /// 去年
    /// </summary>
    LastYear = 7,

    /// <summary>
    /// 自定义
    /// </summary>
    Custom = 99
}
