#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysTask
// Guid:1d28152c-d6e9-4396-addb-b479254bad51
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/8/14 6:30:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Rbac.Entities.Base;
using XiHan.BasicApp.Rbac.Enums;
using XiHan.Framework.Domain.Aggregates;
using TaskStatus = XiHan.BasicApp.Rbac.Enums.TaskStatus;

namespace XiHan.BasicApp.Rbac.Entities;

/// <summary>
/// 系统任务实体
/// </summary>
[SugarTable("Sys_Task", "系统任务表")]
[SugarIndex("IX_SysTask_TaskCode", nameof(TaskCode), OrderByType.Asc, true)]
[SugarIndex("IX_SysTask_TaskStatus", nameof(TaskStatus), OrderByType.Asc)]
[SugarIndex("IX_SysTask_TriggerType", nameof(TriggerType), OrderByType.Asc)]
[SugarIndex("IX_SysTask_NextRunTime", nameof(NextRunTime), OrderByType.Asc)]
[SugarIndex("IX_SysTask_TenantId", nameof(TenantId), OrderByType.Asc)]
public partial class SysTask : AggregateRootBase<long>
{
    /// <summary>
    /// 租户ID
    /// </summary>
    [SugarColumn(ColumnDescription = "租户ID", IsNullable = true)]
    public virtual long? TenantId { get; set; }

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
    /// 任务描述
    /// </summary>
    [SugarColumn(ColumnDescription = "任务描述", Length = 500, IsNullable = true)]
    public virtual string? TaskDescription { get; set; }

    /// <summary>
    /// 任务分组
    /// </summary>
    [SugarColumn(ColumnDescription = "任务分组", Length = 50, IsNullable = true)]
    public virtual string? TaskGroup { get; set; }

    /// <summary>
    /// 任务类名
    /// </summary>
    [SugarColumn(ColumnDescription = "任务类名", Length = 200, IsNullable = false)]
    public virtual string TaskClass { get; set; } = string.Empty;

    /// <summary>
    /// 任务方法
    /// </summary>
    [SugarColumn(ColumnDescription = "任务方法", Length = 100, IsNullable = true)]
    public virtual string? TaskMethod { get; set; }

    /// <summary>
    /// 任务参数（JSON格式）
    /// </summary>
    [SugarColumn(ColumnDescription = "任务参数", ColumnDataType = StaticConfig.CodeFirst_BigString, IsNullable = true)]
    public virtual string? TaskParams { get; set; }

    /// <summary>
    /// 触发类型
    /// </summary>
    [SugarColumn(ColumnDescription = "触发类型")]
    public virtual TriggerType TriggerType { get; set; } = TriggerType.Immediate;

    /// <summary>
    /// Cron表达式
    /// </summary>
    [SugarColumn(ColumnDescription = "Cron表达式", Length = 100, IsNullable = true)]
    public virtual string? CronExpression { get; set; }

    /// <summary>
    /// 开始时间
    /// </summary>
    [SugarColumn(ColumnDescription = "开始时间", IsNullable = true)]
    public virtual DateTimeOffset? StartTime { get; set; }

    /// <summary>
    /// 结束时间
    /// </summary>
    [SugarColumn(ColumnDescription = "结束时间", IsNullable = true)]
    public virtual DateTimeOffset? EndTime { get; set; }

    /// <summary>
    /// 下次执行时间
    /// </summary>
    [SugarColumn(ColumnDescription = "下次执行时间", IsNullable = true)]
    public virtual DateTimeOffset? NextRunTime { get; set; }

    /// <summary>
    /// 上次执行时间
    /// </summary>
    [SugarColumn(ColumnDescription = "上次执行时间", IsNullable = true)]
    public virtual DateTimeOffset? LastRunTime { get; set; }

    /// <summary>
    /// 执行间隔（秒）
    /// </summary>
    [SugarColumn(ColumnDescription = "执行间隔（秒）", IsNullable = true)]
    public virtual int? IntervalSeconds { get; set; }

    /// <summary>
    /// 重复次数（-1表示无限）
    /// </summary>
    [SugarColumn(ColumnDescription = "重复次数")]
    public virtual int RepeatCount { get; set; } = -1;

    /// <summary>
    /// 已执行次数
    /// </summary>
    [SugarColumn(ColumnDescription = "已执行次数")]
    public virtual int ExecutedCount { get; set; } = 0;

    /// <summary>
    /// 超时时间（秒）
    /// </summary>
    [SugarColumn(ColumnDescription = "超时时间（秒）")]
    public virtual int TimeoutSeconds { get; set; } = 300;

    /// <summary>
    /// 任务状态
    /// </summary>
    [SugarColumn(ColumnDescription = "任务状态")]
    public virtual TaskStatus TaskStatus { get; set; } = TaskStatus.Pending;

    /// <summary>
    /// 优先级（1-5，数字越小优先级越高）
    /// </summary>
    [SugarColumn(ColumnDescription = "优先级")]
    public virtual int Priority { get; set; } = 3;

    /// <summary>
    /// 是否允许并发执行
    /// </summary>
    [SugarColumn(ColumnDescription = "是否允许并发执行")]
    public virtual bool AllowConcurrent { get; set; } = false;

    /// <summary>
    /// 失败重试次数
    /// </summary>
    [SugarColumn(ColumnDescription = "失败重试次数")]
    public virtual int RetryCount { get; set; } = 0;

    /// <summary>
    /// 最大重试次数
    /// </summary>
    [SugarColumn(ColumnDescription = "最大重试次数")]
    public virtual int MaxRetryCount { get; set; } = 3;

    /// <summary>
    /// 状态
    /// </summary>
    [SugarColumn(ColumnDescription = "状态")]
    public virtual YesOrNo Status { get; set; } = YesOrNo.Yes;

    /// <summary>
    /// 备注
    /// </summary>
    [SugarColumn(ColumnDescription = "备注", Length = 500, IsNullable = true)]
    public virtual string? Remark { get; set; }
}
