#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:TaskLogListItemDto
// Guid:ce4e8dcb-b8a9-459f-9f72-65dcb4045321
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/01 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 任务日志列表项 DTO
/// </summary>
public class TaskLogListItemDto : BasicAppDto
{
    /// <summary>
    /// 任务主键
    /// </summary>
    public long TaskId { get; set; }

    /// <summary>
    /// 任务编码
    /// </summary>
    public string TaskCode { get; set; } = string.Empty;

    /// <summary>
    /// 任务名称
    /// </summary>
    public string TaskName { get; set; } = string.Empty;

    /// <summary>
    /// 执行批次号
    /// </summary>
    public string? BatchNumber { get; set; }

    /// <summary>
    /// 任务状态
    /// </summary>
    public RunTaskStatus TaskStatus { get; set; }

    /// <summary>
    /// 开始时间
    /// </summary>
    public DateTimeOffset StartTime { get; set; }

    /// <summary>
    /// 结束时间
    /// </summary>
    public DateTimeOffset? EndTime { get; set; }

    /// <summary>
    /// 执行时长（毫秒）
    /// </summary>
    public long ExecutionTime { get; set; }

    /// <summary>
    /// 重试次数
    /// </summary>
    public int RetryCount { get; set; }

    /// <summary>
    /// 触发方式
    /// </summary>
    public string? TriggerMode { get; set; }

    /// <summary>
    /// 是否包含运行结果
    /// </summary>
    public bool HasRunResult { get; set; }

    /// <summary>
    /// 是否包含异常文本
    /// </summary>
    public bool HasExceptionText { get; set; }

    /// <summary>
    /// 是否包含堆栈
    /// </summary>
    public bool HasStack { get; set; }

    /// <summary>
    /// 是否包含输出轨迹
    /// </summary>
    public bool HasOutputTrace { get; set; }

    /// <summary>
    /// 是否包含扩展数据
    /// </summary>
    public bool HasExtension { get; set; }

    /// <summary>
    /// 是否包含备注
    /// </summary>
    public bool HasNote { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTimeOffset CreatedTime { get; set; }
}
