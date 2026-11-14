// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Contracts.Entities;

namespace XiHan.BasicApp.Auditing.Domain.Entities;

/// <summary>
/// 按月分表的审计日志基类
/// </summary>
/// <remarks>
/// 租户库实体，只追加。平台一侧的日志（平台主体、匿名调用、找不到租户的登录）租户列记 <c>platform</c>
/// 分表字段 <see cref="OccurredAt"/> 是 UTC 的 <see cref="DateTime"/>（ADR-0023：SqlSugar 按它推表名，写入后不改）；链路追踪按这里的公共列聚合
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
