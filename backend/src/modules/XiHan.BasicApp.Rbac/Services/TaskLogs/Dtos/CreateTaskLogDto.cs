#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:CreateTaskLogDto
// Guid:i2c2d3e4-f5a6-7890-abcd-ef1234567909
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/12/28 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core;
using TaskStatus = XiHan.BasicApp.Rbac.Enums.TaskStatus;

namespace XiHan.BasicApp.Rbac.Services.TaskLogs.Dtos;

/// <summary>
/// 创建任务日志 DTO
/// </summary>
public class CreateTaskLogDto
{
    /// <summary>
    /// 任务ID
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
    /// 服务器名称
    /// </summary>
    public string? ServerName { get; set; }

    /// <summary>
    /// 进程ID
    /// </summary>
    public int? ProcessId { get; set; }

    /// <summary>
    /// 线程ID
    /// </summary>
    public int? ThreadId { get; set; }

    /// <summary>
    /// 任务状态
    /// </summary>
    public TaskStatus TaskStatus { get; set; } = TaskStatus.Running;

    /// <summary>
    /// 执行时长（毫秒）
    /// </summary>
    public long ExecutionTime { get; set; } = 0;

    /// <summary>
    /// 执行结果
    /// </summary>
    public string? ExecutionResult { get; set; }

    /// <summary>
    /// 异常信息
    /// </summary>
    public string? ExceptionMessage { get; set; }

    /// <summary>
    /// 异常堆栈
    /// </summary>
    public string? ExceptionStackTrace { get; set; }

    /// <summary>
    /// 输出日志
    /// </summary>
    public string? OutputLog { get; set; }

    /// <summary>
    /// 内存使用（MB）
    /// </summary>
    public decimal? MemoryUsage { get; set; }

    /// <summary>
    /// CPU使用率（%）
    /// </summary>
    public decimal? CpuUsage { get; set; }

    /// <summary>
    /// 重试次数
    /// </summary>
    public int RetryCount { get; set; } = 0;

    /// <summary>
    /// 触发方式
    /// </summary>
    public string? TriggerMode { get; set; }

    /// <summary>
    /// 扩展数据（JSON格式）
    /// </summary>
    public string? ExtendData { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}
