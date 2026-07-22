// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 链路追踪时间线结果 DTO
/// </summary>
public sealed class TraceTimelineResultDto
{
    /// <summary>
    /// 时间线条目（已按时间倒序）
    /// </summary>
    public List<TraceTimelineItemDto> Items { get; set; } = [];

    /// <summary>
    /// 各日志类型命中条数（键为 <see cref="TraceLogType"/> 名称）
    /// </summary>
    public Dictionary<string, int> TypeCounts { get; set; } = [];

    /// <summary>
    /// 是否有类型达到单类型上限而被截断
    /// </summary>
    public bool Truncated { get; set; }

    /// <summary>
    /// 合并后总条数
    /// </summary>
    public int TotalCount { get; set; }
}
