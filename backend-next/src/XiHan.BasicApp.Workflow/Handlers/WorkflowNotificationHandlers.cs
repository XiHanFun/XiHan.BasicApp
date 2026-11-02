// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Contracts.Notification;
using XiHan.BasicApp.Contracts.Users;
using XiHan.BasicApp.Runtime.Persistence;
using XiHan.BasicApp.Workflow.Domain.Entities;
using XiHan.BasicApp.Workflow.Domain.Services;
using XiHan.Framework.Messaging.Abstractions.Handlers;
using XiHan.Framework.Persistence.Abstractions.Identifiers;
using XiHan.Framework.Primitives;
using XiHan.Framework.Workflow.Abstractions.Events;
using XiHan.Framework.Workflow.Abstractions.Serialization;
using XiHan.Framework.Workflow.Abstractions.Tasks;

namespace XiHan.BasicApp.Workflow.Handlers;

/// <summary>
/// 通知处理器共用的去重与站内投递（作用域）。
/// </summary>
/// <remarks>
/// 照 4.x 的 <c>WorkflowNotificationDispatcher</c>：经 Notification 的站内待办投递发「待办」类站内信，链接到审批中心；Notification 没装时不发，投递返回失败只记日志、不阻断流程（4.x 同）。
/// 去重照设计稿 D7：第一次处理一条消息时在同一个事务里写回执，再投递时看到回执即跳过。
/// 文案按收件人的界面语言出（<see cref="IUserLocales"/>，偏好里同步的语言），收件人按语言分组各投一条；没存语言的按这次调用的区域，
/// 后台恢复流程时调用没有区域，落到缺省区域。
/// </remarks>
/// <param name="rows">按侧读写。</param>
/// <param name="ids">ID 生成。</param>
/// <param name="deliveries">站内待办投递（Notification 实现）。</param>
/// <param name="locales">用户的界面语言。</param>
/// <param name="texts">文案。</param>
/// <param name="logger">日志。</param>
public sealed partial class WorkflowNotifier(SideRows rows, IIdGenerator<Guid> ids, IEnumerable<IInboxDelivery> deliveries, IUserLocales locales, WorkflowTexts texts, ILogger<WorkflowNotifier> logger)
{
    /// <summary>
    /// 审批中心「待我审批」。
    /// </summary>
    public const string TodoLink = "/workflow/approvals?tab=todo";

    /// <summary>
    /// 审批中心「抄送我的」。
    /// </summary>
    public const string CcLink = "/workflow/approvals?tab=cc";

    /// <summary>
    /// 审批中心「我发起的」。
    /// </summary>
    public const string StartedLink = "/workflow/approvals?tab=started";

    /// <summary>
    /// 站内信的业务类型。
    /// </summary>
    public const string BusinessType = "workflow";

    /// <summary>
    /// 第一次处理这条消息时写回执并返回 <c>true</c>，处理过的返回 <c>false</c>。
    /// </summary>
    /// <param name="delivery">投递。</param>
    /// <param name="context">调用上下文。</param>
    /// <returns>是否第一次。</returns>
    public async Task<bool> FirstTimeAsync(MessageDelivery delivery, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(delivery);
        ArgumentNullException.ThrowIfNull(context);
        var side = SideRows.SideOf(context);
        var messageId = delivery.Envelope.Id;
        if (await rows.Records<SysWorkflowMessageReceipt>(side).AnyAsync(r => r.MessageId == messageId, context.CancellationToken).ConfigureAwait(false))
        {
            return false;
        }

        await rows.AppendAsync(new SysWorkflowMessageReceipt { Id = ids.NewId(), TenantId = side, MessageId = messageId, Topic = delivery.Topic }, context.CancellationToken).ConfigureAwait(false);
        return true;
    }

