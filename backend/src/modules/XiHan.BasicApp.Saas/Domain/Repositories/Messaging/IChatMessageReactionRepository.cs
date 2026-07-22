// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Domain.Repositories;

/// <summary>
/// 聊天消息表情回应仓储接口
/// </summary>
public interface IChatMessageReactionRepository : ISaasRepository<SysChatMessageReaction>
{
    /// <summary>
    /// 按消息ID集合批量获取回应（历史消息聚合带出）
    /// </summary>
    Task<IReadOnlyList<SysChatMessageReaction>> GetByMessageIdsAsync(IReadOnlyCollection<long> messageIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取指定 (消息, 用户, 表情) 的回应（toggle 判定用）
    /// </summary>
    Task<SysChatMessageReaction?> GetAsync(long messageId, long userId, string emoji, CancellationToken cancellationToken = default);
}
