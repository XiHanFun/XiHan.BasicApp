// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Chat.Domain.Services;
using XiHan.BasicApp.Contracts;
using XiHan.BasicApp.Realtime.Infrastructure;
using XiHan.Framework.Kernel.Hosting;
using XiHan.Framework.Kernel.Pipeline;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Chat.Infrastructure;

/// <summary>
/// 聊天 Hub（<c>/hubs/chat</c>）
/// </summary>
/// <remarks>
/// 认证、会话门禁与连接登记由 Realtime 的基类做；客户端可调进入、离开会话、正在输入与通话信令，进入与正在输入先核对是会话成员（不是即忽略并记日志）
/// 进入会话只为正在输入的组播加入会话组（经基类记进连接登记，吊销会话时一并移出）；正在输入组播给组里的其他连接、不落库，配了 Redis 时经背板跨实例
/// 通话信令（SDP、ICE 候选）只在接通了的通话里转发、转给通话里的其他人，不落库也不解析；媒体在两端之间直连或经 TURN 中继，不经服务端
/// Hub 绕过内核管道，核对成员与通话时以连接所在的一侧开一次内部调用读库（租户经 Baggage，独立库的租户读它自己的库）
/// </remarks>
/// <param name="realtime">实时服务</param>
/// <param name="app">内核应用</param>
/// <param name="scopes">服务作用域工厂</param>
/// <param name="logger">日志</param>
internal sealed partial class ChatHub(RealtimeHubServices realtime, App app, IServiceScopeFactory scopes, ILogger<ChatHub> logger) : RealtimeHub(realtime)
{
    /// <summary>
    /// 进入会话
    /// </summary>
    /// <param name="conversationId">会话 ID</param>
    public async Task JoinConversation(string conversationId)
    {
        if (!Guid.TryParse(conversationId, out var id) || Connection is not { } connection)
        {
            return;
        }

        var (isMember, _) = await CheckAsync(connection, id, withName: false).ConfigureAwait(false);
        if (!isMember)
        {
            LogNotMember(logger, connection.UserId, id);
            return;
        }

        await JoinGroupAsync(GroupOf(id)).ConfigureAwait(false);
    }

    /// <summary>
    /// 离开会话
    /// </summary>
    /// <param name="conversationId">会话 ID</param>
    public Task LeaveConversation(string conversationId)
        => Guid.TryParse(conversationId, out var id) ? LeaveGroupAsync(GroupOf(id)) : Task.CompletedTask;

    /// <summary>
    /// 正在输入
    /// </summary>
    /// <param name="conversationId">会话 ID</param>
    public async Task Typing(string conversationId)
    {
        if (!Guid.TryParse(conversationId, out var id) || Connection is not { } connection)
        {
            return;
        }

        var (isMember, userName) = await CheckAsync(connection, id, withName: true).ConfigureAwait(false);
        if (!isMember)
        {
            return;
        }

        var payload = JsonSerializer.SerializeToElement(new ChatTypingPayload(id, connection.UserId, userName), ChatJsonContext.Default.ChatTypingPayload);
        await Clients.OthersInGroup(GroupOf(id)).SendAsync(ChatRealtimeMethods.ChatTyping, payload, Context.ConnectionAborted).ConfigureAwait(false);
    }

    /// <summary>
    /// 通话信令
    /// </summary>
    /// <remarks>
    /// 转给这通电话里的其他人；不是这通接通了的电话里的人、信令为空或过长即忽略
    /// </remarks>
    /// <param name="callId">通话 ID</param>
    /// <param name="data">信令（两端之间约定的 JSON），服务端不解析</param>
    public async Task CallSignal(string callId, string data)
    {
        if (!Guid.TryParse(callId, out var id) || Connection is not { } connection || string.IsNullOrEmpty(data) || data.Length > MaxSignalLength)
        {
            return;
        }

        await using var scope = scopes.CreateAsyncScope();
        var outcome = await app.InvokeAsync(ContextOf(scope.ServiceProvider, connection), async ctx =>
        {
            var targets = await ctx.Services.GetRequiredService<ChatCallService>().SignalTargetsAsync(connection.Side, id, connection.UserId, ctx.CancellationToken).ConfigureAwait(false);
            await ctx.Services.GetRequiredService<ChatPush>().CallSignalAsync(connection.Side, new ChatCallSignalPayload(id, connection.UserId, data), targets, ctx.CancellationToken).ConfigureAwait(false);
            ctx.SetOutcome(Result.Success());
        }).ConfigureAwait(false);
        if (outcome.IsFailure)
        {
            LogSignalFailed(logger, connection.UserId, id, outcome.Error!.Code);
        }
    }

    /// <summary>
    /// 会话组名
    /// </summary>
    internal static string GroupOf(Guid conversationId) => $"chat:c:{conversationId:N}";

    /// <summary>
    /// 一条信令的长度上限
    /// </summary>
    /// <remarks>
    /// 带音视频的 SDP 在几千字符，留出余量；整条消息另受 Hub 的接收上限约束
    /// </remarks>
    private const int MaxSignalLength = 20000;

    /// <summary>
    /// 以连接所在的一侧开一次内部调用的上下文
    /// </summary>
    private InvocationContext ContextOf(IServiceProvider services, RealtimeConnection connection)
    {
        var baggage = string.Equals(connection.Side, TenantIds.Platform, StringComparison.Ordinal)
            ? null
            : new Dictionary<string, string>(StringComparer.Ordinal) { [BaggageKeys.Tenant] = connection.Side };
        return App.CreateContext(services, new InvocationContextOptions { Baggage = baggage }, Context.ConnectionAborted);
    }

    private async Task<(bool IsMember, string? UserName)> CheckAsync(RealtimeConnection connection, Guid conversationId, bool withName)
    {
        var isMember = false;
        string? userName = null;
        await using var scope = scopes.CreateAsyncScope();
        var outcome = await app.InvokeAsync(ContextOf(scope.ServiceProvider, connection), async ctx =>
        {
            var store = ctx.Services.GetRequiredService<ChatStore>();
            isMember = await store.MemberAsync(connection.Side, conversationId, connection.UserId, ctx.CancellationToken).ConfigureAwait(false) is not null;
            if (isMember && withName)
            {
                userName = (await store.UsersAsync([connection.UserId], ctx.CancellationToken).ConfigureAwait(false)).GetValueOrDefault(connection.UserId)?.UserName;
            }

            ctx.SetOutcome(Result.Success());
        }).ConfigureAwait(false);
        return (outcome.IsSuccess && isMember, userName);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "User {UserId} tried to join chat conversation {ConversationId} without being a member.")]
    private static partial void LogNotMember(ILogger logger, Guid userId, Guid conversationId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Relaying a signal from user {UserId} for chat call {CallId} failed ({Code}).")]
    private static partial void LogSignalFailed(ILogger logger, Guid userId, Guid callId, string code);
}
