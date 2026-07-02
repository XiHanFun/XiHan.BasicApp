#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IChatQueryService
// Guid:7d2f1a4b-6adc-4baa-efb9-0f1a2b3c4d5e
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/03 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.Framework.Application.Contracts.Services;

namespace XiHan.BasicApp.Saas.Application.Contracts;

/// <summary>
/// 聊天查询应用服务接口
/// </summary>
public interface IChatQueryService : IApplicationService
{
    /// <summary>
    /// 获取我的会话列表（含未读数，按最后消息时间倒序）
    /// </summary>
    Task<List<ChatConversationListItemDto>> GetMyConversationsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取会话消息历史（游标分页，仅会话成员）
    /// </summary>
    Task<ChatMessageHistoryResultDto> GetMessageHistoryAsync(ChatMessageHistoryQueryDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取会话成员列表（仅会话成员）
    /// </summary>
    Task<List<ChatMemberItemDto>> GetMembersAsync(long conversationId, CancellationToken cancellationToken = default);
}
