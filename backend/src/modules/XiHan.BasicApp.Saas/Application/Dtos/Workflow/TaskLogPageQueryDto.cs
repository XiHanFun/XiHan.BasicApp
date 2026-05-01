#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:TaskLogPageQueryDto
// Guid:8325c2b7-64e6-4e66-a1d5-8ad24053c8f3
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/01 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 任务日志分页查询 DTO
/// </summary>
public sealed class TaskLogPageQueryDto : BasicAppPRDto
{
    /// <summary>
    /// 关键字
    /// </summary>
    public string? Keyword { get; set; }

    /// <summary>
    /// 任务主键
    /// </summary>
    public long? TaskId { get; set; }

    /// <summary>
    /// 任务编码
    /// </summary>
    public string? TaskCode { get; set; }

    /// <summary>
    /// 任务名称
    /// </summary>
    public string? TaskName { get; set; }

    /// <summary>
    /// 执行批次号
    /// </summary>
    public string? BatchNumber { get; set; }

    /// <summary>
    /// 任务状态
    /// </summary>
    public RunTaskStatus? TaskStatus { get; set; }

    /// <summary>
    /// 触发方式
    /// </summary>
    public string? TriggerMode { get; set; }

    /// <summary>
    /// 最小执行耗时（毫秒）
    /// </summary>
    public long? MinExecutionTime { get; set; }

    /// <summary>
    /// 最大执行耗时（毫秒）
    /// </summary>
    public long? MaxExecutionTime { get; set; }

    /// <summary>
    /// 最小重试次数
    /// </summary>
    public int? MinRetryCount { get; set; }

    /// <summary>
    /// 最大重试次数
    /// </summary>
    public int? MaxRetryCount { get; set; }

    /// <summary>
    /// 开始时间起点
    /// </summary>
    public DateTimeOffset? StartTimeStart { get; set; }

    /// <summary>
    /// 开始时间终点
    /// </summary>
    public DateTimeOffset? StartTimeEnd { get; set; }
}
