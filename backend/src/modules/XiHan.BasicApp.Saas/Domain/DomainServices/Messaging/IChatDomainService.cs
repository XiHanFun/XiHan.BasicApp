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
    /// 编辑消息（仅文本、仅本人、限时窗口内；EditedTime 置标）
    /// </summary>
    Task<ChatMessageEditResult> EditMessageAsync(ChatMessageEditCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 表情回应 toggle（同消息+用户+表情已存在则取消，否则新增）
    /// </summary>
    Task<ChatReactionToggleResult> ToggleReactionAsync(ChatReactionToggleCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 消息 Pin/取消 Pin（单聊双方皆可，群/部门群仅群主与管理员；每会话有上限）
    /// </summary>
    Task<ChatMessagePinResult> SetMessagePinAsync(ChatMessagePinCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 会话置顶 toggle（个人维度），返回新状态
    /// </summary>
    Task<bool> TogglePinConversationAsync(ChatMemberToggleCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 会话免打扰 toggle（个人维度），返回新状态
    /// </summary>
    Task<bool> ToggleMuteConversationAsync(ChatMemberToggleCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 标记会话已读（未读清零并记录已读位；结果用于已读位实时扇出）
    /// </summary>
    Task<ChatMarkReadResult> MarkConversationReadAsync(ChatMarkReadCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新群信息（群聊可改名/公告/描述；部门群名称随部门禁改；公告变更追加系统提示）
    /// </summary>
    Task<ChatGovernanceResult> UpdateConversationInfoAsync(ChatConversationInfoUpdateCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 转让群主（仅群聊、仅群主、新主须现成员；旧主降为普通成员并追加系统提示）
    /// </summary>
    Task<ChatGovernanceResult> TransferOwnerAsync(ChatOwnerTransferCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 成员禁言/解除（群主与管理员可操作普通成员；被禁言者发送/编辑被拦截）
    /// </summary>
    Task<ChatGovernanceResult> SetMemberSilenceAsync(ChatMemberSilenceCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 设置成员角色（仅群聊、仅群主；只能在管理员与普通成员之间切换）
    /// </summary>
    Task<ChatGovernanceResult> SetMemberRoleAsync(ChatMemberRoleCommand command, CancellationToken cancellationToken = default);
}
