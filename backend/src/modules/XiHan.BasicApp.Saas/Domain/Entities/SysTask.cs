#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysTask
// Guid:1d28152c-d6e9-4396-addb-b479254bad51
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/08/14 06:30:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Core.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 系统任务实体
/// 定时/定期任务定义聚合根：承载任务元信息、触发规则、下次运行时间；执行历史在 SysTaskLog
/// </summary>
/// <remarks>
/// 职责边界：
/// - 本表仅存"任务配置"；每次执行的细节（结果/耗时/日志）由 SysTaskLog 按月分表承载
///
/// 关联：
/// - 反向：SysTaskLog.TaskId
///
/// 写入：
/// - TaskCode 全局唯一（UX_TaCo）
/// - TriggerType=Cron 时 CronExpression 必填；TriggerType=Interval 时 IntervalSeconds 必填
/// - NextRunTime 由调度器根据 Trigger 计算并更新，不应由用户直接修改
///
/// 查询：
/// - 调度器拉取待执行队列：IX_NeRuTi，筛选已到执行时间的 NextRunTime
/// - 运行状态概览：IX_RuTaSt
/// - 按触发类型统计：IX_TrTy
///
/// 删除：
/// - 仅软删；停用任务通过 Status 或 RunTaskStatus=Stopped 更安全
///
/// 状态：
/// - Status: Yes/No 启停（No 时即使到点也不执行）
/// - RunTaskStatus: Pending/Running/Success/Failed/Stopped/Paused
///
/// 场景：
/// - 定时数据清理、报表生成、订阅续费检查
/// - 支持分布式锁保证多实例环境下仅单实例执行
/// </remarks>
[SugarTable(TableName = "Sys_Task", TableDescription = "系统任务表")]
[SugarIndex("IX_{table}_TeId_CrTi", nameof(TenantId), OrderByType.Asc, nameof(CreatedTime), OrderByType.Desc)]
[SugarIndex("IX_{table}_CrId", nameof(CreatedId), OrderByType.Asc)]
[SugarIndex("IX_{table}_TeId_IsDe", nameof(TenantId), OrderByType.Asc, nameof(IsDeleted), OrderByType.Asc)]
[SugarIndex("UX_{table}_TeId_TaCo", nameof(TenantId), OrderByType.Asc, nameof(TaskCode), OrderByType.Asc, nameof(IsDeleted), OrderByType.Asc, true)]
[SugarIndex("IX_{table}_RuTaSt", nameof(RunTaskStatus), OrderByType.Asc)]
[SugarIndex("IX_{table}_TrTy", nameof(TriggerType), OrderByType.Asc)]
[SugarIndex("IX_{table}_NeRuTi", nameof(NextRunTime), OrderByType.Desc)]
[SugarIndex("IX_{table}_TeId_St", nameof(TenantId), OrderByType.Asc, nameof(Status), OrderByType.Asc)]
public partial class SysTask : BasicAppAggregateRoot
{
    /// <summary>
    /// 任务编码
    /// </summary>
    [SugarColumn(ColumnName = "Task_Code", ColumnDescription = "任务编码", Length = 100, IsNullable = false)]
    public virtual string TaskCode { get; set; } = string.Empty;

    /// <summary>
    /// 任务名称
    /// </summary>
    [SugarColumn(ColumnName = "Task_Name", ColumnDescription = "任务名称", Length = 200, IsNullable = false)]
    public virtual string TaskName { get; set; } = string.Empty;

    /// <summary>
    /// 任务描述
    /// </summary>
    [SugarColumn(ColumnName = "Task_Description", ColumnDescription = "任务描述", Length = 500, IsNullable = true)]
    public virtual string? TaskDescription { get; set; }

    /// <summary>
    /// 任务分组
    /// </summary>
    [SugarColumn(ColumnName = "Task_Group", ColumnDescription = "任务分组", Length = 50, IsNullable = true)]
    public virtual string? TaskGroup { get; set; }

    /// <summary>
    /// 任务类名
    /// </summary>
    [SugarColumn(ColumnName = "Task_Class", ColumnDescription = "任务类名", Length = 200, IsNullable = false)]
    public virtual string TaskClass { get; set; } = string.Empty;

    /// <summary>
    /// 任务方法
    /// </summary>
    [SugarColumn(ColumnName = "Task_Method", ColumnDescription = "任务方法", Length = 100, IsNullable = true)]
    public virtual string? TaskMethod { get; set; }

