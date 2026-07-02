#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ChatConversationMemberRepository
// Guid:7c1d0e3f-5ec0-4de0-a61f-0b1c2d3e4f5a
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/03 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Data.SqlSugar.Clients;

namespace XiHan.BasicApp.Saas.Infrastructure.Repositories;

/// <summary>
/// 聊天会话成员仓储实现
/// </summary>
public sealed class ChatConversationMemberRepository(ISqlSugarClientResolver clientResolver)
    : SaasRepository<SysChatConversationMember>(clientResolver), IChatConversationMemberRepository
{
    /// <inheritdoc />
    public async Task<SysChatConversationMember?> GetMemberAsync(long conversationId, long userId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateQueryable()
            .Where(member => member.ConversationId == conversationId && member.UserId == userId)
            .FirstAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SysChatConversationMember>> GetByConversationIdAsync(long conversationId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateQueryable()
            .Where(member => member.ConversationId == conversationId)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> IncrementUnreadAsync(long conversationId, long exceptUserId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await DbClient.Updateable<SysChatConversationMember>()
            .SetColumns(member => member.UnreadCount == member.UnreadCount + 1)
            .Where(member => member.ConversationId == conversationId && member.UserId != exceptUserId && !member.IsDeleted)
            .ExecuteCommandAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SysChatConversationMember>> GetByUserIdAsync(long userId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateQueryable()
            .Where(member => member.UserId == userId)
            .ToListAsync(cancellationToken);
    }
}
