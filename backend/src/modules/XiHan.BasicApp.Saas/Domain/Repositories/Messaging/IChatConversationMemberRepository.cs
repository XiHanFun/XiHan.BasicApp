#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IChatConversationMemberRepository
// Guid:4f8a7b0c-2b9e-4abf-d3ec-7e8f9a0b1c2d
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/03 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

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
}
