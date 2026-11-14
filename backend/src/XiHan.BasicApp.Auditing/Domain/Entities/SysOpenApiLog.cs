// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;

namespace XiHan.BasicApp.Auditing.Domain.Entities;

/// <summary>
/// 开放接口日志：签名开放接口的调用与验签结果
/// </summary>
/// <remarks>
/// 签名开放接口即 <c>open.*</c> 操作（<c>/api/open:{动词}</c>）。OpenPlatform 的验签中间件在调用里登记 <c>OpenApiLogEntry</c>，由审计日志中间件按调用结果落库（成败都记）
/// 请求与响应正文只存截断后的摘要，敏感头不落库
/// </remarks>
[SugarTable(TableName = "sys_open_api_log_{year}{month}{day}", TableDescription = "开放接口日志：签名开放接口的调用与验签结果")]
[SplitTable(SplitType.Month)]
[SugarIndex("ix_{split_table}_tenant_id_occurred_at", nameof(TenantId), OrderByType.Asc, nameof(OccurredAt), OrderByType.Asc)]
[SugarIndex("ix_{split_table}_trace_id", nameof(TraceId), OrderByType.Asc)]
[SugarIndex("ix_{split_table}_user_id_occurred_at", nameof(UserId), OrderByType.Asc, nameof(OccurredAt), OrderByType.Asc)]
[SugarIndex("ix_{split_table}_client_id", nameof(ClientId), OrderByType.Asc)]
public sealed class SysOpenApiLog : AuditLogRecord
{
    /// <summary>
    /// 接入应用的客户端标识
    /// </summary>
    [SugarColumn(ColumnName = "client_id", Length = 128, IsNullable = true, ColumnDescription = "接入应用的客户端标识")]
    public string? ClientId { get; set; }

    /// <summary>
    /// 接入应用名称（写入时的快照）
    /// </summary>
    [SugarColumn(ColumnName = "app_name", Length = 256, IsNullable = true, ColumnDescription = "接入应用名称（写入时的快照）")]
    public string? AppName { get; set; }

    /// <summary>
    /// 操作 ID
    /// </summary>
    [SugarColumn(ColumnName = "operation_id", Length = 128, IsNullable = true, ColumnDescription = "操作 ID")]
    public string? OperationId { get; set; }

    /// <summary>
    /// 请求方法
    /// </summary>
    [SugarColumn(ColumnName = "method", Length = 16, IsNullable = false, ColumnDescription = "请求方法")]
    public string Method { get; set; } = string.Empty;

    /// <summary>
    /// 请求路径（不含查询串）
    /// </summary>
    [SugarColumn(ColumnName = "path", Length = 512, IsNullable = false, ColumnDescription = "请求路径（不含查询串）")]
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// 签名算法
    /// </summary>
    [SugarColumn(ColumnName = "signature_algorithm", Length = 32, IsNullable = true, ColumnDescription = "签名算法")]
    public string? SignatureAlgorithm { get; set; }

    /// <summary>
    /// 签名是否有效
    /// </summary>
    [SugarColumn(ColumnName = "signature_valid", IsNullable = false, ColumnDescription = "签名是否有效")]
    public bool SignatureValid { get; set; }

    /// <summary>
    /// HTTP 状态码
    /// </summary>
    [SugarColumn(ColumnName = "status_code", IsNullable = false, ColumnDescription = "HTTP 状态码")]
    public int StatusCode { get; set; }

    /// <summary>
    /// 是否成功
    /// </summary>
    [SugarColumn(ColumnName = "succeeded", IsNullable = false, ColumnDescription = "是否成功")]
    public bool Succeeded { get; set; }

    /// <summary>
    /// 失败的错误码
    /// </summary>
    [SugarColumn(ColumnName = "error_code", Length = 64, IsNullable = true, ColumnDescription = "失败的错误码")]
    public string? ErrorCode { get; set; }

    /// <summary>
    /// 失败的错误消息
    /// </summary>
    [SugarColumn(ColumnName = "error_message", Length = 1024, IsNullable = true, ColumnDescription = "失败的错误消息")]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 耗时（毫秒）
    /// </summary>
    [SugarColumn(ColumnName = "duration_ms", IsNullable = false, ColumnDescription = "耗时（毫秒）")]
    public long DurationMs { get; set; }

    /// <summary>
    /// 请求大小（字节）
    /// </summary>
    [SugarColumn(ColumnName = "request_size", IsNullable = false, ColumnDescription = "请求大小（字节）")]
    public long RequestSize { get; set; }

    /// <summary>
    /// 响应大小（字节）
    /// </summary>
    [SugarColumn(ColumnName = "response_size", IsNullable = false, ColumnDescription = "响应大小（字节）")]
    public long ResponseSize { get; set; }

    /// <summary>
    /// 请求正文摘要（截断）
    /// </summary>
    [SugarColumn(ColumnName = "request_body", Length = 4096, IsNullable = true, ColumnDescription = "请求正文摘要（截断）")]
    public string? RequestBody { get; set; }

    /// <summary>
    /// 响应正文摘要（截断）
    /// </summary>
    [SugarColumn(ColumnName = "response_body", Length = 4096, IsNullable = true, ColumnDescription = "响应正文摘要（截断）")]
    public string? ResponseBody { get; set; }
}
