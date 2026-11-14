// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;

namespace XiHan.BasicApp.Auditing.Domain.Entities;

/// <summary>
/// 访问日志：每次操作调用一条（含认证失败与越权的尝试）
/// </summary>
/// <remarks>
/// 由 Observe 阶段的中间件在调用结束时写入
/// </remarks>
[SugarTable(TableName = "sys_access_log_{year}{month}{day}", TableDescription = "访问日志：每次操作调用一条（含认证失败与越权的尝试）")]
[SplitTable(SplitType.Month)]
[SugarIndex("ix_{split_table}_tenant_id_occurred_at", nameof(TenantId), OrderByType.Asc, nameof(OccurredAt), OrderByType.Asc)]
[SugarIndex("ix_{split_table}_trace_id", nameof(TraceId), OrderByType.Asc)]
[SugarIndex("ix_{split_table}_user_id_occurred_at", nameof(UserId), OrderByType.Asc, nameof(OccurredAt), OrderByType.Asc)]
[SugarIndex("ix_{split_table}_session_id", nameof(SessionId), OrderByType.Asc)]
public sealed class SysAccessLog : AuditLogRecord
{
    /// <summary>
    /// 操作 ID
    /// </summary>
    [SugarColumn(ColumnName = "operation_id", Length = 128, IsNullable = false, ColumnDescription = "操作 ID")]
    public string OperationId { get; set; } = string.Empty;

    /// <summary>
    /// 调用协议（http、grpc、mcp…）
    /// </summary>
    [SugarColumn(ColumnName = "protocol", Length = 16, IsNullable = true, ColumnDescription = "调用协议（http、grpc、mcp…）")]
    public string? Protocol { get; set; }

    /// <summary>
    /// 请求方法（HTTP 方法）
    /// </summary>
    [SugarColumn(ColumnName = "method", Length = 16, IsNullable = true, ColumnDescription = "请求方法（HTTP 方法）")]
    public string? Method { get; set; }

    /// <summary>
    /// 请求路径（不含查询串）
    /// </summary>
    [SugarColumn(ColumnName = "path", Length = 512, IsNullable = true, ColumnDescription = "请求路径（不含查询串）")]
    public string? Path { get; set; }

    /// <summary>
    /// 访问结果
    /// </summary>
    [SugarColumn(ColumnName = "result", IsNullable = false, ColumnDescription = "访问结果")]
    public AccessResult Result { get; set; }

    /// <summary>
    /// HTTP 状态码
    /// </summary>
    /// <remarks>
    /// 不是 HTTP 调用为 <see langword="null"/>
    /// </remarks>
    [SugarColumn(ColumnName = "status_code", IsNullable = true, ColumnDescription = "HTTP 状态码")]
    public int? StatusCode { get; set; }

    /// <summary>
    /// 失败的错误码
    /// </summary>
    [SugarColumn(ColumnName = "error_code", Length = 64, IsNullable = true, ColumnDescription = "失败的错误码")]
    public string? ErrorCode { get; set; }

    /// <summary>
    /// 失败的错误消息（按调用方语言渲染）
    /// </summary>
    [SugarColumn(ColumnName = "error_message", Length = 1024, IsNullable = true, ColumnDescription = "失败的错误消息（按调用方语言渲染）")]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 耗时（毫秒）
    /// </summary>
    [SugarColumn(ColumnName = "duration_ms", IsNullable = false, ColumnDescription = "耗时（毫秒）")]
    public long DurationMs { get; set; }

    /// <summary>
    /// 浏览器（由 User-Agent 解析）
    /// </summary>
    [SugarColumn(ColumnName = "browser", Length = 64, IsNullable = true, ColumnDescription = "浏览器（由 User-Agent 解析）")]
    public string? Browser { get; set; }

    /// <summary>
    /// 操作系统（由 User-Agent 解析）
    /// </summary>
    [SugarColumn(ColumnName = "os", Length = 64, IsNullable = true, ColumnDescription = "操作系统（由 User-Agent 解析）")]
    public string? Os { get; set; }

    /// <summary>
    /// 设备类型（由 User-Agent 解析）
    /// </summary>
    [SugarColumn(ColumnName = "device", Length = 32, IsNullable = true, ColumnDescription = "设备类型（由 User-Agent 解析）")]
    public string? Device { get; set; }

    /// <summary>
    /// 请求来源（Referer）
    /// </summary>
    [SugarColumn(ColumnName = "referer", Length = 1024, IsNullable = true, ColumnDescription = "请求来源（Referer）")]
    public string? Referer { get; set; }
}

/// <summary>
/// 访问结果：由调用结果的错误类别归并
/// </summary>
public enum AccessResult
{
    /// <summary>
    /// 成功
    /// </summary>
    Success = 0,

    /// <summary>
    /// 业务失败（校验、冲突、超时、取消等）
    /// </summary>
    Failed = 1,

    /// <summary>
    /// 越权
    /// </summary>
    Forbidden = 2,

    /// <summary>
    /// 未认证或凭据无效
    /// </summary>
    Unauthorized = 3,

    /// <summary>
    /// 资源不存在
    /// </summary>
    NotFound = 4,

    /// <summary>
    /// 服务端故障（未处理异常、依赖不可用）
    /// </summary>
    ServerError = 5,
}
