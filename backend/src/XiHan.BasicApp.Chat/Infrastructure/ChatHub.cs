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
/// 认证、会话门禁与连接登记由 Realtime 的基类做；客户端可调进入、离开会话与正在输入，进入与正在输入先核对是会话成员（不是即忽略并记日志）
/// 进入会话只为正在输入的组播加入会话组（经基类记进连接登记，吊销会话时一并移出）；正在输入组播给组里的其他连接、不落库，配了 Redis 时经背板跨实例
/// Hub 绕过内核管道，核对成员时以连接所在的一侧开一次内部调用读库（租户经 Baggage，独立库的租户读它自己的库）
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
    /// 会话组名
    /// </summary>
    internal static string GroupOf(Guid conversationId) => $"chat:c:{conversationId:N}";

    private async Task<(bool IsMember, string? UserName)> CheckAsync(RealtimeConnection connection, Guid conversationId, bool withName)
    {
        var isMember = false;
        string? userName = null;
        await using var scope = scopes.CreateAsyncScope();
        var baggage = string.Equals(connection.Side, TenantIds.Platform, StringComparison.Ordinal)
            ? null
            : new Dictionary<string, string>(StringComparer.Ordinal) { [BaggageKeys.Tenant] = connection.Side };
        var context = App.CreateContext(scope.ServiceProvider, Context.ConnectionAborted, baggage: baggage);
        var outcome = await app.InvokeAsync(context, async ctx =>
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
}
