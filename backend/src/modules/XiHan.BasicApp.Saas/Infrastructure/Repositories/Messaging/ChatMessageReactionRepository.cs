#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ChatMessageReactionRepository
// Guid:5b8d7c0f-3e4a-4b1c-d9f2-7e8f9a0b1c2d
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
/// 聊天消息表情回应仓储实现
/// </summary>
public sealed class ChatMessageReactionRepository(ISqlSugarClientResolver clientResolver)
    : SaasRepository<SysChatMessageReaction>(clientResolver), IChatMessageReactionRepository
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<SysChatMessageReaction>> GetByMessageIdsAsync(IReadOnlyCollection<long> messageIds, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (messageIds.Count == 0)
        {
            return [];
        }

        var ids = messageIds.ToList();
        return await CreateQueryable()
            .Where(reaction => ids.Contains(reaction.MessageId))
            .OrderBy(reaction => reaction.CreatedTime)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<SysChatMessageReaction?> GetAsync(long messageId, long userId, string emoji, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateQueryable()
            .Where(reaction => reaction.MessageId == messageId && reaction.UserId == userId && reaction.Emoji == emoji)
            .FirstAsync(cancellationToken);
    }
}