    /// <summary>
    /// 给一组用户投一条待办类站内信。
    /// </summary>
    /// <remarks>
    /// 收件人按界面语言分组，每组按自己的区域出一次文案、投一条。
    /// </remarks>
    /// <param name="context">调用上下文。</param>
    /// <param name="users">收件人。</param>
    /// <param name="render">按区域出标题与正文。</param>
    /// <param name="link">审批中心的链接。</param>
    /// <param name="businessId">业务 ID（任务或实例）。</param>
    public async Task DeliverAsync(InvocationContext context, IReadOnlyCollection<Guid> users, Func<string, (string Title, string Content)> render, string link, Guid businessId)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(users);
        ArgumentNullException.ThrowIfNull(render);
        if (users.Count == 0 || !deliveries.Any())
        {
            return;
        }

        var preferred = await locales.PreferredAsync(users, context.CancellationToken).ConfigureAwait(false);
        var fallback = texts.LocaleOf(context);
        foreach (var group in users.Distinct().GroupBy(u => preferred.TryGetValue(u, out var locale) ? texts.Negotiate(locale) : fallback, StringComparer.Ordinal))
        {
            var (title, content) = render(group.Key);
            var recipients = group.ToList();
            foreach (var delivery in deliveries)
            {
                var delivered = await delivery.DeliverAsync(context, new InboxMessage(recipients, title, content, link, BusinessType, businessId.ToString())).ConfigureAwait(false);
                if (delivered.IsFailure)
                {
                    LogDeliveryFailed(logger, delivered.Error!.Code, businessId);
                }
            }
        }
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Warning, Message = "A workflow notification was not delivered ({Code}) for {BusinessId}; the workflow goes on.")]
    private static partial void LogDeliveryFailed(ILogger logger, string code, Guid businessId);
}

/// <summary>
/// 用户任务创建：通知受理人与抄送人，写抄送记录。
/// </summary>
/// <remarks>
/// 订阅 <see cref="WorkflowTopics.TaskCreated"/>（竞争消费，信封带租户）。照 4.x 的 <c>WorkflowUserTaskCreatedNotificationHandler</c> 与 <c>WorkflowCarbonCopyRecordHandler</c>：
/// 受理人收「待办审批」，被抄送人收「抄送」并写一条抄送记录。依次审批每一轮、会签每个受理人的任务都带着同一串抄送人，同一实例同一节点对同一人只记一次、也只通知一次
/// （4.x 每条任务都通知一遍）。
/// </remarks>
/// <param name="notifier">去重与投递。</param>
/// <param name="records">审批记录。</param>
/// <param name="texts">文案。</param>
public sealed class TaskCreatedHandler(WorkflowNotifier notifier, WorkflowTaskRecords records, WorkflowTexts texts) : IMessageHandler
{
    /// <inheritdoc />
    public async ValueTask<Result> HandleAsync(MessageDelivery delivery, InvocationContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(delivery);
        ArgumentNullException.ThrowIfNull(context);
        if (!await notifier.FirstTimeAsync(delivery, context).ConfigureAwait(false))
        {
            return Result.Success();
        }

        var data = WorkflowJson.DeserializeEvent(delivery.Envelope.Data.Span);
        if (data.TaskId is null || data.NodeId is null)
        {
            return Result.Success();
        }

        var taskId = WorkflowIds.ToContract(data.TaskId);
        var instanceTitle = data.Title ?? data.DefinitionName;
        var title = WorkflowPayload.Text(data.Payload, UserTaskPayloadKeys.Title) ?? instanceTitle;
        if (WorkflowIds.UserOf(data.AssigneeId) is { } assignee)
        {
            await notifier.DeliverAsync(context, [assignee], locale => (texts.Render(locale, WorkflowMessages.NotifyTodoTitle, ("title", title)), texts.Render(locale, WorkflowMessages.NotifyTodoContent, ("instance", instanceTitle))), WorkflowNotifier.TodoLink, taskId).ConfigureAwait(false);
        }

        var cc = (data.CcUserIds ?? []).Select(WorkflowIds.UserOf).OfType<Guid>().Distinct().ToList();
        if (cc.Count == 0)
        {
            return Result.Success();
        }

        var side = SideRows.SideOf(context);
        var instanceId = WorkflowIds.ToContract(data.InstanceId);
        var copied = await records.CopiedAsync(side, instanceId, data.NodeId, cancellationToken).ConfigureAwait(false);
        var recipients = cc.Where(u => !copied.Contains(u)).ToList();
        if (recipients.Count == 0)
        {
            return Result.Success();
        }

        var snapshot = new WorkflowTaskSnapshot(taskId, instanceId, data.DefinitionId, data.DefinitionName, instanceTitle, data.NodeId, title, data.CorrelationId);
        await records.AppendAsync(side, snapshot, recipients, WorkflowTaskRecordAction.CarbonCopied, null, null, null, cancellationToken).ConfigureAwait(false);
        await notifier.DeliverAsync(context, recipients, locale => (texts.Render(locale, WorkflowMessages.NotifyCcTitle, ("title", title)), texts.Render(locale, WorkflowMessages.NotifyCcContent, ("instance", instanceTitle))), WorkflowNotifier.CcLink, taskId).ConfigureAwait(false);
        return Result.Success();
    }
}