    /// <summary>
    /// 任务参数（JSON格式）
    /// </summary>
    [SugarColumn(ColumnName = "Task_Params", ColumnDescription = "任务参数", ColumnDataType = StaticConfig.CodeFirst_BigString, IsNullable = true)]
    public virtual string? TaskParams { get; set; }

    /// <summary>
    /// 触发类型
    /// </summary>
    [SugarColumn(ColumnName = "Trigger_Type", ColumnDescription = "触发类型")]
    public virtual TriggerType TriggerType { get; set; } = TriggerType.Immediate;

    /// <summary>
    /// Cron表达式
    /// </summary>
    [SugarColumn(ColumnName = "Cron_Expression", ColumnDescription = "Cron表达式", Length = 100, IsNullable = true)]
    public virtual string? CronExpression { get; set; }

    /// <summary>
    /// 开始时间
    /// </summary>
    [SugarColumn(ColumnName = "Start_Time", ColumnDescription = "开始时间", IsNullable = true)]
    public virtual DateTimeOffset? StartTime { get; set; }

    /// <summary>
    /// 结束时间
    /// </summary>
    [SugarColumn(ColumnName = "End_Time", ColumnDescription = "结束时间", IsNullable = true)]
    public virtual DateTimeOffset? EndTime { get; set; }

    /// <summary>
    /// 下次执行时间
    /// </summary>
    [SugarColumn(ColumnName = "Next_Run_Time", ColumnDescription = "下次执行时间", IsNullable = true)]
    public virtual DateTimeOffset? NextRunTime { get; set; }

    /// <summary>
    /// 上次执行时间
    /// </summary>
    [SugarColumn(ColumnName = "Last_Run_Time", ColumnDescription = "上次执行时间", IsNullable = true)]
    public virtual DateTimeOffset? LastRunTime { get; set; }

    /// <summary>
    /// 执行间隔（秒）
    /// </summary>
    [SugarColumn(ColumnName = "Interval_Seconds", ColumnDescription = "执行间隔（秒）", IsNullable = true)]
    public virtual int? IntervalSeconds { get; set; }

    /// <summary>
    /// 重复次数（-1表示无限）
    /// </summary>
    [SugarColumn(ColumnName = "Repeat_Count", ColumnDescription = "重复次数")]
    public virtual int RepeatCount { get; set; } = -1;

    /// <summary>
    /// 已执行次数
    /// </summary>
    [SugarColumn(ColumnName = "Executed_Count", ColumnDescription = "已执行次数")]
    public virtual int ExecutedCount { get; set; } = 0;

    /// <summary>
    /// 超时时间（秒）
    /// </summary>
    [SugarColumn(ColumnName = "Timeout_Seconds", ColumnDescription = "超时时间（秒）")]
    public virtual int TimeoutSeconds { get; set; } = 300;

    /// <summary>
    /// 任务状态
    /// </summary>
    [SugarColumn(ColumnName = "Run_Task_Status", ColumnDescription = "任务状态")]
    public virtual RunTaskStatus RunTaskStatus { get; set; } = RunTaskStatus.Pending;

    /// <summary>
    /// 优先级（数字越大优先级越高，与全系统 Priority 方向一致）
    /// </summary>
    [SugarColumn(ColumnName = "Priority", ColumnDescription = "优先级")]
    public virtual int Priority { get; set; } = 0;

    /// <summary>
    /// 是否允许并发执行
    /// </summary>
    [SugarColumn(ColumnName = "Allow_Concurrent", ColumnDescription = "是否允许并发执行")]
    public virtual bool AllowConcurrent { get; set; } = false;

    /// <summary>
    /// 失败重试次数
    /// </summary>
    [SugarColumn(ColumnName = "Retry_Count", ColumnDescription = "失败重试次数")]
    public virtual int RetryCount { get; set; } = 0;

    /// <summary>
    /// 最大重试次数
    /// </summary>
    [SugarColumn(ColumnName = "Max_Retry_Count", ColumnDescription = "最大重试次数")]
    public virtual int MaxRetryCount { get; set; } = 3;

    /// <summary>
    /// 状态
    /// </summary>
    [SugarColumn(ColumnName = "Status", ColumnDescription = "状态")]
    public virtual EnableStatus Status { get; set; } = EnableStatus.Enabled;

    /// <summary>
    /// 备注
    /// </summary>
    [SugarColumn(ColumnName = "Remark", ColumnDescription = "备注", Length = 500, IsNullable = true)]
    public virtual string? Remark { get; set; }
}
