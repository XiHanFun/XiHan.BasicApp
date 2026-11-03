// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SqlSugar;

namespace XiHan.BasicApp.Runtime.Persistence;

/// <summary>
/// SQL 诊断日志
/// </summary>
/// <remarks>
/// 按 <see cref="DatabaseOptions"/> 挂到连接配置的 AOP 事件上：每条 SQL、慢 SQL 与 SQL 错误。只记语句，不记参数值
/// 连接配置在装配时就建好，那时还没有日志工厂：缺省记到 <see cref="SharedLogger"/>，由运行时包启动时换上
/// </remarks>
/// <param name="options">配置</param>
/// <param name="logger">日志；为空即 <see cref="SharedLogger"/></param>
public sealed partial class SqlDiagnostics(DatabaseOptions options, ILogger? logger = null)
{
    private const int MaxSqlLength = 4000;

    private static readonly AsyncLocal<long> _startedAt = new();

    /// <summary>
    /// 共用的日志
    /// </summary>
    public static ILogger SharedLogger { get; set; } = NullLogger.Instance;

    private ILogger Logger => logger ?? SharedLogger;

    /// <summary>
    /// 把日志挂到 AOP 事件上
    /// </summary>
    /// <param name="events">连接配置的 AOP 事件</param>
    public void Attach(AopEvents events)
    {
        ArgumentNullException.ThrowIfNull(events);
        if (options.LogSql || options.LogSlowSql)
        {
            events.OnLogExecuting = (_, _) => _startedAt.Value = Stopwatch.GetTimestamp();
            events.OnLogExecuted = (sql, _) => Executed(sql);
        }

        if (options.LogSqlErrors)
        {
            events.OnError = exception =>
            {
                var text = Trim(exception.Sql);
                LogSqlFailed(Logger, exception, text);
            };
        }
    }

    private void Executed(string sql)
    {
        var elapsed = Stopwatch.GetElapsedTime(_startedAt.Value).TotalMilliseconds;
        var text = Trim(sql);
        if (options.LogSql)
        {
            LogSqlExecuted(Logger, elapsed, text);
        }

        if (options.LogSlowSql && elapsed >= options.SlowSqlThresholdMilliseconds)
        {
            LogSlowSqlExecuted(Logger, elapsed, text);
        }
    }

    private static string Trim(string? sql)
        => sql is null ? string.Empty : sql.Length <= MaxSqlLength ? sql : sql[..MaxSqlLength];

    [LoggerMessage(Level = LogLevel.Information, Message = "SQL executed in {ElapsedMilliseconds:F1} ms: {Sql}")]
    private static partial void LogSqlExecuted(ILogger logger, double elapsedMilliseconds, string sql);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Slow SQL took {ElapsedMilliseconds:F1} ms: {Sql}")]
    private static partial void LogSlowSqlExecuted(ILogger logger, double elapsedMilliseconds, string sql);

    [LoggerMessage(Level = LogLevel.Error, Message = "SQL failed: {Sql}")]
    private static partial void LogSqlFailed(ILogger logger, Exception exception, string sql);
}
