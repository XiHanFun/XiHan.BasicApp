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

using XiHan.BasicApp.Saas.Application.Caching;
using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Saas.Application.Services;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Events;
using XiHan.BasicApp.Saas.Hubs;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.EventBus.Abstractions.Local;
using XiHan.Framework.Web.RealTime.Constants;
using XiHan.Framework.Web.RealTime.Services;

namespace XiHan.BasicApp.Saas.Application.EventHandlers;

/// <summary>
/// 用户会话撤销事件处理器
/// </summary>
/// <remarks>
/// 当用户会话被撤销时，写入登录日志、发送通知给用户，并向其在线连接实时推送 ForceLogout 强制下线。
/// </remarks>
public sealed class UserSessionRevokedEventHandler : ILocalEventHandler<UserSessionRevokedDomainEvent>
{
    private readonly ISqlSugarClientResolver _clientResolver;
    private readonly IRealtimeNotificationService<BasicAppNotificationHub> _realtimeNotificationService;
    private readonly IUserNotificationDispatchService _notificationDispatchService;
    private readonly ISaasCacheInvalidator _cacheInvalidator;

    private readonly ILogger<UserSessionRevokedEventHandler> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    public UserSessionRevokedEventHandler(
        ISqlSugarClientResolver clientResolver,
        IRealtimeNotificationService<BasicAppNotificationHub> realtimeNotificationService,
        IUserNotificationDispatchService notificationDispatchService,
        ISaasCacheInvalidator cacheInvalidator,
        ILogger<UserSessionRevokedEventHandler> logger)
    {
        _clientResolver = clientResolver ?? throw new ArgumentNullException(nameof(clientResolver));
        _realtimeNotificationService = realtimeNotificationService ?? throw new ArgumentNullException(nameof(realtimeNotificationService));
        _notificationDispatchService = notificationDispatchService ?? throw new ArgumentNullException(nameof(notificationDispatchService));
        _cacheInvalidator = cacheInvalidator ?? throw new ArgumentNullException(nameof(cacheInvalidator));
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

        // 会话状态缓存失效——这一刀决定了踢下线是不是**真的踢得掉**：
        // 会话闸门每请求读这份缓存，不清就得等 TTL 到期才生效。
        // 注意 SignalR 那条只是"通知前端自己登出"，纯客户端行为，curl 绕过即可；
        // 服务端硬拦截靠的是闸门读到 Status=Revoked。
        await InvalidateSessionCacheAsync(eventData);

        // 写入登录/登出日志
        await WriteLoginLogAsync(eventData);

        // 发送通知给用户
        await SendNotificationAsync(eventData);

        // 实时推送强制下线（被踢设备立即登出，而非等到下次请求才失效）
        await PushForceLogoutAsync(eventData);
    }

    /// <summary>
    /// 失效被撤销会话的状态缓存（撤销全部会话时整体清空）
    /// </summary>
    private async Task InvalidateSessionCacheAsync(UserSessionRevokedDomainEvent eventData)
    {
        try
        {
            if (eventData.RevokeAllUserSessions || string.IsNullOrWhiteSpace(eventData.UserSessionId))
            {
                await _cacheInvalidator.InvalidateAllSessionStatesAsync();
                return;
            }

            await _cacheInvalidator.InvalidateSessionStateAsync(eventData.UserSessionId);
        }
        catch (Exception ex)
        {
            // 失效失败不阻断撤销主流程：缓存有 60s 短 TTL 兜底，最迟 60 秒后闸门也会读到 Revoked
            _logger.LogError(ex, "[UserSessionRevoked] 会话状态缓存失效失败：{SessionId}", eventData.UserSessionId);
        }
    }

    /// <summary>
    /// 向用户在线连接推送 ForceLogout。
    /// 单会话撤销带 targetSessionIds（前端按 JWT session_id 匹配，仅目标会话登出）；
    /// 全部撤销不带目标列表（该用户所有在线连接立即登出）。
    /// </summary>
    /// <remarks>
    /// 这只是"通知前端自己登出"——<b>纯客户端行为，忽略推送或直接 curl 即可绕过</b>。
    /// 服务端的硬拦截靠 <c>XiHanSessionStateMiddleware</c> + <c>SaasSessionStateGate</c> 读到 Status=Revoked 后 401。
    /// </remarks>
    private async Task PushForceLogoutAsync(UserSessionRevokedDomainEvent eventData)
    {
        try
        {
            var payload = new
            {
                reason = string.IsNullOrWhiteSpace(eventData.Reason) ? "您的登录会话已被管理员撤销。" : eventData.Reason,
                targetSessionIds = eventData.RevokeAllUserSessions || string.IsNullOrWhiteSpace(eventData.UserSessionId)
                    ? null
                    : new[] { eventData.UserSessionId },
            };
            await _realtimeNotificationService.SendToUserAsync(
                eventData.UserId.ToString(),
                SignalRConstants.ClientMethods.ForceLogout,
                payload);
        }
        catch (Exception ex)
        {
            // 实时推送失败不影响撤销主流程（令牌校验仍会拒绝已撤销会话）
            _logger.LogError(ex,
                "[UserSessionRevoked] Failed to push ForceLogout to user {UserId}", eventData.UserId);
        }
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
            // 走统一的站内信投递（与登录/登出通知同一条路）：
            await _notificationDispatchService.DispatchToUserAsync(
                eventData.UserId,
                "会话已撤销",
                string.IsNullOrEmpty(eventData.Reason)
                    ? "您的登录会话已被撤销，请重新登录。"
                    : $"您的登录会话已被撤销，原因：{eventData.Reason}。请重新登录或联系平台管理员。",
                NotificationType.Security,
                "auth.session.revoked",
                eventData.SessionId,
                icon: "lucide:shield-alert",
                link: "/workbench/profile");

            _logger.LogDebug(
                "[UserSessionRevoked] Notification sent to user {UserId}, session {SessionId}",
                eventData.UserId, eventData.SessionId?.ToString() ?? eventData.UserSessionId);
        }
        catch (Exception ex)
        {
            // 通知发送失败不应阻塞主流程
            _logger.LogError(ex,
                "[UserSessionRevoked] Failed to send notification to user {UserId}", eventData.UserId);
        }
    }
}
