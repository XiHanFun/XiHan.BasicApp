// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Contracts.Realtime;
using XiHan.BasicApp.Realtime.Handlers;
using XiHan.Framework.Messaging.Abstractions.Envelopes;
using XiHan.Framework.Messaging.Abstractions.Publishing;
using XiHan.Framework.Persistence.Abstractions.UnitOfWork;

namespace XiHan.BasicApp.Realtime.Infrastructure;

/// <summary>
/// <see cref="IRealtimePush"/> 的实现（作用域）。
/// </summary>
/// <remarks>
/// 调用管道里由 <see cref="Middleware.RealtimeDispatchMiddleware"/> 开一层暂存，
/// 推送先记进当前层，调用成功提交后并入外层或发出，失败即丢弃；不在管道里立即发出。发送失败只记日志，不影响已经提交的调用。
/// 断开连接：本实例上的连接直接移出并断开；配了 Redis 时别的实例登记的连接经背板移出全部组；再经消息代理广播，由各实例断开自己的连接。
/// </remarks>
/// <param name="hubs">Hub 登记表。</param>
/// <param name="closer">移出与断开连接。</param>
/// <param name="node">本实例的标识。</param>
/// <param name="services">当前作用域（取工作单元、发布者与在线登记板）。</param>
/// <param name="logger">日志。</param>
internal sealed partial class RealtimeOutbox(RealtimeHubCatalog hubs, ConnectionCloser closer, RealtimeNode node, IServiceProvider services, ILogger<RealtimeOutbox> logger) : IRealtimePush
{
    private readonly Stack<List<Func<Task>>> _frames = new();

    /// <inheritdoc />
    public ValueTask SendAsync<T>(string hub, RealtimeAudience audience, string method, T payload, JsonTypeInfo<T> payloadInfo, CancellationToken cancellationToken)
    {
        var send = Prepare(hub, audience, method, payload, payloadInfo);
        return send is null ? ValueTask.CompletedTask : EnqueueAsync(send, method);
    }

    /// <inheritdoc />
    public async ValueTask SendNowAsync<T>(string hub, RealtimeAudience audience, string method, T payload, JsonTypeInfo<T> payloadInfo, CancellationToken cancellationToken)
    {
        if (services.GetService<IUnitOfWork>() is { State: UnitOfWorkState.Active })
        {
            throw new InvalidOperationException($"Realtime push '{method}' is sent immediately and cannot be used inside a transaction; use SendAsync so it follows the commit.");
        }

        var send = Prepare(hub, audience, method, payload, payloadInfo);
        if (send is not null)
        {
            await RunAsync(send, method).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public ValueTask DisconnectAsync(IReadOnlyCollection<Guid> sessionIds, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(sessionIds);
        if (sessionIds.Count == 0)
        {
            return ValueTask.CompletedTask;
        }

        var targets = sessionIds.Distinct().ToArray();
        return EnqueueAsync(async () =>
        {
            await closer.CloseLocalAsync(targets).ConfigureAwait(false);
            if (services.GetService<PresenceBoard>() is { } board)
            {
                // 别的实例上的连接：经背板移出全部组，之后的推送不再送到；断开由它们自己的实例收到广播后做
                var wanted = targets.ToHashSet();
                foreach (var entry in (await board.OthersAsync(CancellationToken.None).ConfigureAwait(false)).Where(e => wanted.Contains(e.SessionId)))
                {
                    await closer.RemoveFromGroupsAsync(entry.Hub, entry.ConnectionId, [.. RealtimeGroups.Of(entry.SessionId, entry.UserId, entry.Side), .. entry.Groups]).ConfigureAwait(false);
                }
            }

            var envelope = new MessageEnvelope
            {
                Id = Guid.NewGuid().ToString("N"),
                Source = DisconnectHandler.SourceOf(node),
                Type = DisconnectHandler.Topic,
                Data = JsonSerializer.SerializeToUtf8Bytes(new DisconnectRequest(targets), RealtimeJsonContext.Default.DisconnectRequest),
            };
            var published = await services.GetRequiredService<IMessagePublisher>().PublishAsync(DisconnectHandler.Topic, envelope, CancellationToken.None).ConfigureAwait(false);
            if (published.IsFailure)
            {
                LogBroadcastFailed(logger, published.Error!.Code);
            }
        }, "disconnect");
    }

    /// <summary>
    /// 开一层暂存（一次调用管道）。
    /// </summary>
    public void Begin() => _frames.Push([]);

    /// <summary>
    /// 结束当前层：成功即并入外层（没有外层就发出），失败即丢弃。
    /// </summary>
    public async Task EndAsync(bool succeeded)
    {
        var frame = _frames.Pop();
        if (!succeeded || frame.Count == 0)
        {
            return;
        }

        if (_frames.TryPeek(out var outer))
        {
            outer.AddRange(frame);
            return;
        }

        foreach (var send in frame)
        {
            await RunAsync(send, "deferred").ConfigureAwait(false);
        }
    }

    private Func<Task>? Prepare<T>(string hub, RealtimeAudience audience, string method, T payload, JsonTypeInfo<T> payloadInfo)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(hub);
        ArgumentNullException.ThrowIfNull(audience);
        ArgumentException.ThrowIfNullOrWhiteSpace(method);
        ArgumentNullException.ThrowIfNull(payloadInfo);
        // 没登记的 Hub 在调用里就抛出，而不是等到提交之后
        var context = hubs.ContextOf(hub, services);
        if (audience.IsEmpty)
        {
            return null;
        }

        // 记下时就序列化：载荷是调用此刻的状态，不随之后的修改变化
        var element = JsonSerializer.SerializeToElement(payload, payloadInfo);
        var clients = ClientsOf(context.Clients, audience);
        return () => clients.SendAsync(method, element, CancellationToken.None);
    }

    private static IClientProxy ClientsOf(IHubClients clients, RealtimeAudience audience) => audience.Kind switch
    {
        RealtimeAudienceKind.Sessions => clients.Groups(audience.Ids.Select(RealtimeGroups.Session).ToList()),
        RealtimeAudienceKind.UsersInSide => clients.Groups(audience.Ids.Select(id => RealtimeGroups.UserIn(audience.SideId!, id)).ToList()),
        RealtimeAudienceKind.Users => clients.Groups(audience.Ids.Select(RealtimeGroups.User).ToList()),
        RealtimeAudienceKind.Side => clients.Group(RealtimeGroups.Side(audience.SideId!)),
        _ => throw new ArgumentOutOfRangeException(nameof(audience), audience.Kind, "Unknown realtime audience."),
    };

    private async ValueTask EnqueueAsync(Func<Task> send, string what)
    {
        if (_frames.TryPeek(out var frame))
        {
            frame.Add(send);
            return;
        }

        await RunAsync(send, what).ConfigureAwait(false);
    }

    private async Task RunAsync(Func<Task> send, string what)
    {
        try
        {
            await send().ConfigureAwait(false);
        }
#pragma warning disable CA1031 // 推送尽力而为：调用已经提交，发送失败只记日志
        catch (Exception exception)
#pragma warning restore CA1031
        {
            LogSendFailed(logger, what, exception);
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Realtime push '{What}' failed.")]
    private static partial void LogSendFailed(ILogger logger, string what, Exception exception);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Broadcasting the realtime disconnect failed ({Code}); connections on other instances stay until their clients log out.")]
    private static partial void LogBroadcastFailed(ILogger logger, string code);
}
