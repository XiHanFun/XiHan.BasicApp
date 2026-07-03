#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:WorkbenchDashboardSummaryDto
// Guid:58d8b031-9f1d-487d-b5c2-b518be5e1f4d
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/07 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 工作台仪表盘摘要 DTO
/// </summary>
public sealed class WorkbenchDashboardSummaryDto
{
    /// <summary>
    /// 当前用户统计摘要
    /// </summary>
    public WorkbenchDashboardStatisticsDto Statistics { get; set; } = new();

    /// <summary>
    /// 生成时间
    /// </summary>
    public DateTimeOffset GeneratedTime { get; set; }
}

/// <summary>
/// 工作台仪表盘统计 DTO
/// </summary>
public sealed class WorkbenchDashboardStatisticsDto
{
    /// <summary>
    /// 统计日期
    /// </summary>
    public DateOnly StatisticsDate { get; set; }

    /// <summary>
    /// 统计时间范围
    /// </summary>
    public StatisticsPeriod Period { get; set; } = StatisticsPeriod.Today;

    /// <summary>
    /// 登录次数
    /// </summary>
    public int LoginCount { get; set; }

    /// <summary>
    /// 访问次数
    /// </summary>
    public int AccessCount { get; set; }

    /// <summary>
    /// 在线时长（秒）
    /// </summary>
    public long OnlineTime { get; set; }

    /// <summary>
    /// 操作次数
    /// </summary>
    public int OperationCount { get; set; }

    /// <summary>
    /// API 调用次数
    /// </summary>
    public int ApiCallCount { get; set; }

    /// <summary>
    /// 错误操作次数
    /// </summary>
    public int ErrorOperationCount { get; set; }

    /// <summary>
    /// 最后登录时间
    /// </summary>
    public DateTimeOffset? LastLoginTime { get; set; }

    /// <summary>
    /// 最后访问时间
    /// </summary>
    public DateTimeOffset? LastAccessTime { get; set; }

    /// <summary>
    /// 最后操作时间
    /// </summary>
    public DateTimeOffset? LastOperationTime { get; set; }
}
