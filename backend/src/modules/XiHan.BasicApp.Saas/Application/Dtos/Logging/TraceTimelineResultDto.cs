#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:TraceTimelineResultDto
// Guid:8c3a6db5-4e70-4f39-a182-1c9d5e7a4f03
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/09 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

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
