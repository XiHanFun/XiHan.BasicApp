#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SaasTelegramMessageAuditStore
// Guid:a9c2e7f4-1b58-4d63-8e0a-6f3d5c9b2e71
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/03 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.Framework.Bot.Telegram.Abstractions;
using XiHan.Framework.Data.SqlSugar.Clients;

namespace XiHan.BasicApp.Saas.Infrastructure.Messaging;

/// <summary>
/// SaaS Telegram 出站消息审计存储（数据库实现，覆盖框架默认 no-op 实现）
/// </summary>
/// <remarks>
/// 审计记录直插 <see cref="SysTelegramMessage"/> 月分表；
/// <b>任何异常吞掉仅记日志，审计不得阻断发送主流程</b>。
/// Singleton 生命周期，Scoped 数据库客户端经 <see cref="IServiceScopeFactory"/> 开作用域解析。
/// </remarks>
public sealed class SaasTelegramMessageAuditStore : ITelegramMessageAuditStore
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SaasTelegramMessageAuditStore> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="scopeFactory">服务作用域工厂（用于解析 Scoped 数据库客户端）</param>
    /// <param name="logger">日志记录器</param>
    public SaasTelegramMessageAuditStore(
        IServiceScopeFactory scopeFactory,
        ILogger<SaasTelegramMessageAuditStore> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task AppendAsync(TelegramMessageAuditRecord record, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(record);

            var entity = new SysTelegramMessage
            {
                BotName = record.BotName,
                BotConfigId = record.BotConfigId,
                ChatId = record.ChatId,
                ApiMethod = record.ApiMethod,
                MessageType = record.MessageType,
                Content = record.Content,
                ParseMode = record.ParseMode,
                TelegramMessageId = record.TelegramMessageId,
                Success = record.Success,
                ErrorCode = record.ErrorCode,
                ErrorMessage = Truncate(record.ErrorMessage, 1000),
                ElapsedMs = record.ElapsedMs,
                SendTime = record.SendTime,
                // 分表字段：以发送时间落表，保证审计行按业务时间归档
                CreatedTime = record.SendTime
            };

            await using var scope = _scopeFactory.CreateAsyncScope();
            var clientResolver = scope.ServiceProvider.GetRequiredService<ISqlSugarClientResolver>();
            var db = clientResolver.GetCurrentClient();
            _ = await db.Insertable(entity).SplitTable().ExecuteCommandAsync();
        }
        catch (Exception ex)
        {
            // 审计失败不抛出：记日志后放行，发送主流程不受影响
            _logger.LogError(ex, "Telegram 出站消息审计落库失败。Bot={BotName}, Api={ApiMethod}, ChatId={ChatId}",
                record?.BotName, record?.ApiMethod, record?.ChatId);
        }
    }

    /// <summary>
    /// 按列长截断（防御性兜底，超长错误信息不应导致整行插入失败）
    /// </summary>
    private static string? Truncate(string? value, int maxLength)
    {
        return value is null || value.Length <= maxLength ? value : value[..maxLength];
    }
}
