// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Data.SqlSugar.Clients;

namespace XiHan.BasicApp.Saas.Infrastructure.Repositories;

/// <summary>
/// 聊天消息仓储实现
/// </summary>
public sealed class ChatMessageRepository(ISqlSugarClientResolver clientResolver)
    : SaasRepository<SysChatMessage>(clientResolver), IChatMessageRepository
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<SysChatMessage>> GetHistoryAsync(long conversationId, long? beforeMessageId, int take, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var query = CreateQueryable().Where(message => message.ConversationId == conversationId);
        if (beforeMessageId is { } before && before > 0)
        {
            query = query.Where(message => message.BasicId < before);
        }

        return await query
            .OrderByDescending(message => message.BasicId)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SysChatMessage>> GetPinnedAsync(long conversationId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateQueryable()
            .Where(message => message.ConversationId == conversationId && message.IsPinned)
            .OrderByDescending(message => message.PinnedTime)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SysChatMessage>> GetAroundAsync(long conversationId, long aroundMessageId, int beforeTake, int afterTake, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var before = await CreateQueryable()
            .Where(message => message.ConversationId == conversationId && message.BasicId < aroundMessageId)
            .OrderByDescending(message => message.BasicId)
            .Take(beforeTake)
            .ToListAsync(cancellationToken);
        var fromTarget = await CreateQueryable()
            .Where(message => message.ConversationId == conversationId && message.BasicId >= aroundMessageId)
            .OrderBy(message => message.BasicId)
            .Take(afterTake + 1)
            .ToListAsync(cancellationToken);

        return [.. before.OrderBy(message => message.BasicId), .. fromTarget];
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SysChatMessage>> SearchAsync(long conversationId, string keyword, long? beforeMessageId, int take, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var query = CreateQueryable()
            .Where(message => message.ConversationId == conversationId && !message.IsRecalled)
            .Where(message => message.Content!.Contains(keyword) || message.Attachments!.Contains(keyword));
        if (beforeMessageId is { } before && before > 0)
        {
            query = query.Where(message => message.BasicId < before);
        }

        return await query
            .OrderByDescending(message => message.BasicId)
            .Take(take)
            .ToListAsync(cancellationToken);
    }
}
