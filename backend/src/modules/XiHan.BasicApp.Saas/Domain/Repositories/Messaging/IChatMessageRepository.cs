// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

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

    /// <summary>
    /// 获取会话内被 Pin 的消息（按 Pin 时间倒序）
    /// </summary>
    Task<IReadOnlyList<SysChatMessage>> GetPinnedAsync(long conversationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 定位加载：以 aroundMessageId 为中心，取之前 beforeTake 条（不含）+ 自身及之后 afterTake 条，按消息 ID 正序
    /// </summary>
    Task<IReadOnlyList<SysChatMessage>> GetAroundAsync(long conversationId, long aroundMessageId, int beforeTake, int afterTake, CancellationToken cancellationToken = default);

    /// <summary>
    /// 会话内关键字搜索（正文/文件名 LIKE，排除已撤回），按消息 ID 倒序游标分页
    /// </summary>
    Task<IReadOnlyList<SysChatMessage>> SearchAsync(long conversationId, string keyword, long? beforeMessageId, int take, CancellationToken cancellationToken = default);
}
