#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:TraceTimelineQueryDto
// Guid:6a1e4b93-2c58-4d17-8f60-9a7b3c5e2d81
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/09 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 链路追踪时间线查询 DTO
/// </summary>
/// <remarks>
/// 非分页：按维度 + 时间范围跨多类日志聚合，每类型各取前 <see cref="MaxPerType"/> 条后合并倒序。
/// 时间范围必填（分表需带时间范围），窗口上限由服务端约束。
/// </remarks>
public sealed class TraceTimelineQueryDto
{
    /// <summary>
    /// 追踪维度
    /// </summary>
    public TraceDimension Dimension { get; set; }

    /// <summary>
    /// 维度取值（如用户名 / 会话标识 / TraceId / IP / 用户主键）
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// 开始时间（必填）
    /// </summary>
    public DateTimeOffset? StartTime { get; set; }

    /// <summary>
    /// 结束时间（必填）
    /// </summary>
    public DateTimeOffset? EndTime { get; set; }

    /// <summary>
    /// 参与追踪的日志类型（为空表示全部 = 综合追踪）
    /// </summary>
    public List<TraceLogType>? LogTypes { get; set; }

    /// <summary>
    /// 每类日志最多返回条数（默认 200，上限 500）
    /// </summary>
    public int MaxPerType { get; set; } = 200;
}
