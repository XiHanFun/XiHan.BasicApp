// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using SqlSugar;

namespace XiHan.BasicApp.Auditing.Domain.Entities;

/// <summary>
/// 开放接口日志：签名开放接口的调用与验签结果。
/// </summary>
/// <remarks>
/// 签名开放接口即 <c>open.*</c> 操作（<c>/api/open:{动词}</c>）。OpenPlatform 的验签中间件在调用里登记 <c>OpenApiLogEntry</c>，由审计日志中间件按调用结果落库（成败都记）。
/// 请求与响应正文只存截断后的摘要，敏感头不落库。
/// </remarks>
[SugarTable("sys_open_api_log_{year}{month}{day}")]
[SplitTable(SplitType.Month)]
public sealed class SysOpenApiLog : AuditLogRecord
{
    /// <summary>
    /// 接入应用的客户端标识。
    /// </summary>
    [StringLength(128)]
    public string? ClientId { get; set; }

    /// <summary>
    /// 接入应用名称（写入时的快照）。
    /// </summary>
    [StringLength(256)]
    public string? AppName { get; set; }

    /// <summary>
    /// 操作 ID。
    /// </summary>
    [StringLength(128)]
    public string? OperationId { get; set; }

    /// <summary>
    /// 请求方法。
    /// </summary>
    [StringLength(16)]
    public string Method { get; set; } = string.Empty;

    /// <summary>
    /// 请求路径（不含查询串）。
    /// </summary>
    [StringLength(512)]
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// 签名算法。
    /// </summary>
    [StringLength(32)]
    public string? SignatureAlgorithm { get; set; }

    /// <summary>
    /// 签名是否有效。
    /// </summary>
    public bool SignatureValid { get; set; }

    /// <summary>
    /// HTTP 状态码。
    /// </summary>
    public int StatusCode { get; set; }

    /// <summary>
    /// 是否成功。
    /// </summary>
    public bool Succeeded { get; set; }

    /// <summary>
    /// 失败的错误码。
    /// </summary>
    [StringLength(64)]
    public string? ErrorCode { get; set; }

    /// <summary>
    /// 失败的错误消息。
    /// </summary>
    [StringLength(1024)]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 耗时（毫秒）。
    /// </summary>
    public long DurationMs { get; set; }

    /// <summary>
    /// 请求大小（字节）。
    /// </summary>
    public long RequestSize { get; set; }

    /// <summary>
    /// 响应大小（字节）。
    /// </summary>
    public long ResponseSize { get; set; }

    /// <summary>
    /// 请求正文摘要（截断）。
    /// </summary>
    [StringLength(4096)]
    public string? RequestBody { get; set; }

    /// <summary>
    /// 响应正文摘要（截断）。
    /// </summary>
    [StringLength(4096)]
    public string? ResponseBody { get; set; }
}
