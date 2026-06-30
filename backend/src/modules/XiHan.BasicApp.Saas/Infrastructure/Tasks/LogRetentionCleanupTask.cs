#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:LogRetentionCleanupTask
// Guid:9b4e7c3a-8d6f-4a1b-be55-3f4a5b6c7d8e
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/01 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.Extensions.Logging;
using SqlSugar;
using XiHan.BasicApp.Core.Entities;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
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
///   <item>保留期天数：优先读取全局配置 <c>saas:log:retention-days</c>（TenantId=0），缺省/非法时回退 <see cref="DefaultRetentionDays"/> 天；</item>
///   <item>覆盖：访问/操作/异常/登录/差异/开放接口/权限变更 共 7 类日志，统一按分表字段 CreatedTime 删除早于截止时间的行；</item>
///   <item>平台态执行（关闭租户过滤）跨租户清理，删除走 SqlSugar SplitTable（仅命中实际存在的月表）；</item>
///   <item>单类失败不影响其它类（逐类 try/catch），结果汇总返回。</item>
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

    private readonly ICurrentTenant _currentTenant;

    private readonly ILogger<LogRetentionCleanupTask> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    public LogRetentionCleanupTask(
        ISqlSugarClientResolver clientResolver,
        ICurrentTenant currentTenant,
        ILogger<LogRetentionCleanupTask> logger)
    {
        _clientResolver = clientResolver;
        _currentTenant = currentTenant;
        _logger = logger;
    }

    /// <summary>
    /// 执行清理（动态任务反射入口）
    /// </summary>
    /// <returns>清理结果摘要</returns>
    public async Task<string> ExecuteAsync()
    {
        using var platformScope = _currentTenant.Change(null);
        var client = _clientResolver.GetCurrentClient();

        var retentionDays = await ResolveRetentionDaysAsync(client);
        var cutoff = DateTimeOffset.UtcNow.AddDays(-retentionDays);

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

        long total = 0;
        var parts = new List<string>(jobs.Length);
        foreach (var (name, run) in jobs)
        {
            try
            {
                var count = await run();
                total += count;
                parts.Add($"{name} {count}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "清理 {Name} 日志失败", name);
                parts.Add($"{name} 失败");
            }
        }

        var summary = $"日志清理完成：保留 {retentionDays} 天（截止 {cutoff:yyyy-MM-dd}），共删除 {total} 行（{string.Join("，", parts)}）";
        _logger.LogInformation("{Summary}", summary);
        return summary;
    }

    /// <summary>
    /// 删除某类日志早于截止时间的行（按月分表，仅命中实际存在的月表）
    /// </summary>
    private static async Task<int> CleanupAsync<T>(ISqlSugarClient client, DateTimeOffset cutoff)
        where T : BasicAppCreationEntity, ISplitTableEntity, new()
    {
        return await client.Deleteable<T>()
            .Where(entity => entity.CreatedTime < cutoff)
            .SplitTable()
            .ExecuteCommandAsync();
    }

    /// <summary>
    /// 解析保留天数：全局配置优先，缺省/非法时回退默认值
    /// </summary>
    private async Task<int> ResolveRetentionDaysAsync(ISqlSugarClient client)
    {
        try
        {
            var value = await client.Queryable<SysConfig>()
                .Where(config => config.ConfigKey == RetentionConfigKey
                    && config.TenantId == 0
                    && config.Status == EnableStatus.Enabled)
                .Select(config => config.ConfigValue)
                .FirstAsync();

            if (int.TryParse(value, out var days) && days > 0)
            {
                return days;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "读取日志保留期配置 {Key} 失败，回退默认 {Default} 天", RetentionConfigKey, DefaultRetentionDays);
        }

        return DefaultRetentionDays;
    }
}
