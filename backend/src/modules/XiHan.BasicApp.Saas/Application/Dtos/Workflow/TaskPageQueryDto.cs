// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 系统任务分页查询 DTO
/// </summary>
public sealed class TaskPageQueryDto : BasicAppPRDto
{
    /// <summary>
    /// 关键字
    /// </summary>
    public string? Keyword { get; set; }

    /// <summary>
    /// 任务编码
    /// </summary>
    public string? TaskCode { get; set; }

    /// <summary>
    /// 任务分组
    /// </summary>
    public string? TaskGroup { get; set; }

    /// <summary>
    /// 触发类型
    /// </summary>
    public TriggerType? TriggerType { get; set; }

    /// <summary>
    /// 运行状态
    /// </summary>
    public RunTaskStatus? RunTaskStatus { get; set; }

    /// <summary>
    /// 是否允许并发执行
    /// </summary>
    public bool? AllowConcurrent { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    public EnableStatus? Status { get; set; }

    /// <summary>
    /// 下次执行时间起点
    /// </summary>
    public DateTimeOffset? NextRunTimeStart { get; set; }

    /// <summary>
    /// 下次执行时间终点
    /// </summary>
    public DateTimeOffset? NextRunTimeEnd { get; set; }

    /// <summary>
    /// 上次执行时间起点
    /// </summary>
    public DateTimeOffset? LastRunTimeStart { get; set; }

    /// <summary>
    /// 上次执行时间终点
    /// </summary>
    public DateTimeOffset? LastRunTimeEnd { get; set; }
}
