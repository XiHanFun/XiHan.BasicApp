#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:UserSessionRevokedEventHandler
// Guid:3c4d5e6f-7a8b-9012-cdef-123456789012
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/12 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Events;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.EventBus.Abstractions.Local;

namespace XiHan.BasicApp.Saas.Application.EventHandlers;

/// <summary>
/// 用户会话撤销事件处理器
/// </summary>
/// <remarks>
/// 当用户会话被撤销时，写入登录日志并发送通知给用户。
/// </remarks>
public sealed class UserSessionRevokedEventHandler : ILocalEventHandler<UserSessionRevokedDomainEvent>
{
    private readonly ISqlSugarClientResolver _clientResolver;
    private readonly ILogger<UserSessionRevokedEventHandler> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    public UserSessionRevokedEventHandler(
        ISqlSugarClientResolver clientResolver,
        ILogger<UserSessionRevokedEventHandler> logger)
    {
        _clientResolver = clientResolver ?? throw new ArgumentNullException(nameof(clientResolver));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 处理用户会话撤销事件
    /// </summary>
    /// <param name="eventData">事件数据</param>
    public async Task HandleEventAsync(UserSessionRevokedDomainEvent eventData)
    {
        ArgumentNullException.ThrowIfNull(eventData);

        _logger.LogInformation(
            "[UserSessionRevoked] User {UserId} session revoked (sessionId: {SessionId}, jti: {AccessTokenJti}, revokeAll: {RevokeAll}), reason: {Reason}",
            eventData.UserId, eventData.SessionId?.ToString() ?? eventData.UserSessionId, eventData.AccessTokenJti,
            eventData.RevokeAllUserSessions, eventData.Reason);

        // 写入登录/登出日志
        await WriteLoginLogAsync(eventData);

        // 发送通知给用户
        await SendNotificationAsync(eventData);
    }

    /// <summary>
    /// 写入会话撤销日志
    /// </summary>
    private async Task WriteLoginLogAsync(UserSessionRevokedDomainEvent eventData)
    {
        try
        {
            var db = _clientResolver.GetCurrentClient();
            var now = DateTimeOffset.UtcNow;
            var sessionId = eventData.SessionId?.ToString() ?? eventData.UserSessionId;

            // 查询用户信息以获取用户名
            var user = await db.Queryable<SysUser>()
                .Where(u => u.BasicId == eventData.UserId && u.TenantId == eventData.TenantId && !u.IsDeleted)
                .Select(u => new { u.UserName })
                .FirstAsync();

            var log = new SysLoginLog
            {
                UserId = eventData.UserId,
                UserName = user?.UserName,
                SessionId = sessionId,
                LoginResult = LoginResult.Logout, // 登出/撤销
                LoginTime = now,
                Message = string.IsNullOrEmpty(eventData.Reason)
                    ? "Session revoked"
                    : $"Session revoked: {eventData.Reason}",
                TenantId = eventData.TenantId,
                CreatedTime = now
            };

            // SysLoginLog 是按月分表，使用 SplitTable 插入
            await db.Insertable(log).SplitTable().ExecuteCommandAsync();

            _logger.LogDebug(
                "[UserSessionRevoked] Login log written for user {UserId}, session {SessionId}",
                eventData.UserId, sessionId);
        }
        catch (Exception ex)
        {
            // 日志写入失败不应阻塞主流程
            _logger.LogError(ex,
                "[UserSessionRevoked] Failed to write login log for user {UserId}", eventData.UserId);
        }
    }

    /// <summary>
    /// 向用户发送会话撤销通知
    /// </summary>
    private async Task SendNotificationAsync(UserSessionRevokedDomainEvent eventData)
    {
        try
        {
            var db = _clientResolver.GetCurrentClient();
            var now = DateTimeOffset.UtcNow;

            var notification = new SysNotification
            {
                NotificationType = NotificationType.User,
                Title = "会话已撤销",
                Content = string.IsNullOrEmpty(eventData.Reason)
                    ? "您的登录会话已被管理员撤销，请重新登录。"
                    : $"您的登录会话已被撤销，原因：{eventData.Reason}。请重新登录或联系平台管理员。",
                SendUserId = eventData.OperatorUserId,
                SendTime = now,
                TargetType = NotificationTargetType.User,
                TargetValue = eventData.UserId.ToString(),
                IsPublished = true,
                IsBroadcast = false,
                TenantId = eventData.TenantId
            };

            var notificationId = await db.Insertable(notification).ExecuteReturnBigIdentityAsync();

            // 创建用户通知关联
            var userNotification = new SysUserNotification
            {
                NotificationId = notificationId,
                UserId = eventData.UserId,
                NotificationStatus = NotificationStatus.Unread,
                TenantId = eventData.TenantId
            };

            await db.Insertable(userNotification).ExecuteCommandAsync();

            _logger.LogDebug(
                "[UserSessionRevoked] Notification sent to user {UserId}, notificationId: {NotificationId}",
                eventData.UserId, notificationId);
        }
        catch (Exception ex)
        {
            // 通知发送失败不应阻塞主流程
            _logger.LogError(ex,
                "[UserSessionRevoked] Failed to send notification to user {UserId}", eventData.UserId);
        }
    }
}
