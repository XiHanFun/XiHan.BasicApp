#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:NotificationAppService
// Guid:0982e8eb-7416-4d39-b831-597c10e97f69
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/06 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using XiHan.BasicApp.Saas.Application.Contracts;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.Mappers;
using XiHan.BasicApp.Saas.Application.Services;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Permissions;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.BasicApp.Saas.Hubs;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Authorization.AspNetCore;
using XiHan.Framework.Security.Users;
using XiHan.Framework.Uow.Attributes;
using XiHan.Framework.Web.RealTime.Constants;
using XiHan.Framework.Web.RealTime.Services;

namespace XiHan.BasicApp.Saas.Application.AppServices;

/// <summary>
/// 系统通知命令应用服务
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "系统通知")]
public sealed class NotificationAppService
    : SaasApplicationService, INotificationAppService
{
    private readonly INotificationDomainService _notificationDomainService;

    private readonly IMessageTemplateRenderer _messageTemplateRenderer;

    private readonly IRealtimeNotificationService<BasicAppNotificationHub> _realtimeNotificationService;

    private readonly IUserTaskProgressNotifier _taskProgressNotifier;

    private readonly ICurrentUser _currentUser;

    private readonly INotificationRepository _notificationRepository;

    private readonly IUserNotificationRepository _userNotificationRepository;

    private readonly ILogger<NotificationAppService> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    public NotificationAppService(
        INotificationDomainService notificationDomainService,
        IMessageTemplateRenderer messageTemplateRenderer,
        IRealtimeNotificationService<BasicAppNotificationHub> realtimeNotificationService,
        IUserTaskProgressNotifier taskProgressNotifier,
        ICurrentUser currentUser,
        INotificationRepository notificationRepository,
        IUserNotificationRepository userNotificationRepository,
        ILogger<NotificationAppService> logger)
    {
        _notificationDomainService = notificationDomainService;
        _messageTemplateRenderer = messageTemplateRenderer;
        _realtimeNotificationService = realtimeNotificationService;
        _taskProgressNotifier = taskProgressNotifier;
        _currentUser = currentUser;
        _notificationRepository = notificationRepository;
        _userNotificationRepository = userNotificationRepository;
        _logger = logger;
    }

    /// <summary>
    /// 创建系统通知
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Message.Create)]
    public async Task<NotificationDetailDto> CreateNotificationAsync(NotificationCreateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        // 渲染前置：提供模板编码时按 站内通知 渠道渲染（租户模板优先回退全局），
        // 标题取模板 Subject、内容取模板 Content；模板缺失/停用/损坏回退调用方传入值
        if (!string.IsNullOrWhiteSpace(input.TemplateCode))
        {
            var variables = (input.TemplateParams ?? []).ToDictionary(kvp => kvp.Key, kvp => (object?)kvp.Value);
            var rendered = await _messageTemplateRenderer.RenderAsync(MessageChannel.SiteNotification, input.TemplateCode, variables, cancellationToken);
            if (rendered is not null)
            {
                input.Content = rendered.Content;
                if (!string.IsNullOrWhiteSpace(rendered.Subject))
                {
                    input.Title = rendered.Subject;
                }
            }
        }

        var result = await _notificationDomainService.CreateNotificationAsync(NotificationApplicationMapper.ToCreateCommand(input), cancellationToken);
        return NotificationApplicationMapper.ToDetailDto(result.Notification);
    }

    /// <summary>
    /// 删除系统通知
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Message.Delete)]
    public async Task DeleteNotificationAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _notificationDomainService.DeleteNotificationAsync(id, cancellationToken);
    }

    /// <summary>
    /// 发布系统通知
    /// </summary>
    /// <remarks>
    /// 发布落库后：① 给在线接收者实时推送 ReceiveNotification（收件箱即时刷新，无需等下次拉取）；
    /// ② 给发布者经 TaskProgress 推送任务进度（灵动岛呈现）。推送失败均不影响发布结果。
    /// </remarks>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Message.Publish)]
    public async Task<NotificationPublishResultDto> PublishNotificationAsync(NotificationPublishDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var publisherId = _currentUser.UserId ?? 0;
        var progressTaskId = $"notification-publish:{input.BasicId}";
        await _taskProgressNotifier.NotifyRunningAsync(publisherId, progressTaskId, "正在发布公告…", cancellationToken: cancellationToken);

        NotificationPublishCommandResult result;
        try
        {
            result = await _notificationDomainService.PublishNotificationAsync(NotificationApplicationMapper.ToPublishCommand(input), cancellationToken);
        }
        catch (Exception ex)
        {
            await _taskProgressNotifier.NotifyFailedAsync(publisherId, progressTaskId, "公告发布失败", ex.Message, cancellationToken);
            throw;
        }

        await PushPublishedNotificationAsync(result.Notification);
        await _taskProgressNotifier.NotifySucceededAsync(
            publisherId,
            progressTaskId,
            "公告已发布",
            $"已发送给 {result.RecipientCount} 位用户",
            link: "/message/notification",
            cancellationToken: cancellationToken);

        return NotificationApplicationMapper.ToPublishResultDto(result);
    }

    /// <summary>
    /// 更新系统通知
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Message.Update)]
    public async Task<NotificationDetailDto> UpdateNotificationAsync(NotificationUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _notificationDomainService.UpdateNotificationAsync(NotificationApplicationMapper.ToUpdateCommand(input), cancellationToken);
        return NotificationApplicationMapper.ToDetailDto(result.Notification);
    }

    /// <summary>
    /// 催办：对未读人员重新实时推送（不改库；在线者即时再提醒）
    /// </summary>
    [PermissionAuthorize(SaasPermissionCodes.Message.Publish)]
    public async Task<NotificationPublishResultDto> RemindAsync(long id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "系统通知主键必须大于 0。");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var notification = await _notificationRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("系统通知不存在。");
        var unread = await _userNotificationRepository.GetListAsync(
            item => item.NotificationId == id && item.NotificationStatus == NotificationStatus.Unread,
            cancellationToken);
        var unreadUserIds = unread.Select(item => item.UserId).Distinct().ToArray();
        if (unreadUserIds.Length > 0)
        {
            try
            {
                var payload = new
                {
                    type = (int)notification.NotificationType,
                    title = notification.Title,
                    content = notification.Content,
                    basicId = notification.BasicId,
                    notificationId = notification.BasicId,
                    notificationType = (int)notification.NotificationType,
                    sendTime = notification.SendTime
                };
                await _realtimeNotificationService.SendToUsersAsync(
                    [.. unreadUserIds.Select(userId => userId.ToString())],
                    SignalRConstants.ClientMethods.ReceiveNotification,
                    payload);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "催办实时推送失败，NotificationId={NotificationId}", notification.BasicId);
            }
        }

        return new NotificationPublishResultDto
        {
            BasicId = id,
            RecipientCount = unreadUserIds.Length,
            SendTime = DateTimeOffset.UtcNow
        };
    }

    /// <summary>
    /// 给在线接收者实时推送已发布公告（全员目标广播，指定用户目标点发；失败只记日志）
    /// </summary>
    private async Task PushPublishedNotificationAsync(SysNotification notification)
    {
        try
        {
            var payload = new
            {
                type = (int)notification.NotificationType,
                title = notification.Title,
                content = notification.Content,
                basicId = notification.BasicId,
                notificationId = notification.BasicId,
                notificationType = (int)notification.NotificationType,
                sendTime = notification.SendTime
            };

            if (notification.TargetType == NotificationTargetType.All)
            {
                await _realtimeNotificationService.SendToAllAsync(SignalRConstants.ClientMethods.ReceiveNotification, payload);
                return;
            }

            if (!string.IsNullOrWhiteSpace(notification.TargetValue))
            {
                var userIds = JsonSerializer.Deserialize<long[]>(notification.TargetValue) ?? [];
                if (userIds.Length > 0)
                {
                    await _realtimeNotificationService.SendToUsersAsync(
                        [.. userIds.Select(id => id.ToString())],
                        SignalRConstants.ClientMethods.ReceiveNotification,
                        payload);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "公告实时推送失败，NotificationId={NotificationId}", notification.BasicId);
        }
    }
}
