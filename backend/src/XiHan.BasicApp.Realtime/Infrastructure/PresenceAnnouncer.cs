// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Contracts;
using XiHan.BasicApp.Contracts.Realtime;
using XiHan.BasicApp.Realtime.Handlers;
using XiHan.Framework.Messaging.Abstractions.Envelopes;
using XiHan.Framework.Messaging.Abstractions.Publishing;

namespace XiHan.BasicApp.Realtime.Infrastructure;

/// <summary>
/// 发布用户的上线与离线（单例）
/// </summary>
/// <remarks>
/// 用户在某一侧的第一条连接到了、最后一条连接断了，各发一条 <see cref="Topics.RealtimePresenceChanged"/>（信封带侧），订阅的包据此把在线状态推给相关的人
/// 离线留一段宽限（<see cref="RealtimePresenceOptions.OfflineGrace"/>）：宽限之内又连上的（刷新页面、断线重连）两头都不发，别人看到的一直是在线
/// 只看本实例的连接：同一个人还连着别的实例时这里照样发，消息不带「上线还是离线」、订阅方读当时的状态，多发一条也不会让状态出错
/// 发布尽力而为：失败只记日志，各处读到的在线状态以 <see cref="IRealtimePresence"/> 为准；停机时还在宽限里的离线不再发
/// </remarks>
/// <param name="options">发布参数</param>
/// <param name="node">本实例的标识</param>
/// <param name="scopes">服务作用域工厂（取发布者）</param>
/// <param name="clock">时间源</param>
/// <param name="logger">日志</param>
internal sealed partial class PresenceAnnouncer(RealtimePresenceOptions options, RealtimeNode node, IServiceScopeFactory scopes, TimeProvider clock, ILogger<PresenceAnnouncer> logger)
{
    // 还在宽限里的离线：值是那一次离开的票号，回来即拿掉，宽限到了还在且票号没变才发
    private readonly ConcurrentDictionary<(string Side, Guid UserId), long> _leaving = new();
    private long _tickets;

    /// <summary>
    /// 用户在这一侧的第一条连接到了
    /// </summary>
    public Task ArrivedAsync(string side, Guid userId)
        => _leaving.TryRemove((side, userId), out _) ? Task.CompletedTask : PublishAsync(side, userId);

    /// <summary>
    /// 用户在这一侧的最后一条连接断了
    /// </summary>
    public void Left(string side, Guid userId)
    {
        var ticket = Interlocked.Increment(ref _tickets);
        _leaving[(side, userId)] = ticket;
        _ = ConfirmAsync(side, userId, ticket);
    }

    private async Task ConfirmAsync(string side, Guid userId, long ticket)
    {
        await Task.Delay(options.OfflineGrace, clock).ConfigureAwait(false);
        if (_leaving.TryRemove(new KeyValuePair<(string Side, Guid UserId), long>((side, userId), ticket)))
        {
            await PublishAsync(side, userId).ConfigureAwait(false);
        }
    }

    private async Task PublishAsync(string side, Guid userId)
    {
        try
        {
            var envelope = new MessageEnvelope
            {
                Id = Guid.NewGuid().ToString("N"),
                Source = DisconnectHandler.SourceOf(node),
                Type = Topics.RealtimePresenceChanged,
                Data = JsonSerializer.SerializeToUtf8Bytes(new PresenceChanged(userId), RealtimeJsonContext.Default.PresenceChanged),
            };
            if (!string.Equals(side, TenantIds.Platform, StringComparison.Ordinal))
            {
                envelope = envelope.WithExtension(EnvelopeExtensions.Tenant, side);
            }

            await using var scope = scopes.CreateAsyncScope();
            var published = await scope.ServiceProvider.GetRequiredService<IMessagePublisher>().PublishAsync(Topics.RealtimePresenceChanged, envelope, CancellationToken.None).ConfigureAwait(false);
            if (published.IsFailure)
            {
                LogPublishFailed(logger, published.Error!.Code, null);
            }
        }
#pragma warning disable CA1031 // 上下线的发布尽力而为：它跑在连接的建立与断开上，失败不能连累连接本身
        catch (Exception exception)
#pragma warning restore CA1031
        {
            LogPublishFailed(logger, exception.GetType().Name, exception);
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Publishing the realtime presence change failed ({Reason}); readers still get the current presence.")]
    private static partial void LogPublishFailed(ILogger logger, string reason, Exception? exception);
}
