// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace XiHan.BasicApp.Integration.Tests.Infrastructure;

/// <summary>
/// 记下一个宿主写的日志
/// </summary>
/// <remarks>
/// 建宿主时 <c>builder.Logging.AddProvider</c> 加上；只收过了宿主日志级别的条目（测试缺省 Warning 起）
/// </remarks>
public sealed class LogCapture : ILoggerProvider
{
    private readonly ConcurrentQueue<LogEntry> _entries = new();

    public IReadOnlyList<LogEntry> Entries => [.. _entries];

    public ILogger CreateLogger(string categoryName) => new Logger(categoryName, _entries);

    public void Dispose()
    {
    }

    private sealed class Logger(string category, ConcurrentQueue<LogEntry> entries) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            => entries.Enqueue(new LogEntry(category, logLevel, formatter(state, exception)));
    }
}

/// <summary>
/// 一条日志
/// </summary>
/// <param name="Category">类别</param>
/// <param name="Level">级别</param>
/// <param name="Message">格式化后的消息</param>
public sealed record LogEntry(string Category, LogLevel Level, string Message);
