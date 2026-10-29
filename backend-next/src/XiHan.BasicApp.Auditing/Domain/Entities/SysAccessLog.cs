// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using SqlSugar;

namespace XiHan.BasicApp.Auditing.Domain.Entities;

/// <summary>
/// 访问日志：每次操作调用一条（含认证失败与越权的尝试）。
/// </summary>
/// <remarks>
/// 由 Observe 阶段的中间件在调用结束时写入。
/// </remarks>
[SugarTable("sys_access_log_{year}{month}{day}")]
[SplitTable(SplitType.Month)]
public sealed class SysAccessLog : AuditLogRecord
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    [StringLength(128)]
    public string OperationId { get; set; } = string.Empty;

    /// <summary>
    /// 调用协议（http、grpc、mcp…）。
    /// </summary>
    [StringLength(16)]
    public string? Protocol { get; set; }

    /// <summary>
    /// 请求方法（HTTP 方法）。
    /// </summary>
    [StringLength(16)]
    public string? Method { get; set; }

    /// <summary>
    /// 请求路径（不含查询串）。
    /// </summary>
    [StringLength(512)]
    public string? Path { get; set; }

    /// <summary>
    /// 访问结果。
    /// </summary>
    public AccessResult Result { get; set; }

    /// <summary>
    /// HTTP 状态码。
    /// </summary>
    /// <remarks>
    /// 不是 HTTP 调用为 <see langword="null"/>。
    /// </remarks>
    public int? StatusCode { get; set; }

    /// <summary>
    /// 失败的错误码。
    /// </summary>
    [StringLength(64)]
    public string? ErrorCode { get; set; }

    /// <summary>
    /// 失败的错误消息（按调用方语言渲染）。
    /// </summary>
    [StringLength(1024)]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 耗时（毫秒）。
    /// </summary>
    public long DurationMs { get; set; }

    /// <summary>
    /// 浏览器（由 User-Agent 解析）。
    /// </summary>
    [StringLength(64)]
    public string? Browser { get; set; }

    /// <summary>
    /// 操作系统（由 User-Agent 解析）。
    /// </summary>
    [StringLength(64)]
    public string? Os { get; set; }

    /// <summary>
    /// 设备类型（由 User-Agent 解析）。
    /// </summary>
    [StringLength(32)]
    public string? Device { get; set; }

    /// <summary>
    /// 请求来源（Referer）。
    /// </summary>
    [StringLength(1024)]
    public string? Referer { get; set; }
}
