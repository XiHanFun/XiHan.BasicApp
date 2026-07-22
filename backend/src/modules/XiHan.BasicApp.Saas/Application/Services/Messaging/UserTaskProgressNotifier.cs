// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Saas.Hubs;
using XiHan.Framework.Core.DependencyInjection.ServiceLifetimes;
using XiHan.Framework.Web.RealTime.Constants;
using XiHan.Framework.Web.RealTime.Services;

namespace XiHan.BasicApp.Saas.Application.Services;

/// <summary>
/// 用户后台任务进度推送服务实现（经通知 Hub 的 TaskProgress 事件）
/// </summary>
public sealed class UserTaskProgressNotifier
    : IUserTaskProgressNotifier, IScopedDependency
{
    private readonly IRealtimeNotificationService<BasicAppNotificationHub> _realtimeNotificationService;

    private readonly ILogger<UserTaskProgressNotifier> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    public UserTaskProgressNotifier(
        IRealtimeNotificationService<BasicAppNotificationHub> realtimeNotificationService,
        ILogger<UserTaskProgressNotifier> logger)
    {
        _realtimeNotificationService = realtimeNotificationService;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task NotifyRunningAsync(long userId, string taskId, string label, string? detail = null, int? progress = null, CancellationToken cancellationToken = default)
    {
        return PushAsync(userId, taskId, label, detail, "loading", progress, null, cancellationToken);
    }

    /// <inheritdoc />
    public Task NotifySucceededAsync(long userId, string taskId, string label, string? detail = null, string? link = null, CancellationToken cancellationToken = default)
    {
        return PushAsync(userId, taskId, label, detail, "success", 100, link, cancellationToken);
    }

    /// <inheritdoc />
    public Task NotifyFailedAsync(long userId, string taskId, string label, string? detail = null, CancellationToken cancellationToken = default)
    {
        return PushAsync(userId, taskId, label, detail, "error", null, null, cancellationToken);
    }

    /// <summary>
    /// 统一推送（失败只记日志，绝不阻断业务主流程）
    /// </summary>
    private async Task PushAsync(long userId, string taskId, string label, string? detail, string state, int? progress, string? link, CancellationToken cancellationToken)
    {
        if (userId <= 0 || string.IsNullOrWhiteSpace(taskId))
        {
            return;
        }

        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            await _realtimeNotificationService.SendToUserAsync(
                userId.ToString(),
                SignalRConstants.ClientMethods.TaskProgress,
                new
                {
                    taskId,
                    label,
                    detail,
                    state,
                    progress,
                    link
                });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "任务进度推送失败，UserId={UserId}，TaskId={TaskId}", userId, taskId);
        }
    }
}
