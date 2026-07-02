#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ChatDepartmentMemberSyncEventHandler
// Guid:1b6d5e8f-0eba-4fee-cd23-4d5e6f7a8b9c
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/03 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Saas.Application.Services;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Events;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.EventBus.Abstractions.Local;

namespace XiHan.BasicApp.Saas.Application.EventHandlers;

/// <summary>
/// 聊天部门群成员同步事件处理器
/// </summary>
/// <remarks>
/// 订阅用户部门归属变更：分配进部门 → 自动加入已存在的部门群；移出部门 → 即时踢出部门群。
/// 部门群不存在时为空操作（首次打开部门群时会全量同步成员）。同步失败只记日志，不阻断主流程。
/// </remarks>
public sealed class ChatDepartmentMemberSyncEventHandler : ILocalEventHandler<UserDepartmentChangedDomainEvent>
{
    private readonly IChatConversationRepository _conversationRepository;

    private readonly IChatConversationMemberRepository _memberRepository;

    private readonly IChatRealtimePushService _pushService;

    private readonly ILogger<ChatDepartmentMemberSyncEventHandler> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    public ChatDepartmentMemberSyncEventHandler(
        IChatConversationRepository conversationRepository,
        IChatConversationMemberRepository memberRepository,
        IChatRealtimePushService pushService,
        ILogger<ChatDepartmentMemberSyncEventHandler> logger)
    {
        _conversationRepository = conversationRepository;
        _memberRepository = memberRepository;
        _pushService = pushService;
        _logger = logger;
    }

    /// <summary>
    /// 处理用户部门归属变更：同步部门群成员
    /// </summary>
    /// <param name="eventData">事件数据</param>
    public async Task HandleEventAsync(UserDepartmentChangedDomainEvent eventData)
    {
        ArgumentNullException.ThrowIfNull(eventData);

        try
        {
            var conversation = await _conversationRepository.GetByDepartmentIdAsync(eventData.DepartmentId);
            if (conversation is null)
            {
                return;
            }

            var member = await _memberRepository.GetMemberAsync(conversation.BasicId, eventData.UserId);
            if (eventData.IsAssigned)
            {
                if (member is not null)
                {
                    return;
                }

                _ = await _memberRepository.AddAsync(new SysChatConversationMember
                {
                    ConversationId = conversation.BasicId,
                    UserId = eventData.UserId,
                    MemberRole = ChatMemberRole.Member,
                    JoinTime = DateTimeOffset.UtcNow
                });
                conversation.MemberCount += 1;
                _ = await _conversationRepository.UpdateAsync(conversation);
                await _pushService.PushConversationChangedAsync(conversation.BasicId, "department-joined", [eventData.UserId]);
            }
            else
            {
                if (member is null)
                {
                    return;
                }

                _ = await _memberRepository.DeleteAsync(member);
                conversation.MemberCount = Math.Max(0, conversation.MemberCount - 1);
                _ = await _conversationRepository.UpdateAsync(conversation);
                await _pushService.PushConversationChangedAsync(conversation.BasicId, "department-kicked", [eventData.UserId]);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "部门群成员同步失败（不阻断主流程），UserId={UserId}, DepartmentId={DepartmentId}, IsAssigned={IsAssigned}",
                eventData.UserId, eventData.DepartmentId, eventData.IsAssigned);
        }
    }
}
