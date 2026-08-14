// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Chat.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Chat.Domain.Repositories;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Data.SqlSugar.Clients;

using XiHan.BasicApp.Saas.Infrastructure.Repositories;

namespace XiHan.BasicApp.Chat.Infrastructure.Repositories;

/// <summary>
/// 聊天会话成员仓储实现
/// </summary>
public sealed class ChatConversationMemberRepository(ISqlSugarClientResolver clientResolver)
    : SaasRepository<SysChatConversationMember>(clientResolver), IChatConversationMemberRepository
{
    /// <summary>
    /// 查询某会话内某用户的成员记录
    /// </summary>
    public async Task<SysChatConversationMember?> GetMemberAsync(long conversationId, long userId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateQueryable()
            .Where(member => member.ConversationId == conversationId && member.UserId == userId)
            .FirstAsync(cancellationToken);
    }

    /// <summary>
    /// 查询会话全部成员
    /// </summary>
    public async Task<IReadOnlyList<SysChatConversationMember>> GetByConversationIdAsync(long conversationId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateQueryable()
            .Where(member => member.ConversationId == conversationId)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 会话内除指定用户外全员未读数原子 +1（发消息路径）
    /// </summary>
    public async Task<int> IncrementUnreadAsync(long conversationId, long exceptUserId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await DbClient.Updateable<SysChatConversationMember>()
            .SetColumns(member => member.UnreadCount == member.UnreadCount + 1)
            .Where(member => member.ConversationId == conversationId && member.UserId != exceptUserId && !member.IsDeleted)
            .ExecuteCommandAsync(cancellationToken);
    }

    /// <summary>
    /// 查询某用户的全部会话成员记录（会话列表数据源）
    /// </summary>
    public async Task<IReadOnlyList<SysChatConversationMember>> GetByUserIdAsync(long userId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateQueryable()
            .Where(member => member.UserId == userId)
            .ToListAsync(cancellationToken);
    }
}
