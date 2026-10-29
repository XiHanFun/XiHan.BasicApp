// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Notification.Domain.Entities;
using XiHan.BasicApp.Notification.Domain.Services;
using XiHan.BasicApp.Runtime.Security;
using XiHan.Framework.Domain.Specifications;
using XiHan.Framework.Kernel.Hosting;
using XiHan.Framework.Kernel.Pipeline;
using XiHan.Framework.Persistence.Abstractions.Queries;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Notification.Infrastructure.Telegram;

/// <summary>
/// Telegram 机器人的托管（4.x TelegramBotManager 同一套）：每个刷新周期在平台调用里读设置与启用的机器人，对照运行中的：新增的启动、配置变了的重启、
/// 换了令牌的先删旧 Webhook、删除或停用的删 Webhook 后移除；Webhook 根地址、密钥或网络变了即全部重建。启动 = <c>getMe</c> 回填用户名，
/// Webhook 模式 <c>setWebhook</c>（带密钥与订阅种类；没配密钥即不注册），长轮询模式先 <c>deleteWebhook</c> 再循环 <c>getUpdates</c>，再同步命令菜单。
/// 收到的 Update 交给 <see cref="TelegramDispatcher"/>，全部机器人共用一个并发上限。
/// 与 4.x 不同：总开关关掉时删各机器人的 Webhook（4.x 不删，Telegram 继续推送、Update 丢失）；Webhook 也声明订阅种类（4.x 只轮询声明）。
/// </summary>
/// <param name="scopes">作用域工厂。</param>
/// <param name="app">内核。</param>
/// <param name="runtime">当前设置与机器人。</param>
/// <param name="notifier">出站。</param>
/// <param name="dispatcher">分发。</param>
/// <param name="options">选项。</param>
/// <param name="clock">时钟。</param>
/// <param name="logger">日志。</param>
public sealed partial class TelegramBotManager(IServiceScopeFactory scopes, App app, TelegramRuntime runtime, TelegramNotifier notifier, TelegramDispatcher dispatcher, TelegramOptions options, TimeProvider clock, ILogger<TelegramBotManager> logger) : IDisposable
{
    private static readonly TimeSpan _initialRetry = TimeSpan.FromSeconds(30);

    private readonly SemaphoreSlim _concurrency = new(Math.Max(1, options.MaxDispatchConcurrency));
    private readonly ConcurrentDictionary<Task, byte> _inflight = new();
    private readonly Dictionary<string, Running> _running = new(StringComparer.OrdinalIgnoreCase);
    private readonly SemaphoreSlim _refreshing = new(1, 1);
    private TelegramSettings _settings = TelegramSettings.Default;
    private CancellationToken _stopping;

    /// <summary>
    /// 循环到停止：每个周期刷新一次；第一次加载失败且没有刷新周期时 30 秒后重试（4.x 同）。
    /// </summary>
    /// <param name="stopping">停止信号。</param>
    /// <returns>循环。</returns>
    public async Task RunAsync(CancellationToken stopping)
    {
        _stopping = stopping;
        var loaded = false;

        // 读设置要开平台调用：等内核开始服务（各包都启动完）再读
        while (!app.State.IsServing() && !stopping.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromMilliseconds(200), clock, stopping).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }

        while (!stopping.IsCancellationRequested)
        {
            try
            {
                loaded = await RefreshAsync(stopping).ConfigureAwait(false) || loaded;
                var seconds = _settings.ManagerRefreshSeconds;
                if (seconds <= 0 && loaded)
                {
                    await Task.Delay(Timeout.Infinite, stopping).ConfigureAwait(false);
                }

                await Task.Delay(seconds > 0 ? TimeSpan.FromSeconds(seconds) : _initialRetry, clock, stopping).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stopping.IsCancellationRequested)
            {
                break;
            }
#pragma warning disable CA1031 // 一轮出错（库暂时连不上等）不结束循环
            catch (Exception exception)
#pragma warning restore CA1031
            {
                LogRefreshFailed(logger, exception);
                await Task.Delay(_initialRetry, clock, stopping).ConfigureAwait(false);
            }
        }

