// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Globalization;
using Microsoft.Extensions.Configuration;
using XiHan.Framework.Scheduling.Abstractions.Schedules;

namespace XiHan.BasicApp.Auditing;

/// <summary>
/// 审计日志的选项，配置节 <c>BasicApp:Auditing</c>。
/// </summary>
/// <remarks>
/// 与框架的 <c>XiHan:Auditing</c>（操作审计中间件）分开：这里管的是 BasicApp 自己的分表日志。
/// </remarks>
public sealed record AuditLogOptions
{
    /// <summary>
    /// 配置节。
    /// </summary>
    public const string ConfigurationSection = "BasicApp:Auditing";

    /// <summary>
    /// 保留天数参数的键（Settings 的系统参数，平台一侧）。
    /// </summary>
    /// <remarks>
    /// 没建这个参数即用 <see cref="DefaultRetentionDays"/>。
    /// </remarks>
    public const string RetentionDaysConfigKey = "auditing.retention-days";

    /// <summary>
    /// 等待落库的日志条数上限（<c>QueueCapacity</c>，缺省 10000）。
    /// </summary>
    /// <remarks>
    /// 满了新到的丢弃并计数，不阻塞调用。
    /// </remarks>
    public int QueueCapacity { get; init; } = 10_000;

    /// <summary>
    /// 一批落库的条数上限（<c>BatchSize</c>，缺省 200）：攒够即刷写。
    /// </summary>
    public int BatchSize { get; init; } = 200;

    /// <summary>
    /// 刷写间隔（<c>FlushIntervalSeconds</c>，缺省 1 秒）。
    /// </summary>
    /// <remarks>
    /// 不足一批也按此周期刷写。
    /// </remarks>
    public TimeSpan FlushInterval { get; init; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// 是否记访问日志（<c>AccessLog:Enabled</c>，缺省开）。
    /// </summary>
    public bool AccessLogEnabled { get; init; } = true;

    /// <summary>
    /// 不记访问日志的操作 ID（<c>AccessLog:Exclude</c>，缺省空）。
    /// </summary>
    /// <remarks>
    /// 异常、登录与变更日志不受影响。
    /// </remarks>
    public IReadOnlySet<string> AccessLogExclusions { get; init; } = new HashSet<string>(StringComparer.Ordinal);

    /// <summary>
    /// 没建保留天数参数时的保留天数。
    /// </summary>
    /// <remarks>
    /// 配置项 <c>DefaultRetentionDays</c>，缺省 180。
    /// </remarks>
    public int DefaultRetentionDays { get; init; } = 180;

    /// <summary>
    /// 保留清理作业的计划。
    /// </summary>
    /// <remarks>
    /// <c>RetentionCron</c> 写 cron 表达式，按 UTC；缺省每天 03:00。
    /// </remarks>
    public JobSchedule RetentionSchedule { get; init; } = JobSchedule.FromCron("0 3 * * *");

    /// <summary>
    /// 从配置读取选项。
    /// </summary>
    /// <exception cref="ArgumentException">不合法的值是装配错误，直接抛出。</exception>
    public static AuditLogOptions FromConfiguration(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        var section = configuration.GetSection(ConfigurationSection);
        var options = new AuditLogOptions();
        if (ReadPositiveInteger(section, "QueueCapacity") is { } capacity)
        {
            options = options with { QueueCapacity = capacity };
        }

        if (ReadPositiveInteger(section, "BatchSize") is { } batch)
        {
            options = options with { BatchSize = batch };
        }

        if (section["FlushIntervalSeconds"] is { Length: > 0 } interval)
        {
            options = double.TryParse(interval, NumberStyles.Float, CultureInfo.InvariantCulture, out var seconds) && seconds > 0
                ? options with { FlushInterval = TimeSpan.FromSeconds(seconds) }
                : throw new ArgumentException($"'{ConfigurationSection}:FlushIntervalSeconds' must be a positive number, but was '{interval}'.");
        }

        if (section["AccessLog:Enabled"] is { Length: > 0 } enabled)
        {
            options = bool.TryParse(enabled, out var parsed)
                ? options with { AccessLogEnabled = parsed }
                : throw new ArgumentException($"'{ConfigurationSection}:AccessLog:Enabled' must be 'true' or 'false', but was '{enabled}'.");
        }

        var exclusions = section.GetSection("AccessLog:Exclude").GetChildren().Select(static c => c.Value).Where(static v => !string.IsNullOrWhiteSpace(v)).Select(static v => v!.Trim()).ToHashSet(StringComparer.Ordinal);
        if (exclusions.Count > 0)
        {
            options = options with { AccessLogExclusions = exclusions };
        }

        if (ReadPositiveInteger(section, "DefaultRetentionDays") is { } days)
        {
            options = options with { DefaultRetentionDays = days };
        }

        if (section["RetentionCron"] is { Length: > 0 } cron)
        {
            try
            {
                options = options with { RetentionSchedule = JobSchedule.FromCron(cron) };
            }
            catch (ArgumentException exception)
            {
                throw new ArgumentException($"'{ConfigurationSection}:RetentionCron' is not a valid cron expression: '{cron}'.", exception);
            }
        }

        return options;
    }

    private static int? ReadPositiveInteger(IConfiguration section, string key)
    {
        var raw = section[key];
        if (string.IsNullOrEmpty(raw))
        {
            return null;
        }

        return int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) && value > 0
            ? value
            : throw new ArgumentException($"'{ConfigurationSection}:{key}' must be a positive integer, but was '{raw}'.");
    }
}
