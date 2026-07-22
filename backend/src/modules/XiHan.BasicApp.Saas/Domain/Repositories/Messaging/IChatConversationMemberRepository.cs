// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Domain.Repositories;

/// <summary>
/// 聊天会话成员仓储接口
/// </summary>
public interface IChatConversationMemberRepository : ISaasRepository<SysChatConversationMember>
{
    /// <summary>
    /// 查询某会话内某用户的成员记录
    /// </summary>
    Task<SysChatConversationMember?> GetMemberAsync(long conversationId, long userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 查询会话全部成员
    /// </summary>
    Task<IReadOnlyList<SysChatConversationMember>> GetByConversationIdAsync(long conversationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 会话内除指定用户外全员未读数原子 +1（发消息路径）
    /// </summary>
    Task<int> IncrementUnreadAsync(long conversationId, long exceptUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 查询某用户的全部会话成员记录（会话列表数据源）
    /// </summary>
    Task<IReadOnlyList<SysChatConversationMember>> GetByUserIdAsync(long userId, CancellationToken cancellationToken = default);
}
