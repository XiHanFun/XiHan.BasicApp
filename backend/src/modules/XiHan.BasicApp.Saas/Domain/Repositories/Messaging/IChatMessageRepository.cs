#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IChatMessageRepository
// Guid:5a9b8c1d-3cae-4bcf-e4fd-8f9a0b1c2d3e
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/03 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Domain.Repositories;

/// <summary>
/// 聊天消息仓储接口
/// </summary>
public interface IChatMessageRepository : ISaasRepository<SysChatMessage>
{
    /// <summary>
    /// 会话消息历史游标分页：取 beforeMessageId 之前（不含）的最近 take 条，按消息 ID 倒序
    /// </summary>
    Task<IReadOnlyList<SysChatMessage>> GetHistoryAsync(long conversationId, long? beforeMessageId, int take, CancellationToken cancellationToken = default);
}
