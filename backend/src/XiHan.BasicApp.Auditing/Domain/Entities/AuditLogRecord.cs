// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Contracts.Entities;

namespace XiHan.BasicApp.Auditing.Domain.Entities;

/// <summary>
/// 按月分表的审计日志基类
/// </summary>
/// <remarks>
/// 七类日志共用的部分放在基类：<c>AuditLogMiddleware</c> 一处按同一套规则给所有日志填调用方、时间与 trace-id，写入器、保留清理作业与链路查询都按基类泛型处理，新增一类日志不必再写这些
/// 租户库实体，只追加。平台一侧的日志（平台主体、匿名调用、找不到租户的登录）租户列记 <c>platform</c>，独立库的租户落进它自己的库：日志与产生它的业务数据同样隔离，列表、详情与链路都只看当前一侧
/// 量随调用次数增长，所以子类都按月分表；分表字段 <see cref="OccurredAt"/> 是 UTC 的 <see cref="DateTime"/>（ADR-0023：SqlSugar 按它推表名，写入后不改），清理作业按它成批删掉过期的月份数据
/// 用户、登录名、会话与客户端地址做成公共列，链路时间线才能按其中任一维度跨七类日志聚合；登录名是写入时的快照，账号改名或删除后日志仍看得懂
/// 子类都建「租户 + 时间」「trace-id」「用户 + 时间」三个索引，对应列表、链路与行为追溯三种通用读法，各类再按自己独有的读法补索引
/// </remarks>
public abstract class AuditLogRecord : TenantScopedRecord
{
    /// <summary>
    /// 发生时间（UTC）
    /// </summary>
    /// <remarks>
    /// 分表字段
    /// </remarks>
    [SugarColumn(ColumnName = "occurred_at", IsNullable = false, ColumnDescription = "发生时间（UTC）")]
    [SplitField]
    public DateTime OccurredAt { get; set; }

    /// <summary>
    /// 所属调用的 trace-id
    /// </summary>
    [SugarColumn(ColumnName = "trace_id", Length = 64, IsNullable = false, ColumnDescription = "所属调用的 trace-id")]
    public string TraceId { get; set; } = string.Empty;

    /// <summary>
    /// 当时的用户
    /// </summary>
    [SugarColumn(ColumnName = "user_id", IsNullable = true, ColumnDescription = "当时的用户")]
    public Guid? UserId { get; set; }

    /// <summary>
    /// 当时用户的登录名（写入时的快照）
    /// </summary>
    [SugarColumn(ColumnName = "user_name", Length = 256, IsNullable = true, ColumnDescription = "当时用户的登录名（写入时的快照）")]
    public string? UserName { get; set; }

    /// <summary>
    /// 当时的会话
    /// </summary>
    [SugarColumn(ColumnName = "session_id", IsNullable = true, ColumnDescription = "当时的会话")]
    public Guid? SessionId { get; set; }

    /// <summary>
    /// 客户端地址
    /// </summary>
    [SugarColumn(ColumnName = "client_ip", Length = 64, IsNullable = true, ColumnDescription = "客户端地址")]
    public string? ClientIp { get; set; }

    /// <summary>
    /// 客户端的 User-Agent
    /// </summary>
    [SugarColumn(ColumnName = "user_agent", Length = 512, IsNullable = true, ColumnDescription = "客户端的 User-Agent")]
    public string? UserAgent { get; set; }
}
