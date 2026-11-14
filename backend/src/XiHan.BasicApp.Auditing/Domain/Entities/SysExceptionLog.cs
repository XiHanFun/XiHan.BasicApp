// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.Framework.Persistence.Abstractions;

namespace XiHan.BasicApp.Auditing.Domain.Entities;

/// <summary>
/// 异常日志：调用期间抛出的未处理异常
/// </summary>
/// <remarks>
/// 未处理异常单独成表，存完整堆栈、抛出点、主机、运行环境与应用版本：访问日志只记得结果是服务端故障，排查要看到异常本身；堆栈很大，并进访问日志会让每一行都变大
/// 在 Observe 阶段、内核把异常转成 <c>KERNEL_UNEXPECTED</c> 结果之前截下：转换之后只剩通用错误，异常的类型与堆栈就丢了
/// 取消不记，取消不是故障；作业等非操作调用同样记录，后台任务的故障也要查得到
/// 不受访问日志的开关与排除名单影响：可以为降噪关掉访问日志，故障不能跟着漏掉
/// trace-id 与访问日志共用，从失败的那次访问直接跳到对应的异常；分表、异步批量写入与只追加同其他分表日志（见 <see cref="AuditLogRecord"/>）
/// </remarks>
[SugarTable(TableName = "sys_exception_log_{year}{month}{day}", TableDescription = "异常日志：调用期间抛出的未处理异常")]
[SplitTable(SplitType.Month)]
[SugarIndex("ix_{split_table}_tenant_id_occurred_at", nameof(TenantId), OrderByType.Asc, nameof(OccurredAt), OrderByType.Asc)]
[SugarIndex("ix_{split_table}_trace_id", nameof(TraceId), OrderByType.Asc)]
[SugarIndex("ix_{split_table}_user_id_occurred_at", nameof(UserId), OrderByType.Asc, nameof(OccurredAt), OrderByType.Asc)]
public sealed class SysExceptionLog : AuditLogRecord
{
    /// <summary>
    /// 操作 ID
    /// </summary>
    /// <remarks>
    /// 不是操作调用（作业、消息派发）为 <see langword="null"/>
    /// </remarks>
    [SugarColumn(ColumnName = "operation_id", Length = 128, IsNullable = true, ColumnDescription = "操作 ID")]
    public string? OperationId { get; set; }

    /// <summary>
    /// 调用协议
    /// </summary>
    [SugarColumn(ColumnName = "protocol", Length = 16, IsNullable = true, ColumnDescription = "调用协议")]
    public string? Protocol { get; set; }

    /// <summary>
    /// 请求方法
    /// </summary>
    [SugarColumn(ColumnName = "method", Length = 16, IsNullable = true, ColumnDescription = "请求方法")]
    public string? Method { get; set; }

    /// <summary>
    /// 请求路径
    /// </summary>
    [SugarColumn(ColumnName = "path", Length = 512, IsNullable = true, ColumnDescription = "请求路径")]
    public string? Path { get; set; }

    /// <summary>
    /// 异常类型（最外层的完整类型名）
    /// </summary>
    [SugarColumn(ColumnName = "exception_type", Length = 256, IsNullable = false, ColumnDescription = "异常类型（最外层的完整类型名）")]
    public string ExceptionType { get; set; } = string.Empty;

    /// <summary>
    /// 异常消息
    /// </summary>
    [SugarColumn(ColumnName = "message", IsNullable = false, ColumnDescription = "异常消息")]
    [LongText]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 异常堆栈（含内部异常）
    /// </summary>
    [SugarColumn(ColumnName = "stack_trace", IsNullable = true, ColumnDescription = "异常堆栈（含内部异常）")]
    [LongText]
    public string? StackTrace { get; set; }

    /// <summary>
    /// 异常源（程序集）
    /// </summary>
    [SugarColumn(ColumnName = "source", Length = 256, IsNullable = true, ColumnDescription = "异常源（程序集）")]
    public string? Source { get; set; }

    /// <summary>
    /// 发生位置（抛出点的类型与方法）
    /// </summary>
    [SugarColumn(ColumnName = "location", Length = 512, IsNullable = true, ColumnDescription = "发生位置（抛出点的类型与方法）")]
    public string? Location { get; set; }

    /// <summary>
    /// 服务器主机名
    /// </summary>
    [SugarColumn(ColumnName = "machine_name", Length = 128, IsNullable = true, ColumnDescription = "服务器主机名")]
    public string? MachineName { get; set; }

    /// <summary>
    /// 运行环境（Development、Production…）
    /// </summary>
    [SugarColumn(ColumnName = "environment_name", Length = 64, IsNullable = true, ColumnDescription = "运行环境（Development、Production…）")]
    public string? EnvironmentName { get; set; }

    /// <summary>
    /// 应用版本
    /// </summary>
    [SugarColumn(ColumnName = "app_version", Length = 64, IsNullable = true, ColumnDescription = "应用版本")]
    public string? AppVersion { get; set; }
}
