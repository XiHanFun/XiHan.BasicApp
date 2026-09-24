// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Logging;
using SqlSugar;
using XiHan.BasicApp.Chat.Domain.Configurations;
using XiHan.BasicApp.Chat.Domain.Entities;
using XiHan.BasicApp.Saas.Application.Services;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.MultiTenancy.Abstractions;

namespace XiHan.BasicApp.Chat.Infrastructure.Tasks;

/// <summary>
/// 聊天消息保留清理任务：按保留期物理删除过期聊天消息及其表情回应，防止两张表无限增长
/// </summary>
/// <remarks>
/// <para>由动态任务调度（SysTask：TaskClass=本类全名，TaskMethod=ExecuteAsync，建议 Cron 每日凌晨）触发。</para>
/// <para>清理范围：过期消息 + 这些消息名下的全部表情回应（回应无独立保留期，随所属消息一同消失）。</para>
/// <para>保留期天数：平台聊天策略 <see cref="ChatConfigKeys.Policy"/> 的 retentionDays，未配置按 <see cref="ChatPolicySettings"/> 的默认值，配置非法直接失败；
/// 聊天与审计日志留存合规口径不同，独立配置。</para>
/// <para>聊天实体严格隔离：逐数据作用域（平台 + 每个数据可达的租户）切入清理，库隔离租户在它自己的库里清理。</para>
/// </remarks>
public sealed class ChatRetentionCleanupTask
{
    private readonly ISqlSugarClientResolver _clientResolver;

    private readonly ISaasConfigurationService _configuration;

    private readonly ITenantDataScopeRunner _scopeRunner;

    private readonly ICurrentTenant _currentTenant;

    private readonly ILogger<ChatRetentionCleanupTask> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    public ChatRetentionCleanupTask(
        ISqlSugarClientResolver clientResolver,
        ISaasConfigurationService configuration,
        ITenantDataScopeRunner scopeRunner,
        ICurrentTenant currentTenant,
        ILogger<ChatRetentionCleanupTask> logger)
    {
        _clientResolver = clientResolver;
        _configuration = configuration;
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
            retentionDays = await ResolveRetentionDaysAsync();
        }

        var cutoff = DateTimeOffset.UtcNow.AddDays(-retentionDays);
        long reactionCount = 0;
        long count = 0;
        var failures = new List<string>();

        await _scopeRunner.RunAsync(async tenantId =>
        {
            try
            {
                var (reactions, messages) = await CleanupScopeAsync(_clientResolver.GetCurrentClient(), cutoff);
                reactionCount += reactions;
                count += messages;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "清理作用域 {TenantId} 的聊天消息失败", tenantId?.ToString() ?? "平台");
                failures.Add(tenantId?.ToString() ?? "平台");
            }
        });

        var summary = failures.Count == 0
            ? $"聊天消息清理完成：保留 {retentionDays} 天（截止 {cutoff:yyyy-MM-dd}），共删除消息 {count} 行、表情回应 {reactionCount} 行"
            : $"聊天消息清理部分失败：保留 {retentionDays} 天（截止 {cutoff:yyyy-MM-dd}），共删除消息 {count} 行、表情回应 {reactionCount} 行，失败作用域 {string.Join("，", failures)}";
        _logger.LogInformation("{Summary}", summary);
        return summary;
    }

    /// <summary>
    /// 清理当前作用域内的过期消息与表情回应
    /// </summary>
    private static async Task<(int Reactions, int Messages)> CleanupScopeAsync(ISqlSugarClient client, DateTimeOffset cutoff)
    {
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

        var messageCount = await client.Deleteable<SysChatMessage>()
            .Where(message => message.CreatedTime < cutoff)
            .ExecuteCommandAsync();

        return (reactionCount, messageCount);
    }

    /// <summary>
    /// 解析保留天数：平台的聊天策略，未配置用默认值；不是正整数直接失败（不按错误的保留期删数据）
    /// </summary>
    private async Task<int> ResolveRetentionDaysAsync()
    {
        var policy = await _configuration.GetJsonAsync(ChatConfigKeys.Policy, new ChatPolicySettings());
        return policy.RetentionDays > 0
            ? policy.RetentionDays
            : throw new InvalidOperationException($"聊天策略 {ChatConfigKeys.Policy} 的保留天数必须是正整数，当前为 {policy.RetentionDays}。");
    }
}