/// <summary>
/// 转办：通知新受理人。
/// </summary>
/// <remarks>
/// 订阅 <see cref="WorkflowTopics.TaskTransferred"/>。照 4.x 的 <c>WorkflowUserTaskTransferredNotificationHandler</c>，带上转办意见。
/// </remarks>
/// <param name="notifier">去重与投递。</param>
/// <param name="texts">文案。</param>
public sealed class TaskTransferredHandler(WorkflowNotifier notifier, WorkflowTexts texts) : IMessageHandler
{
    /// <inheritdoc />
    public async ValueTask<Result> HandleAsync(MessageDelivery delivery, InvocationContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(delivery);
        ArgumentNullException.ThrowIfNull(context);
        if (!await notifier.FirstTimeAsync(delivery, context).ConfigureAwait(false))
        {
            return Result.Success();
        }

        var data = WorkflowJson.DeserializeEvent(delivery.Envelope.Data.Span);
        if (WorkflowIds.UserOf(data.AssigneeId) is not { } target || data.TaskId is null)
        {
            return Result.Success();
        }

        var reason = data.Reason;
        await notifier.DeliverAsync(
            context,
            [target],
            locale => (
                texts.Render(locale, WorkflowMessages.NotifyTransferTitle),
                string.IsNullOrWhiteSpace(reason)
                    ? texts.Render(locale, WorkflowMessages.NotifyTransferContent)
                    : texts.Render(locale, WorkflowMessages.NotifyTransferContentWithComment, ("comment", reason))),
            WorkflowNotifier.TodoLink,
            WorkflowIds.ToContract(data.TaskId)).ConfigureAwait(false);
        return Result.Success();
    }
}

/// <summary>
/// 实例故障：通知发起人。
/// </summary>
/// <remarks>
/// 订阅 <see cref="WorkflowTopics.InstanceFaulted"/>。照 4.x 的 <c>WorkflowInstanceFaultedNotificationHandler</c>：只通知发起人，链接到「我发起的」。
/// </remarks>
/// <param name="notifier">去重与投递。</param>
/// <param name="texts">文案。</param>
public sealed class InstanceFaultedHandler(WorkflowNotifier notifier, WorkflowTexts texts) : IMessageHandler
{
    /// <inheritdoc />
    public async ValueTask<Result> HandleAsync(MessageDelivery delivery, InvocationContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(delivery);
        ArgumentNullException.ThrowIfNull(context);
        if (!await notifier.FirstTimeAsync(delivery, context).ConfigureAwait(false))
        {
            return Result.Success();
        }

        var data = WorkflowJson.DeserializeEvent(delivery.Envelope.Data.Span);
        if (WorkflowIds.UserOf(data.StarterId) is not { } starter)
        {
            return Result.Success();
        }

        var instance = data.Title ?? data.DefinitionName;
        await notifier.DeliverAsync(
            context,
            [starter],
            locale => (
                texts.Render(locale, WorkflowMessages.NotifyFaultTitle, ("instance", instance)),
                texts.Render(locale, WorkflowMessages.NotifyFaultContent, ("instance", instance), ("reason", data.Fault?.Message ?? string.Empty))),
            WorkflowNotifier.StartedLink,
            WorkflowIds.ToContract(data.InstanceId)).ConfigureAwait(false);
        return Result.Success();
    }
}
