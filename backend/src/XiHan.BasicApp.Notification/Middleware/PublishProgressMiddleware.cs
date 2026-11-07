// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.Contracts.Realtime;
using XiHan.BasicApp.Notification.Domain.Services;
using XiHan.BasicApp.Notification.Operations.Notifications;
using XiHan.BasicApp.Runtime.Persistence;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Kernel.Pipeline;
using XiHan.Framework.Localization.Abstractions.Messages;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Notification.Middleware;

/// <summary>
/// 把通知发布的开始与失败推给发布人
/// </summary>
/// <remarks>
/// 发布时灵动岛先显示「正在发布」，失败时收尾并写明原因。成功带着接收人数，由 <see cref="PublishNotification"/> 随提交发出
/// 排在 Transaction 阶段开头、工作单元之外：这两条不随事务生效，要在事务开始前、回滚之后立即发出，提交失败与异常也要让那一条收尾。排在授权之后，没有发布权限的调用什么也收不到
/// </remarks>
public sealed class PublishProgressMiddleware : IPipelineMiddleware
{
    /// <inheritdoc />
    public async ValueTask InvokeAsync(InvocationContext context, PipelineStep nextStep)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(nextStep);
        if (!context.GetOperation().TryGetValue(out var operation)
            || !string.Equals(operation.Id, PublishNotification.Id, StringComparison.Ordinal)
            || NotificationService.CallerOf(context) is not { } publisher)
        {
            await nextStep(context).ConfigureAwait(false);
            return;
        }

        var taskId = NotificationService.PublishTaskIdOf(context);
        var audience = RealtimeAudience.UsersIn(SideRows.SideOf(context), [publisher]);
        await SendAsync(context, audience, TaskProgressPayload.Running(taskId, context.Localize(NotificationMessages.PublishProgressRunning))).ConfigureAwait(false);
        var finished = false;
        string? reason = null;
        try
        {
            await nextStep(context).ConfigureAwait(false);
            var failed = context.GetOutcome().TryGetValue(out var outcome) && outcome.IsFailure;
            reason = failed ? context.Localize(outcome.Error!) : null;
            finished = !failed;
        }
        finally
        {
            if (!finished)
            {
                await SendAsync(context, audience, TaskProgressPayload.Failed(taskId, context.Localize(NotificationMessages.PublishProgressFailed), reason)).ConfigureAwait(false);
            }
        }
    }

    private static async Task SendAsync(InvocationContext context, RealtimeAudience audience, TaskProgressPayload payload)
    {
        foreach (var push in context.Services.GetServices<IRealtimePush>())
        {
            await push.SendNowAsync(RealtimeHubs.Notification, audience, RealtimeMethods.TaskProgress, payload, NotificationJsonContext.Default.TaskProgressPayload, CancellationToken.None).ConfigureAwait(false);
        }
    }
}
