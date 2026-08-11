// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Logging;
using SqlSugar;
using XiHan.BasicApp.Chat.Domain.Configurations;
using XiHan.BasicApp.Chat.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.MultiTenancy.Abstractions;

namespace XiHan.BasicApp.Chat.Infrastructure.Tasks;

/// <summary>
/// 聊天消息保留清理任务：按保留期物理删除过期聊天消息，防止消息表无限增长
/// </summary>
/// <remarks>
/// <para>由动态任务调度（SysTask：TaskClass=本类全名，TaskMethod=ExecuteAsync，建议 Cron 每日凌晨）触发。</para>
/// <para>保留期天数：优先读取全局配置 <c>chat:retention-days</c>（TenantId=0），缺省/非法时回退 <see cref="DefaultRetentionDays"/> 天；
/// 平台态执行（关闭租户过滤）跨租户清理；聊天与审计日志留存合规口径不同，独立配置。</para>
/// </remarks>
public sealed class ChatRetentionCleanupTask
{
    /// <summary>
    /// 默认保留天数（未配置 chat:retention-days 时使用）
    /// </summary>
    private const int DefaultRetentionDays = 365;

    private readonly ISqlSugarClientResolver _clientResolver;

    private readonly ICurrentTenant _currentTenant;

    private readonly ILogger<ChatRetentionCleanupTask> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    public ChatRetentionCleanupTask(
        ISqlSugarClientResolver clientResolver,
        ICurrentTenant currentTenant,
        ILogger<ChatRetentionCleanupTask> logger)
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

        var count = await client.Deleteable<SysChatMessage>()
            .Where(message => message.CreatedTime < cutoff)
            .ExecuteCommandAsync();

        var summary = $"聊天消息清理完成：保留 {retentionDays} 天（截止 {cutoff:yyyy-MM-dd}），共删除 {count} 行";
        _logger.LogInformation("{Summary}", summary);
        return summary;
    }

    /// <summary>
    /// 解析保留天数：全局配置优先，缺省/非法时回退默认值
    /// </summary>
    private async Task<int> ResolveRetentionDaysAsync(ISqlSugarClient client)
    {
        try
        {
            var value = await client.Queryable<SysConfig>()
                .Where(config => config.ConfigKey == ChatConfigKeys.RetentionDays
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
            _logger.LogWarning(ex, "读取聊天保留期配置失败，回退默认 {Default} 天", DefaultRetentionDays);
        }

        return DefaultRetentionDays;
    }
}
