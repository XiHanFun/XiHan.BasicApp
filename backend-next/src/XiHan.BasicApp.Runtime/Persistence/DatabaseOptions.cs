// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Globalization;
using Microsoft.Extensions.Configuration;

namespace XiHan.BasicApp.Runtime.Persistence;

/// <summary>
/// 平台库的一个从库
/// </summary>
/// <param name="ConnectionString">连接串</param>
/// <param name="HitRate">读权重，数字越大分到的读越多；0 即交给框架的缺省权重</param>
public sealed record DatabaseReplica(string ConnectionString, int HitRate);

/// <summary>
/// 数据库连接的应用配置
/// </summary>
/// <remarks>
/// 配置节 <c>BasicApp:Database</c>：平台库的从库（读写分离）与 SQL 诊断日志
/// 从库只给平台库：独立库租户的连接串来自租户目录，平台的从库绝不能用到它们身上
/// </remarks>
public sealed record DatabaseOptions
{
    /// <summary>
    /// 配置节
    /// </summary>
    public const string ConfigurationSection = "BasicApp:Database";

    /// <summary>
    /// 平台库的从库
    /// </summary>
    /// <remarks>
    /// 配置键 <c>Replicas</c>，每项 <c>ConnectionString</c> 与可选的 <c>HitRate</c>。没配即单库。配了之后只读工作单元的读走从库、会写的工作单元走主库；缺省权重与健康探测在 <c>XiHan:Persistence:SqlSugar:Replicas</c>
    /// </remarks>
    public IReadOnlyList<DatabaseReplica> Replicas { get; init; } = [];

    /// <summary>
    /// 是否记录每条 SQL
    /// </summary>
    /// <remarks>
    /// 配置键 <c>LogSql</c>，缺省关。日志量大，只在排查时打开
    /// </remarks>
    public bool LogSql { get; init; }

    /// <summary>
    /// 是否记录 SQL 错误
    /// </summary>
    /// <remarks>
    /// 配置键 <c>LogSqlErrors</c>，缺省开
    /// </remarks>
    public bool LogSqlErrors { get; init; } = true;

    /// <summary>
    /// 是否记录慢 SQL
    /// </summary>
    /// <remarks>
    /// 配置键 <c>LogSlowSql</c>，缺省开
    /// </remarks>
    public bool LogSlowSql { get; init; } = true;

    /// <summary>
    /// 慢 SQL 的阈值（毫秒）
    /// </summary>
    /// <remarks>
    /// 配置键 <c>SlowSqlThresholdMilliseconds</c>，缺省 1000。只影响记录，不影响语句执行
    /// </remarks>
    public int SlowSqlThresholdMilliseconds { get; init; } = 1000;

    /// <summary>
    /// 读取配置
    /// </summary>
    /// <exception cref="ArgumentException">配置写错，启动即失败</exception>
    public static DatabaseOptions FromConfiguration(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        var section = configuration.GetSection(ConfigurationSection);
        var replicas = new List<DatabaseReplica>();
        foreach (var child in section.GetSection(nameof(Replicas)).GetChildren())
        {
            if (string.IsNullOrWhiteSpace(child["ConnectionString"]))
            {
                throw new ArgumentException($"'{ConfigurationSection}:Replicas:{child.Key}:ConnectionString' must not be empty.");
            }

            var hitRate = 0;
            if (child["HitRate"] is { Length: > 0 } raw && (!int.TryParse(raw, NumberStyles.None, CultureInfo.InvariantCulture, out hitRate) || hitRate <= 0))
            {
                throw new ArgumentException($"'{ConfigurationSection}:Replicas:{child.Key}:HitRate' must be a positive integer, but was '{raw}'.");
            }

            replicas.Add(new DatabaseReplica(child["ConnectionString"]!, hitRate));
        }

        var threshold = 1000;
        if (section[nameof(SlowSqlThresholdMilliseconds)] is { Length: > 0 } thresholdText && (!int.TryParse(thresholdText, NumberStyles.None, CultureInfo.InvariantCulture, out threshold) || threshold <= 0))
        {
            throw new ArgumentException($"'{ConfigurationSection}:{nameof(SlowSqlThresholdMilliseconds)}' must be a positive integer, but was '{thresholdText}'.");
        }

        return new DatabaseOptions
        {
            Replicas = replicas,
            LogSql = Switch(section, nameof(LogSql), defaultValue: false),
            LogSqlErrors = Switch(section, nameof(LogSqlErrors), defaultValue: true),
            LogSlowSql = Switch(section, nameof(LogSlowSql), defaultValue: true),
            SlowSqlThresholdMilliseconds = threshold,
        };
    }

    private static bool Switch(IConfigurationSection section, string key, bool defaultValue)
    {
        if (section[key] is not { Length: > 0 } raw)
        {
            return defaultValue;
        }

        return bool.TryParse(raw, out var value)
            ? value
            : throw new ArgumentException($"'{ConfigurationSection}:{key}' must be 'true' or 'false', but was '{raw}'.");
    }
}
