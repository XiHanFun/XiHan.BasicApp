// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 用户统计分页查询 DTO
/// </summary>
public sealed class UserStatisticsPageQueryDto : BasicAppPRDto
{
    /// <summary>
    /// 用户主键
    /// </summary>
    public long? UserId { get; set; }

    /// <summary>
    /// 统计时间范围
    /// </summary>
    public StatisticsPeriod? Period { get; set; }

    /// <summary>
    /// 统计开始日期
    /// </summary>
    public DateOnly? StatisticsDateStart { get; set; }

    /// <summary>
    /// 统计结束日期
    /// </summary>
    public DateOnly? StatisticsDateEnd { get; set; }
}
