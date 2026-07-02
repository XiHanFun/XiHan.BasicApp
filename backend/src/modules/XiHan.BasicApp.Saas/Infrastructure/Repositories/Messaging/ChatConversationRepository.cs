#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ChatConversationRepository
// Guid:6b0c9d2e-4dbf-4cdf-f50e-9a0b1c2d3e4f
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
