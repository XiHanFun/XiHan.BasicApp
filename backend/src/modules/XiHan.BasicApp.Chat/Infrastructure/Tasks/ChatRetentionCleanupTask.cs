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
/// 聊天消息保留清理任务：按保留期物理删除过期聊天消息及其表情回应，防止两张表无限增长
/// </summary>
/// <remarks>
/// <para>由动态任务调度（SysTask：TaskClass=本类全名，TaskMethod=ExecuteAsync，建议 Cron 每日凌晨）触发。</para>
/// <para>清理范围：过期消息 + 这些消息名下的全部表情回应（回应无独立保留期，随所属消息一同消失）。</para>
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

        // 先级联删回应再删消息：回应行没有独立保留期，其存活期完全由所属消息决定
        // （SysChatMessageReaction 的实体注释即以此为契约）。顺序反过来的话，消息行一旦消失，
        // 就再也无法按「所属消息已过期」筛出回应，回应表会只增不减。
        // 判据用「所属消息已过期」而非回应自身的 CreatedTime：回应必晚于消息，按回应时间删
        // 会漏掉「老消息 + 新回应」这一类，正是悬空外键的来源。
        var reactionCount = await client.Deleteable<SysChatMessageReaction>()
            .Where(reaction => SqlFunc.Subqueryable<SysChatMessage>()
                .Where(message => message.BasicId == reaction.MessageId && message.CreatedTime < cutoff)
                .Any())
            .ExecuteCommandAsync();

        var count = await client.Deleteable<SysChatMessage>()
            .Where(message => message.CreatedTime < cutoff)
            .ExecuteCommandAsync();

        var summary = $"聊天消息清理完成：保留 {retentionDays} 天（截止 {cutoff:yyyy-MM-dd}），共删除消息 {count} 行、表情回应 {reactionCount} 行";
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
