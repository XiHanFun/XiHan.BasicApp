// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using SqlSugar;
using XiHan.Framework.Persistence.Abstractions;

namespace XiHan.BasicApp.Auditing.Domain.Entities;

/// <summary>
/// 异常日志：调用期间抛出的未处理异常（内核把它转成 <c>KERNEL_UNEXPECTED</c> 结果之前），由 Observe 阶段的中间件写入；作业等非操作调用同样记录。
/// </summary>
[SugarTable("sys_exception_log_{year}{month}{day}")]
[SplitTable(SplitType.Month)]
public sealed class SysExceptionLog : AuditLogRecord
{
    /// <summary>
    /// 操作 ID；不是操作调用（作业、消息派发）为 <see langword="null"/>。
    /// </summary>
    [StringLength(128)]
    public string? OperationId { get; set; }

    /// <summary>
    /// 调用协议。
    /// </summary>
    [StringLength(16)]
    public string? Protocol { get; set; }

    /// <summary>
    /// 请求方法。
    /// </summary>
    [StringLength(16)]
    public string? Method { get; set; }

    /// <summary>
    /// 请求路径。
    /// </summary>
    [StringLength(512)]
    public string? Path { get; set; }

    /// <summary>
    /// 异常类型（最外层的完整类型名）。
    /// </summary>
    [StringLength(256)]
    public string ExceptionType { get; set; } = string.Empty;

    /// <summary>
    /// 异常消息。
    /// </summary>
    [LongText]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 异常堆栈（含内部异常）。
    /// </summary>
    [LongText]
    public string? StackTrace { get; set; }

    /// <summary>
    /// 异常源（程序集）。
    /// </summary>
    [StringLength(256)]
    public string? Source { get; set; }

    /// <summary>
    /// 发生位置（抛出点的类型与方法）。
    /// </summary>
    [StringLength(512)]
    public string? Location { get; set; }

    /// <summary>
    /// 服务器主机名。
    /// </summary>
    [StringLength(128)]
    public string? MachineName { get; set; }

    /// <summary>
    /// 运行环境（Development、Production…）。
    /// </summary>
    [StringLength(64)]
    public string? EnvironmentName { get; set; }

    /// <summary>
    /// 应用版本。
    /// </summary>
    [StringLength(64)]
    public string? AppVersion { get; set; }
}
