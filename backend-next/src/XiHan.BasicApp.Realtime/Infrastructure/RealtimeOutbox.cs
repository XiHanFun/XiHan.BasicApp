// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Contracts.Realtime;

namespace XiHan.BasicApp.Realtime.Infrastructure;

/// <summary>
/// <see cref="IRealtimePush"/> 的实现（作用域）：调用管道里由 <see cref="Middleware.RealtimeDispatchMiddleware"/> 开一层暂存，
/// 推送先记进当前层，调用成功提交后并入外层或发出，失败即丢弃；不在管道里立即发出。发送失败只记日志，不影响已经提交的调用。
/// </summary>
/// <param name="hub">通知 Hub 的上下文。</param>
/// <param name="registry">连接登记（按会话断开）。</param>
/// <param name="logger">日志。</param>
internal sealed partial class RealtimeOutbox(IHubContext<NotificationHub> hub, ConnectionRegistry registry, ILogger<RealtimeOutbox> logger) : IRealtimePush
{
    /// <summary>
    /// 断开前的宽限：先发出的 ForceLogout 还在连接的发送缓冲里（长轮询要等下一次轮询取走），立即断开会把它丢掉。
    /// </summary>
    private static readonly TimeSpan _disconnectGrace = TimeSpan.FromSeconds(2);

    private readonly Stack<List<Func<Task>>> _frames = new();

    /// <inheritdoc />
    public ValueTask SendAsync<T>(RealtimeAudience audience, string method, T payload, JsonTypeInfo<T> payloadInfo, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(audience);
        ArgumentException.ThrowIfNullOrWhiteSpace(method);
        ArgumentNullException.ThrowIfNull(payloadInfo);
        if (audience.IsEmpty)
        {
            return ValueTask.CompletedTask;
        }

        // 记下时就序列化：载荷是调用此刻的状态，不随之后的修改变化
        var element = JsonSerializer.SerializeToElement(payload, payloadInfo);
        var clients = ClientsOf(audience);
        return EnqueueAsync(() => clients.SendAsync(method, element, CancellationToken.None), method);
    }

    /// <inheritdoc />
    public ValueTask DisconnectAsync(IReadOnlyCollection<Guid> sessionIds, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(sessionIds);
        if (sessionIds.Count == 0)
        {
            return ValueTask.CompletedTask;
        }

        var targets = sessionIds.ToArray();
        return EnqueueAsync(async () =>
        {
            var connections = registry.OfSessions(targets);
            // 先移出全部组：之后的推送不再送到这些连接；宽限之后再断开
            foreach (var connection in connections)
            {
                foreach (var group in RealtimeGroups.Of(connection.SessionId, connection.UserId, connection.Side))
                {
                    await hub.Groups.RemoveFromGroupAsync(connection.ConnectionId, group, CancellationToken.None).ConfigureAwait(false);
                }
            }

            if (connections.Count > 0)
            {
                _ = AbortLaterAsync(connections);
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

    private static async Task AbortLaterAsync(IReadOnlyList<RealtimeConnection> connections)
    {
        await Task.Delay(_disconnectGrace).ConfigureAwait(false);
        foreach (var connection in connections)
        {
            connection.Caller.Abort();
        }
    }

    private IClientProxy ClientsOf(RealtimeAudience audience) => audience.Kind switch
    {
        RealtimeAudienceKind.Sessions => hub.Clients.Groups(audience.Ids.Select(RealtimeGroups.Session).ToList()),
        RealtimeAudienceKind.UsersInSide => hub.Clients.Groups(audience.Ids.Select(id => RealtimeGroups.UserIn(audience.SideId!, id)).ToList()),
        RealtimeAudienceKind.Users => hub.Clients.Groups(audience.Ids.Select(RealtimeGroups.User).ToList()),
        RealtimeAudienceKind.Side => hub.Clients.Group(RealtimeGroups.Side(audience.SideId!)),
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
}