        await StopAllAsync(deleteWebhooks: false).ConfigureAwait(false);
    }

    /// <summary>
    /// 立即刷新一次（测试与运维用）。
    /// </summary>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>是否读到了设置。</returns>
    public async Task<bool> RefreshAsync(CancellationToken cancellationToken)
    {
        await _refreshing.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var loaded = await LoadAsync(cancellationToken).ConfigureAwait(false);
            if (loaded is not { } snapshot)
            {
                return false;
            }

            var (settings, bots) = snapshot;
            var previous = _settings;
            _settings = settings;
            if (!settings.Enabled)
            {
                await StopAllAsync(deleteWebhooks: previous.Enabled && previous.UsesWebhook).ConfigureAwait(false);
                runtime.Publish(settings, [], running: false);
                return true;
            }

            // 先换上新设置（网络与密钥），再按差异启停
            runtime.Publish(settings, _running.Values.Where(static r => !r.Stopped).Select(static r => r.Bot), running: true);
            var transportChanged = previous.Enabled && (!string.Equals(previous.WebhookBaseUrl, settings.WebhookBaseUrl, StringComparison.Ordinal) || !string.Equals(previous.SecretToken, settings.SecretToken, StringComparison.Ordinal) || previous.Network != settings.Network);
            if (transportChanged)
            {
                await StopAllAsync(deleteWebhooks: false).ConfigureAwait(false);
            }

            var desired = bots.ToDictionary(static b => b.Name, StringComparer.OrdinalIgnoreCase);
            foreach (var name in _running.Keys.Where(n => !desired.ContainsKey(n)).ToList())
            {
                await StopAsync(name, deleteWebhook: true).ConfigureAwait(false);
            }

            foreach (var bot in bots)
            {
                if (_running.TryGetValue(bot.Name, out var current) && !current.Stopped && current.Bot.SameConfigAs(bot))
                {
                    continue;
                }

                if (current is not null)
                {
                    await StopAsync(bot.Name, deleteWebhook: !string.Equals(current.Bot.Token, bot.Token, StringComparison.Ordinal)).ConfigureAwait(false);
                }

                await StartAsync(bot, settings, cancellationToken).ConfigureAwait(false);
            }

            runtime.Publish(settings, _running.Values.Where(static r => !r.Stopped).Select(static r => r.Bot), running: true);
            return true;
        }
        finally
        {
            _refreshing.Release();
        }
    }

    /// <summary>
    /// 把一条 Update 放到后台处理（没在运行即丢弃，4.x 同）；占满并发上限时排队等。
    /// </summary>
    /// <param name="bot">机器人。</param>
    /// <param name="update">Update。</param>
    public void QueueDispatch(TelegramBot bot, TelegramUpdate update)
    {
        ArgumentNullException.ThrowIfNull(bot);
        ArgumentNullException.ThrowIfNull(update);
        if (!runtime.IsRunning || _stopping.IsCancellationRequested)
        {
            return;
        }

        var stopping = _stopping;
        Task? task = null;
        task = Task.Run(async () =>
        {
            await _concurrency.WaitAsync(stopping).ConfigureAwait(false);
            try
            {
                await dispatcher.DispatchAsync(bot, update, stopping).ConfigureAwait(false);
            }
#pragma warning disable CA1031 // 后台处理的异常只记日志
            catch (Exception exception) when (exception is not OperationCanceledException)
#pragma warning restore CA1031
            {
                LogDispatchFailed(logger, exception, bot.Name, update.UpdateId);
            }
            finally
            {
                _concurrency.Release();
            }
        }, CancellationToken.None);
        _inflight.TryAdd(task, 0);
        _ = task.ContinueWith(t => _inflight.TryRemove(t, out _), CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
    }

    /// <summary>
    /// 等在处理的 Update 处理完（测试用）。
    /// </summary>
    /// <returns>等待。</returns>
    public async Task DrainAsync()
    {
        while (!_inflight.IsEmpty)
        {
            await Task.WhenAll(_inflight.Keys.ToList()).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        foreach (var running in _running.Values)
        {
            running.Polling?.Dispose();
        }

        _concurrency.Dispose();
        _refreshing.Dispose();
    }

    /// <summary>
    /// 规整一个机器人（令牌解不开、名称或令牌为空即跳过）。
    /// </summary>
    private static TelegramBot? Normalize(SysTelegramBot row, ISecretProtector secrets)
    {
        var name = row.Name.Trim();
        if (name.Length == 0 || !secrets.TryUnprotect(TelegramBotService.TokenPurpose, row.Token, out var token) || string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        var admins = TelegramBotService.Split(row.AdminUsers).Select(static s => long.TryParse(s, out var v) ? v : 0).Where(static v => v > 0).Distinct().ToList();
        var groups = TelegramBotService.Split(row.AllowedGroupChatIds).Select(static s => long.TryParse(s, out var v) ? v : 0).Where(static v => v != 0).Distinct().ToList();
        var commands = TelegramBotService.Split(row.AllowedCommands).Select(TelegramCommands.Normalize).OfType<string>().Distinct(StringComparer.Ordinal).ToList();
        return new TelegramBot(row.Id, name, token.Trim(), admins, groups, commands, row.EnableFallbackReply);
    }

    private async Task<(TelegramSettings Settings, IReadOnlyList<TelegramBot> Bots)?> LoadAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopes.CreateAsyncScope();
        var context = App.CreateContext(scope.ServiceProvider, cancellationToken);
        (TelegramSettings, IReadOnlyList<TelegramBot>)? loaded = null;
        var outcome = await app.InvokeAsync(context, async invocation =>
        {
            var settings = await TelegramSettings.LoadAsync(invocation).ConfigureAwait(false);
            if (!settings.TryGetValue(out var value))
            {
                invocation.SetOutcome(settings.Error!);
                return;
            }

            IReadOnlyList<TelegramBot> bots = [];
            if (value.Enabled)
            {
                var rows = await invocation.Services.GetRequiredService<IRepository<SysTelegramBot, Guid>>()
                    .ListAsync(EntityQuery.All<SysTelegramBot>().Where(new Specification<SysTelegramBot>(static b => b.IsEnabled)), invocation.CancellationToken).ConfigureAwait(false);
                var secrets = invocation.Services.GetRequiredService<ISecretProtector>();

                // 重名（不分大小写）只留第一个（4.x 同）
                bots = rows.OrderBy(static b => b.Order).ThenBy(static b => b.CreatedAt)
                    .Select(r => Normalize(r, secrets)).OfType<TelegramBot>()
                    .DistinctBy(static b => b.Name, StringComparer.OrdinalIgnoreCase).ToList();
            }

            loaded = (value, bots);
            invocation.SetOutcome(Result.Success());
        }).ConfigureAwait(false);
        if (outcome.IsFailure)
        {
            LogLoadFailed(logger, outcome.Error!.Code, outcome.Error.Message);
            return null;
        }

        return loaded;
    }

    private async Task StartAsync(TelegramBot bot, TelegramSettings settings, CancellationToken cancellationToken)
    {
        var me = await notifier.CallAsync(bot, "getMe", new JsonObject(), cancellationToken).ConfigureAwait(false);
        if (!me.Ok)
        {
            LogStartFailed(logger, bot.Name, "getMe", me.ErrorCode, me.Description);
            _running[bot.Name] = new Running(bot, null, null) { Stopped = true };
            return;
        }

        var started = bot with
        {
            BotUserId = me.Result.TryGetProperty("id", out var id) && id.TryGetInt64(out var userId) ? userId : 0,
            Username = me.Result.TryGetProperty("username", out var username) ? username.GetString()?.TrimStart('@') : null,
        };
        CancellationTokenSource? polling = null;
        Task? loop = null;
        var allowed = new JsonArray([.. TelegramUpdate.AllowedUpdates.Select(static u => (JsonNode)u)]);

        // 先让 Webhook 端点与轮询认得这个机器人，再注册 Webhook / 开始拉取（否则头几条 Update 会因找不到机器人被丢掉）
        runtime.Publish(settings, _running.Values.Where(r => !r.Stopped && !string.Equals(r.Bot.Name, bot.Name, StringComparison.OrdinalIgnoreCase)).Select(static r => r.Bot).Append(started), running: true);
        if (settings.UsesWebhook)
        {
            if (string.IsNullOrEmpty(settings.SecretToken))
            {
                // fail-closed：没配密钥不注册 Webhook（4.x 同）
                LogSecretMissing(logger, bot.Name);
            }
            else
            {
                var set = await notifier.CallAsync(started, "setWebhook", new JsonObject { ["url"] = settings.WebhookUrlOf(bot.Name), ["secret_token"] = settings.SecretToken, ["allowed_updates"] = allowed }, cancellationToken).ConfigureAwait(false);
                if (!set.Ok)
                {
                    LogStartFailed(logger, bot.Name, "setWebhook", set.ErrorCode, set.Description);
                }
            }
        }
        else
        {
            await notifier.CallAsync(started, "deleteWebhook", new JsonObject(), cancellationToken).ConfigureAwait(false);
            polling = CancellationTokenSource.CreateLinkedTokenSource(_stopping);
            loop = PollAsync(started, polling.Token);
        }

        await SyncMenuAsync(started, cancellationToken).ConfigureAwait(false);
        _running[bot.Name] = new Running(started, polling, loop);
    }

    private async Task SyncMenuAsync(TelegramBot bot, CancellationToken cancellationToken)
    {
        await using var scope = scopes.CreateAsyncScope();
        var services = scope.ServiceProvider;
        var catalog = new TelegramCatalog(
            services.GetServices<ITelegramCommandHandler>(),
            services.GetServices<ITelegramCallbackHandler>(),
            services.GetServices<ITelegramInlineQueryHandler>(),
            services.GetServices<ITelegramStartPayloadHandler>(),
            services.GetServices<ITelegramReplyHandler>(),
            services.GetServices<ITelegramMessageHandler>(),
            services.GetServices<ITelegramStateHandler>());
        foreach (var (scopeType, preferAlias) in new[] { ("default", false), ("all_group_chats", true) })
        {
            var menu = catalog.Menu(bot, preferAlias);
            var scopeNode = new JsonObject { ["type"] = scopeType };
            var result = menu.Count == 0
                ? await notifier.CallAsync(bot, "deleteMyCommands", new JsonObject { ["scope"] = scopeNode }, cancellationToken).ConfigureAwait(false)
                : await notifier.CallAsync(bot, "setMyCommands", new JsonObject { ["commands"] = menu, ["scope"] = scopeNode }, cancellationToken).ConfigureAwait(false);
            if (!result.Ok)
            {
                LogStartFailed(logger, bot.Name, "setMyCommands", result.ErrorCode, result.Description);
            }
        }
    }

    private async Task PollAsync(TelegramBot bot, CancellationToken cancellationToken)
    {
        long offset = 0;
        var allowed = new JsonArray([.. TelegramUpdate.AllowedUpdates.Select(static u => (JsonNode)u)]);
        while (!cancellationToken.IsCancellationRequested)
        {
            var payload = new JsonObject { ["timeout"] = options.PollingTimeoutSeconds, ["allowed_updates"] = allowed.DeepClone() };
            if (offset > 0)
            {
                payload["offset"] = offset;
            }

            var result = await notifier.CallAsync(bot, "getUpdates", payload, cancellationToken).ConfigureAwait(false);
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            if (!result.Ok)
            {
                if (result.ErrorCode == 409)
                {
                    // 别的实例在轮询或设了 Webhook：停掉本实例的轮询，下个周期当作已停止再启动（4.x 同）
                    LogPollingConflict(logger, bot.Name);
                    if (_running.TryGetValue(bot.Name, out var running))
                    {
                        running.Stopped = true;
                    }

                    return;
                }

                await Task.Delay(TimeSpan.FromSeconds(5), clock, cancellationToken).ConfigureAwait(false);
                continue;
            }

            if (result.Result.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var element in result.Result.EnumerateArray())
            {
                if (TelegramUpdate.Parse(element) is { } update)
                {
                    offset = Math.Max(offset, update.UpdateId + 1);
                    if (runtime.Find(bot.Name) is { } current)
                    {
                        QueueDispatch(current, update);
                    }
                }
            }
        }
    }

    private async Task StopAsync(string name, bool deleteWebhook)
    {
        if (!_running.Remove(name, out var running))
        {
            return;
        }

        if (running.Polling is { } polling)
        {
            await polling.CancelAsync().ConfigureAwait(false);
            if (running.Loop is { } loop)
            {
                try
                {
                    await loop.ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    // 停止
                }
            }

            polling.Dispose();
        }
        else if (deleteWebhook)
        {
            await notifier.CallAsync(running.Bot, "deleteWebhook", new JsonObject(), CancellationToken.None).ConfigureAwait(false);
        }
    }

    private async Task StopAllAsync(bool deleteWebhooks)
    {
        foreach (var name in _running.Keys.ToList())
        {
            await StopAsync(name, deleteWebhooks).ConfigureAwait(false);
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Refreshing the Telegram bots failed.")]
    private static partial void LogRefreshFailed(ILogger logger, Exception exception);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Loading the Telegram settings failed: {Code} {Message}")]
    private static partial void LogLoadFailed(ILogger logger, string code, string message);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Starting Telegram bot {Bot} failed at {Method}: {Code} {Description}")]
    private static partial void LogStartFailed(ILogger logger, string bot, string method, int? code, string? description);

    [LoggerMessage(Level = LogLevel.Error, Message = "Telegram bot {Bot} uses a webhook but no secret token is configured; the webhook is not registered.")]
    private static partial void LogSecretMissing(ILogger logger, string bot);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Telegram bot {Bot} long polling conflicts with another consumer (409); stopped until the next refresh.")]
    private static partial void LogPollingConflict(ILogger logger, string bot);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Dispatching update {UpdateId} of Telegram bot {Bot} failed.")]
    private static partial void LogDispatchFailed(ILogger logger, Exception exception, string bot, long updateId);

    private sealed class Running(TelegramBot bot, CancellationTokenSource? polling, Task? loop)
    {
        public TelegramBot Bot { get; } = bot;

        public CancellationTokenSource? Polling { get; } = polling;

        public Task? Loop { get; } = loop;

        public bool Stopped { get; set; }
    }
}
