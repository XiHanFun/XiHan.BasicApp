#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IChatDomainService
// Guid:0f4a3b6c-8bf3-4ab3-d942-3e4f5a6b7c8d
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/03 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 聊天领域服务：会话生命周期、成员关系与消息不变量
/// </summary>
public interface IChatDomainService
{
    /// <summary>
    /// 获取或创建单聊会话（同一对用户租户内唯一）
    /// </summary>
    Task<ChatConversationCommandResult> GetOrCreateSingleConversationAsync(ChatSingleConversationCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 创建群聊（发起者为群主并自动入群）
    /// </summary>
    Task<ChatConversationCommandResult> CreateGroupConversationAsync(ChatGroupCreateCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取或创建部门群，并把部门现有成员同步进群（操作者须属于该部门）
    /// </summary>
    Task<ChatConversationCommandResult> GetOrCreateDepartmentConversationAsync(ChatDepartmentConversationCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 添加群成员（群主/管理员可操作；单聊/部门群拒绝）
    /// </summary>
    Task<ChatConversationCommandResult> AddMembersAsync(ChatMemberAddCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 移除群成员或主动退群（群主不可退群，需先移交）
    /// </summary>
    Task<ChatConversationCommandResult> RemoveMemberAsync(ChatMemberRemoveCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 发送消息：成员校验 → 落消息 → 会话最后消息冗余 → 其余成员未读 +1
    /// </summary>
    Task<ChatMessageSendResult> SendMessageAsync(ChatMessageSendCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 撤回消息（仅本人、限时窗口内；置标清文，保留行维持时序）
    /// </summary>
    Task<ChatMessageRecallResult> RecallMessageAsync(ChatMessageRecallCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 标记会话已读（未读清零并记录已读位）
    /// </summary>
    Task MarkConversationReadAsync(ChatMarkReadCommand command, CancellationToken cancellationToken = default);
}
