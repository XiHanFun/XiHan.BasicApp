// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Chat.Domain.Entities;
using XiHan.BasicApp.Chat.Domain.Repositories;
using XiHan.Framework.Data.SqlSugar.Clients;

using XiHan.BasicApp.Saas.Infrastructure.Repositories;

namespace XiHan.BasicApp.Chat.Infrastructure.Repositories;

/// <summary>
/// 聊天消息表情回应仓储实现
/// </summary>
public sealed class ChatMessageReactionRepository(ISqlSugarClientResolver clientResolver)
    : SaasRepository<SysChatMessageReaction>(clientResolver), IChatMessageReactionRepository
{
    /// <summary>
    /// 按消息ID集合批量获取回应（历史消息聚合带出）
    /// </summary>
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

    /// <summary>
    /// 获取指定 (消息, 用户, 表情) 的回应（toggle 判定用）
    /// </summary>
    public async Task<SysChatMessageReaction?> GetAsync(long messageId, long userId, string emoji, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateQueryable()
            .Where(reaction => reaction.MessageId == messageId && reaction.UserId == userId && reaction.Emoji == emoji)
            .FirstAsync(cancellationToken);
    }
}
