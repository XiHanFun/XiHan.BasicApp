#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysTaskLog
// Guid:2d28152c-d6e9-4396-addb-b479254bad52
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/8/14 6:35:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Rbac.Entities.Base;
using TaskStatus = XiHan.BasicApp.Rbac.Enums.TaskStatus;

namespace XiHan.BasicApp.Rbac.Entities;

/// <summary>
/// 系统任务日志实体
/// </summary>
[SugarTable("Sys_Task_Log_{year}{month}{day}", "系统任务日志表")]
[SplitTable(SplitType.Month)]
[SugarIndex("IX_SysTaskLog_TaskId", nameof(TaskId), OrderByType.Asc)]
[SugarIndex("IX_SysTaskLog_TaskStatus", nameof(TaskStatus), OrderByType.Asc)]
[SugarIndex("IX_SysTaskLog_StartTime", nameof(StartTime), OrderByType.Desc)]
public partial class SysTaskLog : RbacFullAuditedEntity<long>
{
    /// <summary>
    /// 任务ID
    /// </summary>
    [SugarColumn(ColumnDescription = "任务ID", IsNullable = false)]
    public virtual long TaskId { get; set; }

    /// <summary>
    /// 任务编码
    /// </summary>
    [SugarColumn(ColumnDescription = "任务编码", Length = 100, IsNullable = false)]
    public virtual string TaskCode { get; set; } = string.Empty;

    /// <summary>
    /// 任务名称
    /// </summary>
    [SugarColumn(ColumnDescription = "任务名称", Length = 200, IsNullable = false)]
    public virtual string TaskName { get; set; } = string.Empty;

    /// <summary>
    /// 执行批次号
    /// </summary>
    [SugarColumn(ColumnDescription = "执行批次号", Length = 50, IsNullable = true)]
    public virtual string? BatchNumber { get; set; }

    /// <summary>
    /// 服务器名称
    /// </summary>
    [SugarColumn(ColumnDescription = "服务器名称", Length = 100, IsNullable = true)]
    public virtual string? ServerName { get; set; }

    /// <summary>
    /// 进程ID
    /// </summary>
    [SugarColumn(ColumnDescription = "进程ID", IsNullable = true)]
    public virtual int? ProcessId { get; set; }

    /// <summary>
    /// 线程ID
    /// </summary>
    [SugarColumn(ColumnDescription = "线程ID", IsNullable = true)]
    public virtual int? ThreadId { get; set; }

    /// <summary>
    /// 任务状态
    /// </summary>
    [SugarColumn(ColumnDescription = "任务状态")]
    public virtual TaskStatus TaskStatus { get; set; } = TaskStatus.Running;

    /// <summary>
    /// 开始时间
    /// </summary>
    [SugarColumn(ColumnDescription = "开始时间")]
    public virtual DateTimeOffset StartTime { get; set; } = DateTimeOffset.Now;

    /// <summary>
    /// 结束时间
    /// </summary>
    [SugarColumn(ColumnDescription = "结束时间", IsNullable = true)]
    public virtual DateTimeOffset? EndTime { get; set; }

    /// <summary>
    /// 执行时长（毫秒）
    /// </summary>
    [SugarColumn(ColumnDescription = "执行时长（毫秒）")]
    public virtual long ExecutionTime { get; set; } = 0;

    /// <summary>
    /// 执行结果
    /// </summary>
    [SugarColumn(ColumnDescription = "执行结果", ColumnDataType = StaticConfig.CodeFirst_BigString, IsNullable = true)]
    public virtual string? ExecutionResult { get; set; }

    /// <summary>
    /// 异常信息
    /// </summary>
    [SugarColumn(ColumnDescription = "异常信息", ColumnDataType = StaticConfig.CodeFirst_BigString, IsNullable = true)]
    public virtual string? ExceptionMessage { get; set; }

    /// <summary>
    /// 异常堆栈
    /// </summary>
    [SugarColumn(ColumnDescription = "异常堆栈", ColumnDataType = StaticConfig.CodeFirst_BigString, IsNullable = true)]
    public virtual string? ExceptionStackTrace { get; set; }

    /// <summary>
    /// 输出日志
    /// </summary>
    [SugarColumn(ColumnDescription = "输出日志", ColumnDataType = StaticConfig.CodeFirst_BigString, IsNullable = true)]
    public virtual string? OutputLog { get; set; }

    /// <summary>
    /// 内存使用（MB）
    /// </summary>
    [SugarColumn(ColumnDescription = "内存使用（MB）", IsNullable = true)]
    public virtual decimal? MemoryUsage { get; set; }

    /// <summary>
    /// CPU使用率（%）
    /// </summary>
    [SugarColumn(ColumnDescription = "CPU使用率（%）", IsNullable = true)]
    public virtual decimal? CpuUsage { get; set; }

    /// <summary>
    /// 重试次数
    /// </summary>
    [SugarColumn(ColumnDescription = "重试次数")]
    public virtual int RetryCount { get; set; } = 0;

    /// <summary>
    /// 触发方式
    /// </summary>
    [SugarColumn(ColumnDescription = "触发方式", Length = 50, IsNullable = true)]
    public virtual string? TriggerMode { get; set; }

    /// <summary>
    /// 扩展数据（JSON格式）
    /// </summary>
    [SugarColumn(ColumnDescription = "扩展数据", ColumnDataType = StaticConfig.CodeFirst_BigString, IsNullable = true)]
    public virtual string? ExtendData { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [SugarColumn(ColumnDescription = "备注", Length = 500, IsNullable = true)]
    public virtual string? Remark { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    [SugarColumn(IsNullable = false, ColumnDescription = "创建时间")]
    [SplitField]
    public override DateTimeOffset CreatedTime { get; set; }
}
