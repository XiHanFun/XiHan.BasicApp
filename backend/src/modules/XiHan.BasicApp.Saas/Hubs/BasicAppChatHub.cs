#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:BasicAppChatHub
// Guid:bf2a3b4c-5d6e-4f7a-9c3d-be4f5a6b7c8d
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2024/12/06 05:25:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Web.RealTime.Attributes;
using XiHan.Framework.Web.RealTime.Hubs;
using XiHan.Framework.Web.RealTime.Services;

namespace XiHan.BasicApp.Saas.Hubs;

/// <summary>
/// 曦寒基础应用聊天 Hub
/// </summary>
/// <remarks>
/// 仅承载轻量实时语义：进出会话组（typing 组播边界）与输入中提示（不落库）。
/// 消息发送/撤回/已读等持久化操作一律走 ChatAppService（REST），
/// 落库后经 <c>IRealtimeNotificationService&lt;BasicAppChatHub&gt;</c> 按成员 userId 点发
/// （不依赖组：断线重连丢组也不漏消息）。每次进组均校验会话成员身份。
/// </remarks>
[AuthorizeHub]
public class BasicAppChatHub : XiHanHub
{
    private readonly IChatConversationMemberRepository _memberRepository;

    private readonly ILogger<BasicAppChatHub> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="connectionManager">连接管理器</param>
    /// <param name="memberRepository">会话成员仓储</param>
    /// <param name="logger">日志</param>
    public BasicAppChatHub(
        IConnectionManager connectionManager,
        IChatConversationMemberRepository memberRepository,
        ILogger<BasicAppChatHub> logger)
        : base(connectionManager)
    {
        _memberRepository = memberRepository;
        _logger = logger;
    }

    /// <summary>
    /// 进入会话组（打开会话页时调用；成员校验通过才入组）
    /// </summary>
    /// <param name="conversationId">会话ID（字符串形态雪花 ID：JS number 超 2^53 精度丢失，客户端只能传 string）</param>
    public async Task JoinConversation(string conversationId)
    {
        if (!TryParseConversationId(conversationId, out var id) || !await IsMemberAsync(id))
        {
            _logger.LogWarning("非会话成员尝试进组：ConversationId={ConversationId}, UserId={UserId}", conversationId, UserId);
            return;
        }

        await Groups.AddToGroupAsync(ConnectionId!, ChatRealtimeMethods.ConversationGroup(id));
    }

    /// <summary>
    /// 离开会话组（离开会话页时调用）
    /// </summary>
    /// <param name="conversationId">会话ID（字符串形态雪花 ID）</param>
    public async Task LeaveConversation(string conversationId)
    {
        if (!TryParseConversationId(conversationId, out var id))
        {
            return;
        }

        await Groups.RemoveFromGroupAsync(ConnectionId!, ChatRealtimeMethods.ConversationGroup(id));
    }

    /// <summary>
    /// 输入中提示：向会话组内其他连接组播（不落库，前端节流调用）
    /// </summary>
    /// <param name="conversationId">会话ID（字符串形态雪花 ID）</param>
    public async Task Typing(string conversationId)
    {
        if (!TryParseConversationId(conversationId, out var id) || !await IsMemberAsync(id))
        {
            return;
        }

        await Clients.OthersInGroup(ChatRealtimeMethods.ConversationGroup(id))
            .SendAsync(ChatRealtimeMethods.ChatTyping, new { conversationId, userId = UserId, userName = UserName });
    }

    /// <summary>
    /// 解析客户端上送的会话ID（fail-closed：非纯数字或非正数一律拒绝，组名不接受任意字符串）
    /// </summary>
    private static bool TryParseConversationId(string? conversationId, out long id)
    {
        return long.TryParse(conversationId, out id) && id > 0;
    }

    /// <summary>
    /// 当前连接用户是否为会话成员
    /// </summary>
    private async Task<bool> IsMemberAsync(long conversationId)
    {
        if (!long.TryParse(UserId, out var userId))
        {
            return false;
        }

        var member = await _memberRepository.GetMemberAsync(conversationId, userId);
        return member is not null;
    }
}
