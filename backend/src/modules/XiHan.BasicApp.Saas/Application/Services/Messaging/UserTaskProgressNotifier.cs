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

    /// <summary>
    /// 推送任务进行中
    /// </summary>
    /// <param name="userId">接收用户</param>
    /// <param name="taskId">任务标识（同 id 复用同一条灵动岛任务）</param>
    /// <param name="label">任务文案</param>
    /// <param name="detail">副文本（可空）</param>
    /// <param name="progress">进度 0-100（空=不确定态）</param>
    /// <param name="cancellationToken">取消令牌</param>
    public Task NotifyRunningAsync(long userId, string taskId, string label, string? detail = null, int? progress = null, CancellationToken cancellationToken = default)
    {
        return PushAsync(userId, taskId, label, detail, "loading", progress, null, cancellationToken);
    }

    /// <summary>
    /// 推送任务成功
    /// </summary>
    /// <param name="userId">接收用户</param>
    /// <param name="taskId">任务标识</param>
    /// <param name="label">任务文案</param>
    /// <param name="detail">副文本（可空）</param>
    /// <param name="link">点击跳转链接（可空）</param>
    /// <param name="cancellationToken">取消令牌</param>
    public Task NotifySucceededAsync(long userId, string taskId, string label, string? detail = null, string? link = null, CancellationToken cancellationToken = default)
    {
        return PushAsync(userId, taskId, label, detail, "success", 100, link, cancellationToken);
    }

    /// <summary>
    /// 推送任务失败
    /// </summary>
    /// <param name="userId">接收用户</param>
    /// <param name="taskId">任务标识</param>
    /// <param name="label">任务文案</param>
    /// <param name="detail">副文本（可空）</param>
    /// <param name="cancellationToken">取消令牌</param>
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
