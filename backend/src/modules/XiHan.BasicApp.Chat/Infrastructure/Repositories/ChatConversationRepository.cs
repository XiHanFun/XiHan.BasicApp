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
/// 聊天会话仓储实现
/// </summary>
public sealed class ChatConversationRepository(ISqlSugarClientResolver clientResolver)
    : SaasRepository<SysChatConversation>(clientResolver), IChatConversationRepository
{
    /// <summary>
    /// 按单聊配对键查询（租户内唯一）
    /// </summary>
    public async Task<SysChatConversation?> GetByPairKeyAsync(string pairKey, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateQueryable()
            .Where(conversation => conversation.PairKey == pairKey)
            .FirstAsync(cancellationToken);
    }

    /// <summary>
    /// 按部门查询部门群（租户内同一部门至多一个）
    /// </summary>
    public async Task<SysChatConversation?> GetByDepartmentIdAsync(long departmentId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateQueryable()
            .Where(conversation => conversation.DepartmentId == departmentId && conversation.ConversationType == ChatConversationType.Department)
            .FirstAsync(cancellationToken);
    }
}
