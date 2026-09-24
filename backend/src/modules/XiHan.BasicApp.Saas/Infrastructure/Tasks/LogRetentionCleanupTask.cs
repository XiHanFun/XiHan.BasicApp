// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Logging;
using SqlSugar;
using XiHan.BasicApp.Core.Entities;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.Domain.Entities.Abstracts;
using XiHan.Framework.MultiTenancy.Abstractions;

namespace XiHan.BasicApp.Saas.Infrastructure.Tasks;

/// <summary>
/// 日志保留清理任务：按保留期删除 7 类按月分表的审计日志的过期行，防止分月表无限增长
/// </summary>
/// <remarks>
/// <para>由动态任务调度（SysTask：TaskClass=本类全名，TaskMethod=ExecuteAsync，建议 Cron 每日凌晨）触发。</para>
/// <para>口径：</para>
/// <list type="bullet">
///   <item>保留期天数：读取平台配置 <c>saas:log:retention-days</c>（TenantId=0）；未配置时为 <see cref="DefaultRetentionDays"/> 天，配置非法直接报错；</item>
///   <item>覆盖：访问/操作/异常/登录/差异/开放接口/权限变更 共 7 类日志，统一按分表字段 CreatedTime 删除早于截止时间的行；</item>
///   <item>日志严格按上下文隔离，逐个作用域（平台与每个数据可达的租户）切入清理，库隔离租户在它自己的库里清理；删除走 SqlSugar SplitTable（仅命中实际存在的月表）；</item>
///   <item>单个作用域、单类失败不影响其它（逐项 try/catch 并记错误日志），结果汇总返回。</item>
/// </list>
/// <para>说明：本任务只删行不 DROP 表；空月表保留对运行无影响，如需物理回收可另行 DROP。</para>
/// </remarks>
public sealed class LogRetentionCleanupTask
{
    /// <summary>
    /// 默认保留天数（未配置 saas:log:retention-days 时使用）
    /// </summary>
    private const int DefaultRetentionDays = 180;

    /// <summary>
    /// 保留期配置键（全局，TenantId=0）
    /// </summary>
    private const string RetentionConfigKey = "saas:log:retention-days";

    private readonly ISqlSugarClientResolver _clientResolver;

    private readonly ITenantDataScopeRunner _scopeRunner;

    private readonly ICurrentTenant _currentTenant;

    private readonly ILogger<LogRetentionCleanupTask> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    public LogRetentionCleanupTask(
        ISqlSugarClientResolver clientResolver,
        ITenantDataScopeRunner scopeRunner,
        ICurrentTenant currentTenant,
        ILogger<LogRetentionCleanupTask> logger)
    {
        _clientResolver = clientResolver;
        _scopeRunner = scopeRunner;
        _currentTenant = currentTenant;
        _logger = logger;
    }

    /// <summary>
    /// 执行清理（动态任务反射入口）
    /// </summary>
    /// <returns>清理结果摘要</returns>
    public async Task<string> ExecuteAsync()
    {
        int retentionDays;
        using (_currentTenant.Change(null))
        {
            retentionDays = await ResolveRetentionDaysAsync(_clientResolver.GetClientForEntity<SysConfig>());
        }

        var cutoff = DateTimeOffset.UtcNow.AddDays(-retentionDays);
        long total = 0;
        var failures = new List<string>();

        await _scopeRunner.RunAsync(async tenantId =>
        {
            var client = _clientResolver.GetCurrentClient();
            var jobs = new (string Name, Func<Task<int>> Run)[]
            {
                ("访问", () => CleanupAsync<SysAccessLog>(client, cutoff)),
                ("操作", () => CleanupAsync<SysOperationLog>(client, cutoff)),
                ("异常", () => CleanupAsync<SysExceptionLog>(client, cutoff)),
                ("登录", () => CleanupAsync<SysLoginLog>(client, cutoff)),
                ("差异", () => CleanupAsync<SysDiffLog>(client, cutoff)),
                ("开放接口", () => CleanupAsync<SysOpenApiLog>(client, cutoff)),
                ("权限变更", () => CleanupAsync<SysPermissionChangeLog>(client, cutoff))
            };

            foreach (var (name, run) in jobs)
            {
                try
                {
                    total += await run();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "清理作用域 {TenantId} 的{Name}日志失败", tenantId?.ToString() ?? "平台", name);
                    failures.Add($"{tenantId?.ToString() ?? "平台"}/{name}");
                }
            }
        });

        var summary = failures.Count == 0
            ? $"日志清理完成：日志保留 {retentionDays} 天（截止 {cutoff:yyyy-MM-dd}），共删除 {total} 行"
            : $"日志清理部分失败：日志保留 {retentionDays} 天（截止 {cutoff:yyyy-MM-dd}），共删除 {total} 行，失败 {string.Join("，", failures)}";
        _logger.LogInformation("{Summary}", summary);
        return summary;
    }

    /// <summary>
    /// 删除某类日志早于截止时间的行（按月分表，仅命中实际存在的月表）
    /// </summary>
    private static async Task<int> CleanupAsync<T>(ISqlSugarClient client, DateTimeOffset cutoff)
        where T : BasicAppCreationEntity, ISplitTableEntity, new()
    {
        // 无参 SplitTable() 仅支持按实体集合删除（运行时抛异常），条件删除必须走带表筛选的重载
        return await client.Deleteable<T>()
            .Where(entity => entity.CreatedTime < cutoff)
            .SplitTable(tabs => tabs)
            .ExecuteCommandAsync();
    }

    /// <summary>
    /// 解析保留天数：全局配置优先，缺省/非法时回退默认值
    /// </summary>
    private static async Task<int> ResolveRetentionDaysAsync(ISqlSugarClient client)
    {
        var value = await client.Queryable<SysConfig>()
            .Where(config => config.ConfigKey == RetentionConfigKey
                && config.TenantId == 0
                && config.Status == EnableStatus.Enabled)
            .Select(config => config.ConfigValue)
            .FirstAsync();

        if (string.IsNullOrWhiteSpace(value))
        {
            return DefaultRetentionDays;
        }

        return int.TryParse(value, out var days) && days > 0
            ? days
            : throw new InvalidOperationException($"日志保留期配置 {RetentionConfigKey} 的值「{value}」不是正整数。");
    }
}
