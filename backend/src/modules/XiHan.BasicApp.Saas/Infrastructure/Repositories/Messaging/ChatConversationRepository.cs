// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Data.SqlSugar.Clients;

namespace XiHan.BasicApp.Saas.Infrastructure.Repositories;

/// <summary>
/// 聊天会话仓储实现
/// </summary>
public sealed class ChatConversationRepository(ISqlSugarClientResolver clientResolver)
    : SaasRepository<SysChatConversation>(clientResolver), IChatConversationRepository
{
    /// <inheritdoc />
    public async Task<SysChatConversation?> GetByPairKeyAsync(string pairKey, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateQueryable()
            .Where(conversation => conversation.PairKey == pairKey)
            .FirstAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<SysChatConversation?> GetByDepartmentIdAsync(long departmentId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateQueryable()
            .Where(conversation => conversation.DepartmentId == departmentId && conversation.ConversationType == ChatConversationType.Department)
            .FirstAsync(cancellationToken);
    }
}
