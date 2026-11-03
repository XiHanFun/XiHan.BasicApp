// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text.Json;
using XiHan.BasicApp.Chat.Domain.Services;
using XiHan.BasicApp.Contracts;
using XiHan.BasicApp.Organization.Domain.Entities;
using XiHan.BasicApp.Runtime.Persistence;
using XiHan.BasicApp.Runtime.Queries;
using XiHan.Framework.Messaging.Abstractions.Handlers;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Chat.Handlers;

/// <summary>
/// 部门归属变了：同步部门群的成员
/// </summary>
/// <remarks>
/// 订阅 Organization 的领域事件 <see cref="Topics.OrganizationMembershipChanged"/>（竞争消费，信封带租户）。照 4.x 的 <c>ChatDepartmentMemberSyncEventHandler</c>：
/// 只同步已经建了的部门群，进了部门即入群、移出部门即出群，推会话变更给群里的人（被移出的人也收到）
/// 以消息里的用户与部门为线索、按处理时的归属核对（同一用户进出部门的消息可能乱序到达），重复投递也只做一次，即按消息幂等
/// 解不开或写库失败即投递失败、由代理重试（4.x 只记日志）
/// </remarks>
/// <param name="store">按侧读写聊天表</param>
/// <param name="conversations">会话服务</param>
/// <param name="push">推送</param>
public sealed class MembershipChangedHandler(ChatStore store, ChatConversationService conversations, ChatPush push) : IMessageHandler
{
    /// <inheritdoc />
    public async ValueTask<Result> HandleAsync(MessageDelivery delivery, InvocationContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(delivery);
        ArgumentNullException.ThrowIfNull(context);
        var changed = JsonSerializer.Deserialize(delivery.Envelope.Data.Span, ChatJsonContext.Default.MembershipChanged)
            ?? throw new InvalidOperationException($"Message '{delivery.Envelope.Id}' on '{delivery.Topic}' has no data.");
        var side = SideRows.SideOf(context);
        var conversation = await conversations.DepartmentConversationAsync(side, changed.DepartmentId, cancellationToken).ConfigureAwait(false);
        if (conversation is null)
        {
            return Result.Success();
        }

        var userId = changed.UserId;
        var departmentId = changed.DepartmentId;
        var inDepartment = await TenantScopedQuery.For<SysUserDepartment>(context).Where(m => m.DepartmentId == departmentId && m.UserId == userId).AnyAsync(cancellationToken).ConfigureAwait(false);
        var member = await store.MemberAsync(side, conversation.Id, userId, cancellationToken).ConfigureAwait(false);
        if (inDepartment && member is null)
        {
            await conversations.AddMemberRowsAsync(side, conversation.Id, [userId], ownerUserId: null, cancellationToken).ConfigureAwait(false);
            var recounted = await conversations.RecountAsync(side, conversation.Id, cancellationToken).ConfigureAwait(false);
            if (recounted.IsFailure)
            {
                return recounted.Error!;
            }

            // 推给入群后的全体成员：其余成员的成员列表与人数同样要刷新
            var recipients = await store.MemberIdsAsync(side, conversation.Id, cancellationToken).ConfigureAwait(false);
            await push.ChangedAsync(side, conversation.Id, ChatRealtimeMethods.ChangeTypes.DepartmentJoined, recipients, cancellationToken).ConfigureAwait(false);
        }
        else if (!inDepartment && member is not null)
        {
            await store.Rows.SoftDeleteAsync(member, cancellationToken).ConfigureAwait(false);
            var recounted = await conversations.RecountAsync(side, conversation.Id, cancellationToken).ConfigureAwait(false);
            if (recounted.IsFailure)
            {
                return recounted.Error!;
            }

            // 剩下的成员刷新列表，被移出的人自己也要收到（会话列表随之收敛）
            var recipients = await store.MemberIdsAsync(side, conversation.Id, cancellationToken).ConfigureAwait(false);
            await push.ChangedAsync(side, conversation.Id, ChatRealtimeMethods.ChangeTypes.DepartmentKicked, [.. recipients, userId], cancellationToken).ConfigureAwait(false);
        }

        return Result.Success();
    }
}
