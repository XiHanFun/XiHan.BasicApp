// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;

namespace XiHan.BasicApp.Auditing.Domain.Entities;

/// <summary>
/// 访问日志：每次操作调用一条（含认证失败与越权的尝试）
/// </summary>
/// <remarks>
/// 每次操作调用记一条，连认证失败与越权的尝试也记：安全审计与排查「为什么没有权限」要的是全部调用，不只成功的；结果、错误码、耗时与客户端信息随行存下，看一条就知道发生了什么，不必回查别的表
/// 量随调用次数线性增长，所以按 <see cref="AuditLogRecord.OccurredAt"/> 按月分表：查询带时间范围只扫涉及的月表，单表不会无限膨胀，过了保留期的行由作业 <c>auditing.retention</c> 成批删掉
/// 落租户库、按产生它的租户写（平台一侧记 <c>platform</c>）：日志随租户分库隔离，租户只看得到自己的调用
/// 写入不进调用的事务：中间件在调用结束时放进有界队列，由后台批量落库；审计存储慢或不可用时宁可丢日志并计数，也不拖慢或拒绝业务调用
/// 只追加、不修改，留作审计证据；<see cref="AuditLogRecord.TraceId"/> 与操作、异常、登录、权限变更、数据变更日志共用，按一次调用串成时间线
/// 索引对应三种读法：租户加时间的列表、trace-id 与会话的链路、用户加时间的行为追溯
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
