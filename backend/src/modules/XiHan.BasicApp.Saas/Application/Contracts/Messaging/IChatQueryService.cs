// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.Framework.Application.Contracts.Services;

namespace XiHan.BasicApp.Saas.Application.Contracts;

/// <summary>
/// 聊天查询应用服务接口
/// </summary>
public interface IChatQueryService : IApplicationService
{
    /// <summary>
    /// 获取我的会话列表（含未读数，按最后消息时间倒序）
    /// </summary>
    Task<List<ChatConversationListItemDto>> GetMyConversationsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取会话消息历史（游标分页 / AroundMessageId 定位，仅会话成员）
    /// </summary>
    Task<ChatMessageHistoryResultDto> GetMessageHistoryAsync(ChatMessageHistoryQueryDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 会话内消息搜索（关键字匹配正文与文件名，排除已撤回；仅会话成员）
    /// </summary>
    Task<ChatMessageHistoryResultDto> GetMessageSearchAsync(ChatMessageSearchQueryDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取会话成员列表（仅会话成员）
    /// </summary>
    Task<List<ChatMemberItemDto>> GetMembersAsync(long conversationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取聊天可选用户（发起单聊/建群/加成员选人；仅需聊天查看权限的轻量端点）
    /// </summary>
    Task<IReadOnlyList<UserSelectItemDto>> GetUserOptionsAsync(UserSelectQueryDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取会话成员已读位（群已读回执；仅会话成员）
    /// </summary>
    Task<List<ChatReadPositionDto>> GetReadPositionsAsync(long conversationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取会话内被 Pin 的消息（仅会话成员）
    /// </summary>
    Task<List<ChatMessageItemDto>> GetPinnedMessagesAsync(long conversationId, CancellationToken cancellationToken = default);
}
