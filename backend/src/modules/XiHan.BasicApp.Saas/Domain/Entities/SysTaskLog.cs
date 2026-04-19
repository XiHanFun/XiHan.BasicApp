#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysTaskLog
// Guid:2d28152c-d6e9-4396-addb-b479254bad52
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/08/14 06:35:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Core.Entities;

using XiHan.Framework.Domain.Entities.Abstracts;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 系统任务日志实体
/// SysTask 每次执行的运行记录（开始/结束/状态/结果/日志摘要/批次号）
/// </summary>
/// <remarks>
/// 分表策略：
/// - 按月分表；查询/清理必带时间范围
///
/// 关联：
/// - TaskId → SysTask；TaskCode 冗余便于快速过滤
///
/// 写入：
/// - 由调度框架在每次执行时写入一条：开始前预写 Running，完成后更新 Success/Failed
/// - 失败时记录 ErrorMessage / Stacktrace（截断控制大小）
/// - BatchNumber 用于批次任务分组（同次调度多子任务共享 BatchNumber）
///
/// 查询：
/// - 任务最近执行：IX_TeId_TaId + ORDER BY StartTime DESC
/// - 租户任务概览：IX_TeId_StTi
/// - 失败任务扫描：IX_TaSt + WHERE TaskStatus=Failed
/// - 按批次查询：IX_BaNu
///
/// 删除：
/// - 不支持业务删除；按保留策略归档
///
/// 场景：
/// - 任务管理后台展示执行历史
/// - 失败重试决策依据
/// - 按任务码统计成功率/平均耗时
/// </remarks>
[SugarTable("SysTaskLog_{year}{month}{day}", "系统任务日志表"), SplitTable(SplitType.Month)]
[SugarIndex("IX_{split_table}_TeId_CrTi", nameof(TenantId), OrderByType.Asc, nameof(CreatedTime), OrderByType.Desc)]
[SugarIndex("IX_{split_table}_CrId", nameof(CreatedId), OrderByType.Asc)]
[SugarIndex("IX_{split_table}_TaSt", nameof(TaskStatus), OrderByType.Asc)]
[SugarIndex("IX_{split_table}_TaCo", nameof(TaskCode), OrderByType.Asc)]
[SugarIndex("IX_{split_table}_BaNu", nameof(BatchNumber), OrderByType.Asc)]
[SugarIndex("IX_{split_table}_TeId_TaId", nameof(TenantId), OrderByType.Asc, nameof(TaskId), OrderByType.Asc)]
[SugarIndex("IX_{split_table}_TeId_StTi", nameof(TenantId), OrderByType.Asc, nameof(StartTime), OrderByType.Desc)]
public partial class SysTaskLog : BasicAppCreationEntity, ISplitTableEntity
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
    public virtual DateTimeOffset StartTime { get; set; } = DateTimeOffset.UtcNow;

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
