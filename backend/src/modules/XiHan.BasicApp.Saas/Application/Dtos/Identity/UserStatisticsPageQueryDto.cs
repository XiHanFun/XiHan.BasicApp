#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:UserStatisticsPageQueryDto
// Guid:cc4b1b18-c79a-46c2-9326-0787cf6e9891
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/01 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

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
