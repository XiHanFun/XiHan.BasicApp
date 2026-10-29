// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text.Json.Nodes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SqlSugar;
using XiHan.BasicApp.Notification.Domain.Entities;
using XiHan.BasicApp.Notification.Domain.Services;
using XiHan.BasicApp.Runtime.Persistence;
using XiHan.Framework.Persistence.SqlSugar;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Notification.Infrastructure;

/// <summary>
/// 邮件、短信与机器人消息的发送循环（单例，通知包的后台循环；4.x 用 Redis 延迟队列）：每轮取到点的排队记录，先用条件更新把它改成发送中（多实例只有一个能领到），
/// 再按所属侧的缺省配置（没有取平台的）发出；成功记发出时间，失败按「第 n 次等 n 倍重试间隔」退避，用完次数即失败。
/// 启动时把发送中超过 <see cref="NotificationOptions.StuckAfter"/> 的记录改回排队中（上个进程发到一半）。
/// </summary>
/// <param name="scopes">作用域工厂。</param>
/// <param name="signal">唤醒信号。</param>
/// <param name="options">选项。</param>
/// <param name="clock">时钟。</param>
/// <param name="emailTransport">邮件通道。</param>
/// <param name="smsTransports">短信通道（每个服务商一个）。</param>
/// <param name="botTransports">机器人通道（每个服务商一个）。</param>
/// <param name="logger">日志。</param>
public sealed partial class MessageDispatcher(
    IServiceScopeFactory scopes,
    MessageDispatchSignal signal,
    NotificationOptions options,
    TimeProvider clock,
    IEmailTransport emailTransport,
    IEnumerable<ISmsTransport> smsTransports,
    IEnumerable<IBotTransport> botTransports,
    ILogger<MessageDispatcher> logger)
{
    private const int BatchSize = 20;

    private readonly ISmsTransport[] _sms = smsTransports.ToArray();

    private readonly IBotTransport[] _bots = botTransports.ToArray();

    /// <summary>
    /// 循环到停止。
    /// </summary>
    /// <param name="stopping">停止信号。</param>
    /// <returns>循环。</returns>
    public async Task RunAsync(CancellationToken stopping)
    {
        while (!stopping.IsCancellationRequested)
        {
            try
            {
                await DispatchOnceAsync(stopping).ConfigureAwait(false);
                await signal.WaitAsync(options.DispatchInterval, stopping).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stopping.IsCancellationRequested)
            {
                return;
            }
#pragma warning disable CA1031 // 一轮出错（数据库暂时连不上等）不结束循环，下一轮再来
            catch (Exception exception)
#pragma warning restore CA1031
            {
                LogRoundFailed(logger, exception);
                await Task.Delay(options.DispatchInterval, stopping).ConfigureAwait(false);
            }
        }
    }

    /// <summary>
    /// 把发送中太久的记录改回排队中。
    /// </summary>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>结果。</returns>
    public Task<Result> RecoverAsync(CancellationToken cancellationToken)
    {
        var cutoff = clock.GetUtcNow() - options.StuckAfter;
        return StoreScope.WriteAsync(scopes, async (services, ct) =>
        {
            var unitOfWork = services.GetRequiredService<SqlSugarUnitOfWork>();
            await unitOfWork.EnsureTransactionAsync(ct).ConfigureAwait(false);
            await unitOfWork.ConnectionFor(typeof(SysEmail)).Updateable<SysEmail>()
                .SetColumns(e => new SysEmail { Status = MessageSendStatus.Pending })
                .Where(e => e.Status == MessageSendStatus.Sending && SqlFunc.IsNull(e.ModifiedAt, e.CreatedAt) < cutoff)
                .ExecuteCommandAsync(ct).ConfigureAwait(false);
            await unitOfWork.ConnectionFor(typeof(SysSms)).Updateable<SysSms>()
                .SetColumns(s => new SysSms { Status = MessageSendStatus.Pending })
                .Where(s => s.Status == MessageSendStatus.Sending && SqlFunc.IsNull(s.ModifiedAt, s.CreatedAt) < cutoff)
                .ExecuteCommandAsync(ct).ConfigureAwait(false);
            await unitOfWork.ConnectionFor(typeof(SysBotMessage)).Updateable<SysBotMessage>()
                .SetColumns(b => new SysBotMessage { Status = MessageSendStatus.Pending })
                .Where(b => b.Status == MessageSendStatus.Sending && SqlFunc.IsNull(b.ModifiedAt, b.CreatedAt) < cutoff)
                .ExecuteCommandAsync(ct).ConfigureAwait(false);
            return Result.Success();
        }, cancellationToken);
    }

    /// <summary>
    /// 发一轮：每类至多 <c>20</c> 条到点的排队记录（邮件、短信、机器人消息）。
    /// </summary>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>一轮。</returns>
    public async Task DispatchOnceAsync(CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var past = now.AddYears(-100);
        var emailIds = await StoreScope.ReadAsync(scopes, (services, ct) => services.GetRequiredService<SqlSugarUnitOfWork>().ConnectionFor(typeof(SysEmail)).Queryable<SysEmail>()
            .Where(e => !e.IsDeleted && e.Status == MessageSendStatus.Pending && SqlFunc.IsNull(e.NextAttemptAt, past) <= now)
            .OrderBy(e => e.CreatedAt).Take(BatchSize).Select(e => e.Id).ToListAsync(ct), cancellationToken).ConfigureAwait(false);
        foreach (var id in emailIds)
        {
            await SendEmailAsync(id, cancellationToken).ConfigureAwait(false);
        }

        var smsIds = await StoreScope.ReadAsync(scopes, (services, ct) => services.GetRequiredService<SqlSugarUnitOfWork>().ConnectionFor(typeof(SysSms)).Queryable<SysSms>()
            .Where(s => !s.IsDeleted && s.Status == MessageSendStatus.Pending && SqlFunc.IsNull(s.NextAttemptAt, past) <= now)
            .OrderBy(s => s.CreatedAt).Take(BatchSize).Select(s => s.Id).ToListAsync(ct), cancellationToken).ConfigureAwait(false);
        foreach (var id in smsIds)
        {
            await SendSmsAsync(id, cancellationToken).ConfigureAwait(false);
        }

        var botIds = await StoreScope.ReadAsync(scopes, (services, ct) => services.GetRequiredService<SqlSugarUnitOfWork>().ConnectionFor(typeof(SysBotMessage)).Queryable<SysBotMessage>()
            .Where(b => !b.IsDeleted && b.Status == MessageSendStatus.Pending && SqlFunc.IsNull(b.NextAttemptAt, past) <= now)
            .OrderBy(b => b.CreatedAt).Take(BatchSize).Select(b => b.Id).ToListAsync(ct), cancellationToken).ConfigureAwait(false);
        foreach (var id in botIds)
        {
            await SendBotAsync(id, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task SendEmailAsync(Guid id, CancellationToken cancellationToken)
    {
        if (!await ClaimAsync<SysEmail>(id, cancellationToken).ConfigureAwait(false))
        {
            return;
        }

        var (record, config, server) = await StoreScope.ReadAsync(scopes, async (services, ct) =>
        {
            var row = await services.GetRequiredService<SqlSugarUnitOfWork>().ConnectionFor(typeof(SysEmail)).Queryable<SysEmail>().InSingleAsync(id).ConfigureAwait(false);
            var configs = services.GetRequiredService<ChannelConfigService>();
            var config = row is null ? null : await configs.ResolveEmailAsync(row.OwnerTenantId, ct).ConfigureAwait(false);
            return (row, config, config is null ? null : configs.ServerOf(config));
        }, cancellationToken).ConfigureAwait(false);
        if (record is null)
        {
            return;
        }

        var result = server is null
            ? TransportResult.Failed("No enabled default email configuration on this side or the platform.")
            : await emailTransport.SendAsync(server, new EmailEnvelope(record.ToEmail, record.Subject, record.Content, record.IsHtml), cancellationToken).ConfigureAwait(false);
        await FinishAsync<SysEmail>(record, config?.Id, result, provider: null, cancellationToken).ConfigureAwait(false);
    }

    private async Task SendSmsAsync(Guid id, CancellationToken cancellationToken)
    {
        if (!await ClaimAsync<SysSms>(id, cancellationToken).ConfigureAwait(false))
        {
            return;
        }

        var (record, config, account) = await StoreScope.ReadAsync(scopes, async (services, ct) =>
        {
            var row = await services.GetRequiredService<SqlSugarUnitOfWork>().ConnectionFor(typeof(SysSms)).Queryable<SysSms>().InSingleAsync(id).ConfigureAwait(false);
            var configs = services.GetRequiredService<ChannelConfigService>();
            var config = row is null ? null : await configs.ResolveSmsAsync(row.OwnerTenantId, ct).ConfigureAwait(false);
            return (row, config, config is null ? null : configs.AccountOf(config));
        }, cancellationToken).ConfigureAwait(false);
        if (record is null)
        {
            return;
        }

        TransportResult result;
        var providerTemplate = config is null || record.TemplateCode is null ? null : ChannelConfigService.ProviderTemplateOf(config, record.TemplateCode);
        var transport = config is null ? null : _sms.FirstOrDefault(t => t.Provider == config.Provider);
        if (config is null || account is null)
        {
            result = TransportResult.Failed("No usable default SMS configuration on this side or the platform.");
        }
        else if (providerTemplate is null)
        {
            result = TransportResult.Failed($"Template '{record.TemplateCode}' is not mapped to a provider template in SMS configuration '{config.Code}'.");
        }
        else if (transport is null)
        {
            result = TransportResult.Failed($"No transport for SMS provider '{config.Provider}'.");
        }
        else
        {
            var parameters = JsonNode.Parse(record.TemplateParams ?? "{}") is JsonObject json
                ? json.Select(static p => new KeyValuePair<string, string?>(p.Key, p.Value?.ToString())).ToList()
                : [];
            result = await transport.SendAsync(account, new SmsEnvelope(record.ToPhone, providerTemplate, parameters), cancellationToken).ConfigureAwait(false);
        }

        await FinishAsync<SysSms>(record, config?.Id, result, config?.Provider, cancellationToken).ConfigureAwait(false);
    }

    private async Task SendBotAsync(Guid id, CancellationToken cancellationToken)
    {
        if (!await ClaimAsync<SysBotMessage>(id, cancellationToken).ConfigureAwait(false))
        {
            return;
        }

        var (record, endpoint) = await StoreScope.ReadAsync(scopes, async (services, ct) =>
        {
            var row = await services.GetRequiredService<SqlSugarUnitOfWork>().ConnectionFor(typeof(SysBotMessage)).Queryable<SysBotMessage>().InSingleAsync(id).ConfigureAwait(false);
            if (row?.ConfigId is not { } configId)
            {
                return (row, (BotEndpoint?)null);
            }

            if (row.Channel == BotChannel.Telegram)
            {
                var telegram = services.GetRequiredService<TelegramBotService>();
                var bot = await telegram.FindAsync(configId, ct).ConfigureAwait(false);
                return (row, bot.TryGetValue(out var b) && b.IsEnabled && row.ChatId is { } chat ? telegram.EndpointOf(b, chat) : null);
            }

            var configs = services.GetRequiredService<BotConfigService>();
            var found = await configs.FindAsync(row.OwnerTenantId, configId, ct).ConfigureAwait(false);
            return (row, found.TryGetValue(out var config) && config.IsEnabled ? configs.EndpointOf(config) : null);
        }, cancellationToken).ConfigureAwait(false);
        if (record is null)
        {
            return;
        }

        var transport = _bots.FirstOrDefault(t => t.Channel == record.Channel);
        var result = endpoint is null
            ? TransportResult.Failed("The bot configuration was disabled or deleted.")
            : transport is null
                ? TransportResult.Failed($"No transport for bot channel '{record.Channel}'.")
                : await transport.SendAsync(endpoint, record.Content, cancellationToken).ConfigureAwait(false);
        await FinishAsync<SysBotMessage>(record, record.ConfigId, result, provider: null, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// 排队中 → 发送中；改到即领到。
    /// </summary>
    private async Task<bool> ClaimAsync<T>(Guid id, CancellationToken cancellationToken)
        where T : class, IMessageRecord, new()
    {
        var claimed = 0;
        var now = clock.GetUtcNow();
        await StoreScope.WriteAsync(scopes, async (services, ct) =>
        {
            var unitOfWork = services.GetRequiredService<SqlSugarUnitOfWork>();
            await unitOfWork.EnsureTransactionAsync(ct).ConfigureAwait(false);
            var connection = unitOfWork.ConnectionFor(typeof(T));
            claimed = typeof(T) == typeof(SysEmail)
                ? await connection.Updateable<SysEmail>().SetColumns(e => new SysEmail { Status = MessageSendStatus.Sending, ModifiedAt = now }).Where(e => e.Id == id && e.Status == MessageSendStatus.Pending).ExecuteCommandAsync(ct).ConfigureAwait(false)
                : typeof(T) == typeof(SysSms)
                    ? await connection.Updateable<SysSms>().SetColumns(s => new SysSms { Status = MessageSendStatus.Sending, ModifiedAt = now }).Where(s => s.Id == id && s.Status == MessageSendStatus.Pending).ExecuteCommandAsync(ct).ConfigureAwait(false)
                    : await connection.Updateable<SysBotMessage>().SetColumns(b => new SysBotMessage { Status = MessageSendStatus.Sending, ModifiedAt = now }).Where(b => b.Id == id && b.Status == MessageSendStatus.Pending).ExecuteCommandAsync(ct).ConfigureAwait(false);
            return Result.Success();
        }, cancellationToken).ConfigureAwait(false);
        return claimed == 1;
    }

    private async Task FinishAsync<T>(T record, Guid? configId, TransportResult result, SmsProvider? provider, CancellationToken cancellationToken)
        where T : class, IMessageRecord, new()
    {
        var now = clock.GetUtcNow();
        var attempts = record.Attempts + 1;
        var status = result.Succeeded ? MessageSendStatus.Success : attempts >= record.MaxAttempts ? MessageSendStatus.Failed : MessageSendStatus.Pending;
        DateTimeOffset? next = status == MessageSendStatus.Pending ? now + (options.RetryDelay * attempts) : null;
        DateTimeOffset? sentAt = result.Succeeded ? now : null;
        var error = result.Succeeded ? null : Clip(result.Error ?? "unknown", 2000);
        var id = record.Id;
        if (!result.Succeeded)
        {
            LogSendFailed(logger, typeof(T).Name, id, attempts, error);
        }

        await StoreScope.WriteAsync(scopes, async (services, ct) =>
        {
            var unitOfWork = services.GetRequiredService<SqlSugarUnitOfWork>();
            await unitOfWork.EnsureTransactionAsync(ct).ConfigureAwait(false);
            var connection = unitOfWork.ConnectionFor(typeof(T));
            if (typeof(T) == typeof(SysEmail))
            {
                await connection.Updateable<SysEmail>()
                    .SetColumns(e => new SysEmail { Status = status, Attempts = attempts, NextAttemptAt = next, SentAt = sentAt, ErrorMessage = error, ConfigId = configId, ModifiedAt = now })
                    .Where(e => e.Id == id).ExecuteCommandAsync(ct).ConfigureAwait(false);
            }
            else if (typeof(T) == typeof(SysBotMessage))
            {
                await connection.Updateable<SysBotMessage>()
                    .SetColumns(b => new SysBotMessage { Status = status, Attempts = attempts, NextAttemptAt = next, SentAt = sentAt, ErrorMessage = error, ModifiedAt = now })
                    .Where(b => b.Id == id).ExecuteCommandAsync(ct).ConfigureAwait(false);
            }
            else
            {
                var messageId = result.MessageId;
                await connection.Updateable<SysSms>()
                    .SetColumns(s => new SysSms { Status = status, Attempts = attempts, NextAttemptAt = next, SentAt = sentAt, ErrorMessage = error, ConfigId = configId, Provider = provider, ProviderMessageId = messageId, ModifiedAt = now })
                    .Where(s => s.Id == id).ExecuteCommandAsync(ct).ConfigureAwait(false);
            }

            return Result.Success();
        }, cancellationToken).ConfigureAwait(false);
    }

    private static string Clip(string value, int max) => value.Length <= max ? value : value[..max];

    [LoggerMessage(Level = LogLevel.Warning, Message = "A message dispatch round failed.")]
    private static partial void LogRoundFailed(ILogger logger, Exception exception);

    [LoggerMessage(Level = LogLevel.Information, Message = "{Kind} {Id} attempt {Attempt} failed: {Error}")]
    private static partial void LogSendFailed(ILogger logger, string kind, Guid id, int attempt, string? error);
}
