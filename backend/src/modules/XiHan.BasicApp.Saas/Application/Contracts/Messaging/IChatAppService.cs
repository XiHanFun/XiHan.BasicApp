#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IChatAppService
// Guid:6c1e0f3a-5fcb-4af9-dea8-9e0f1a2b3c4d
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/03 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.Framework.Application.Contracts.Services;

namespace XiHan.BasicApp.Saas.Application.Contracts;

/// <summary>
/// 聊天命令应用服务接口
/// </summary>
public interface IChatAppService : IApplicationService
{
    /// <summary>
    /// 打开单聊会话（不存在则创建）
    /// </summary>
    Task<ChatConversationDto> OpenSingleConversationAsync(ChatSingleConversationOpenDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 创建群聊
    /// </summary>
    Task<ChatConversationDto> CreateGroupConversationAsync(ChatGroupCreateDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 打开部门群（不存在则创建，成员按部门归属同步）
    /// </summary>
    Task<ChatConversationDto> OpenDepartmentConversationAsync(ChatDepartmentConversationOpenDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 添加群成员
    /// </summary>
    Task AddMembersAsync(ChatMemberAddDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 移除群成员/退群
    /// </summary>
    Task RemoveMemberAsync(ChatMemberRemoveDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 发送消息（落库后向会话成员实时推送）
    /// </summary>
    Task<ChatMessageItemDto> SendMessageAsync(ChatMessageSendDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 撤回消息（仅本人限时）
    /// </summary>
    Task RecallMessageAsync(long messageId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 编辑消息（仅文本、仅本人、限时窗口）
    /// </summary>
    Task<ChatMessageItemDto> EditMessageAsync(ChatMessageEditDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 表情回应 toggle（已存在则取消，否则新增）
    /// </summary>
    Task<ChatReactionToggleResultDto> ToggleReactionAsync(ChatReactionToggleDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pin 消息（单聊双方皆可，群仅群主/管理员；每会话有上限）
    /// </summary>
    Task PinMessageAsync(ChatMessagePinDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 取消 Pin 消息
    /// </summary>
    Task UnpinMessageAsync(ChatMessagePinDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 会话置顶 toggle（个人维度）
    /// </summary>
    Task<ChatToggleStateDto> TogglePinConversationAsync(ChatConversationToggleDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 会话免打扰 toggle（个人维度）
    /// </summary>
    Task<ChatToggleStateDto> ToggleMuteConversationAsync(ChatConversationToggleDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 标记会话已读
    /// </summary>
    Task MarkReadAsync(ChatMarkReadDto input, CancellationToken cancellationToken = default);
}
