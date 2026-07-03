#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ChatMessageRepository
// Guid:8d2e1f4a-6fd1-4ef1-b720-1c2d3e4f5a6b
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
