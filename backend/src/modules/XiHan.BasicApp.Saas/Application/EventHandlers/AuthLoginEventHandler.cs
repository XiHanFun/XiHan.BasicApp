#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:AuthLoginEventHandler
// Guid:101aa7c8-b68d-4b8e-8c18-58e21c3ce0f1
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/17 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.Extensions.Logging;
using SqlSugar;
using XiHan.BasicApp.Saas.Application.Services;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Events;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.EventBus.Abstractions.Local;
using XiHan.Framework.Web.Api.Logging;
using XiHan.Framework.Web.Api.Logging.Pipelines;

namespace XiHan.BasicApp.Saas.Application.EventHandlers;

/// <summary>
/// 认证登录事件处理器
/// </summary>
public sealed class AuthLoginEventHandler
    : ILocalEventHandler<AuthLoginSucceededDomainEvent>,
      ILocalEventHandler<AuthLoginFailedDomainEvent>,
      ILocalEventHandler<AuthLogoutDomainEvent>
{
    private readonly ILoginLogPipeline _loginLogPipeline;

    private readonly IUserNotificationDispatchService _notificationDispatchService;

    private readonly ISqlSugarClientResolver _clientResolver;

    private readonly ILogger<AuthLoginEventHandler> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    public AuthLoginEventHandler(
        ILoginLogPipeline loginLogPipeline,
        IUserNotificationDispatchService notificationDispatchService,
        ISqlSugarClientResolver clientResolver,
        ILogger<AuthLoginEventHandler> logger)
    {
        _loginLogPipeline = loginLogPipeline;
        _notificationDispatchService = notificationDispatchService;
        _clientResolver = clientResolver;
        _logger = logger;
    }
    /// <inheritdoc />
    public async Task HandleEventAsync(AuthLoginSucceededDomainEvent eventData)
    {
        ArgumentNullException.ThrowIfNull(eventData);

        await WriteLoginLogAsync(
            eventData.TraceId,
            eventData.UserId,
            eventData.UserName,
            eventData.SessionBusinessId,
            LoginResult.Success,
            "登录成功",
            eventData.IpAddress,
            eventData.UserAgent,
            eventData.LoginTime);

        // 多设备登录提醒：存在其它活跃会话时升级为告警通知（实时推送会到达其它在线设备），
        // 并区分「新设备」与「已知设备」；无其它会话时保持普通登录成功通知。
        var concurrent = await DetectConcurrentLoginAsync(eventData);
        if (concurrent.HasOtherActiveSessions)
        {
            var title = concurrent.IsKnownDevice ? "账号在其它设备登录" : "账号在新设备登录";
            await DispatchNotificationAsync(
                eventData.UserId,
                title,
                BuildAuthNotificationContent("您的账号刚刚在另一台设备登录。若非本人操作，请立即修改密码并下线可疑会话。", eventData),
                NotificationType.Warning,
                "auth.login.concurrent",
                eventData.SessionRecordId,
                "lucide:shield-alert",
                "/workbench/profile");
            return;
        }

        await DispatchNotificationAsync(
            eventData.UserId,
            "登录成功",
            BuildAuthNotificationContent("您的账号已成功登录。", eventData),
            NotificationType.User,
            "auth.login",
            eventData.SessionRecordId,
            "lucide:log-in",
            "/workbench/profile");
    }

    /// <summary>
    /// 检测并发登录：是否存在其它活跃会话、本次登录设备是否曾经使用过。
    /// 查询失败时按「无其它会话」处理，不阻塞登录主流程。
    /// </summary>
    private async Task<(bool HasOtherActiveSessions, bool IsKnownDevice)> DetectConcurrentLoginAsync(AuthLoginSucceededDomainEvent eventData)
    {
        try
        {
            var db = _clientResolver.GetCurrentClient();

            var otherActiveCount = await db.Queryable<SysUserSession>()
                .Where(session => session.UserId == eventData.UserId
                    && session.Status == SessionStatus.Active
                    && session.BasicId != eventData.SessionRecordId)
                .CountAsync();
            if (otherActiveCount <= 0)
            {
                return (false, false);
            }

            // 设备识别：本次会话的 DeviceId 此前出现过 → 已知设备
            var currentDeviceId = await db.Queryable<SysUserSession>()
                .Where(session => session.BasicId == eventData.SessionRecordId)
                .Select(session => session.DeviceId)
                .FirstAsync();
            var isKnownDevice = !string.IsNullOrWhiteSpace(currentDeviceId)
                && await db.Queryable<SysUserSession>()
                    .Where(session => session.UserId == eventData.UserId
                        && session.BasicId != eventData.SessionRecordId
                        && session.DeviceId == currentDeviceId)
                    .AnyAsync();

            return (true, isKnownDevice);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "并发登录检测失败，用户：{UserId}", eventData.UserId);
            return (false, false);
        }
    }

    /// <inheritdoc />
    public async Task HandleEventAsync(AuthLoginFailedDomainEvent eventData)
    {
        ArgumentNullException.ThrowIfNull(eventData);

        await WriteLoginLogAsync(
            eventData.TraceId,
            eventData.UserId,
            eventData.UserName,
            sessionId: null,
            eventData.LoginResult,
            eventData.Message,
            eventData.IpAddress,
            eventData.UserAgent,
            eventData.LoginTime);
    }

    /// <inheritdoc />
    public async Task HandleEventAsync(AuthLogoutDomainEvent eventData)
    {
        ArgumentNullException.ThrowIfNull(eventData);

        await WriteLoginLogAsync(
            eventData.TraceId,
            eventData.UserId,
            eventData.UserName,
            eventData.SessionBusinessId,
            LoginResult.Logout,
            "用户主动退出",
            eventData.IpAddress,
            eventData.UserAgent,
            eventData.LogoutTime);

        await DispatchNotificationAsync(
            eventData.UserId,
            "退出登录",
            "您已退出当前会话。",
            NotificationType.User,
            "auth.logout",
            eventData.SessionRecordId,
            "lucide:log-out",
            "/workbench/profile");
    }

    private static string BuildAuthNotificationContent(string prefix, AuthLoginSucceededDomainEvent eventData)
    {
        var parts = new List<string> { prefix, $"时间：{eventData.LoginTime:yyyy-MM-dd HH:mm:ss} UTC" };

        var location = FirstNotEmpty(eventData.Location, eventData.IpAddress);
        if (!string.IsNullOrWhiteSpace(location))
        {
            parts.Add($"位置：{location}");
        }

        var device = string.Join(
            " / ",
            new[] { eventData.Browser, eventData.OperatingSystem, eventData.DeviceName }
                .Where(static item => !string.IsNullOrWhiteSpace(item))
                .Distinct(StringComparer.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(device))
        {
            parts.Add($"设备：{device}");
        }

        return string.Join(" ", parts);
    }

    private static string? FirstNotEmpty(params string?[] values)
    {
        return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim();
    }

    private async Task WriteLoginLogAsync(
                string? traceId,
        long? userId,
        string? userName,
        string? sessionId,
        LoginResult loginResult,
        string? message,
        string? loginIp,
        string? userAgent,
        DateTimeOffset loginTime)
    {
        try
        {
            var record = new LoginLogRecord
            {
                TraceId = traceId,
                UserId = userId,
                UserName = userName,
                SessionId = sessionId,
                LoginResult = (int)loginResult,
                Message = message,
                LoginIp = loginIp,
                UserAgent = userAgent,
                LoginTime = loginTime
            };

            await _loginLogPipeline.WriteAsync(record);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "认证登录日志写入失败，用户：{UserId}，结果：{LoginResult}", userId, loginResult);
        }
    }

    private async Task DispatchNotificationAsync(
        long userId,
        string title,
        string content,
        NotificationType notificationType,
        string businessType,
        long businessId,
        string icon,
        string link)
    {
        try
        {
            await _notificationDispatchService.DispatchToUserAsync(
                userId,
                title,
                content,
                notificationType,
                businessType,
                businessId,
                link: link,
                icon: icon);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "认证通知发送失败，用户：{UserId}，业务：{BusinessType}", userId, businessType);
        }
    }
}
