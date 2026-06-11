#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:UserTaskProgressNotifier
// Guid:c7f3a9e5-2d84-4b61-9e0a-8b5c2f7d4a93
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/06/12 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

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
