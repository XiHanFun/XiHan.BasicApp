// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Logging;
using SqlSugar;
using XiHan.BasicApp.Saas.Application.Services;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Events;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.EventBus.Abstractions.Local;
using XiHan.Framework.Auditing;
using XiHan.Framework.Auditing.Pipelines;

namespace XiHan.BasicApp.Saas.Application.EventHandlers;

/// <summary>
/// 认证登录事件处理器
/// </summary>
public sealed class AuthLoginEventHandler
    : ILocalEventHandler<AuthLoginSucceededDomainEvent>,
      ILocalEventHandler<AuthLoginFailedDomainEvent>,
      ILocalEventHandler<AuthLogoutDomainEvent>,
      ILocalEventHandler<AuthSecurityAuditDomainEvent>
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
                BuildAuthNotificationContent(
                    "您的账号在另一台设备登录。若非本人操作，请立即修改密码并下线可疑会话。",
                    eventData.LoginTime,
                    FirstNotEmpty(eventData.Location, eventData.IpAddress),
                    eventData.Browser,
                    eventData.OperatingSystem,
                    eventData.DeviceName),
                NotificationType.Security,
                "auth.login.concurrent",
                eventData.SessionRecordId,
                "lucide:shield-alert",
                "/workbench/profile");
            return;
        }

        await DispatchNotificationAsync(
            eventData.UserId,
            "登录成功",
            BuildAuthNotificationContent(
                "您的账号已成功登录。",
                eventData.LoginTime,
                FirstNotEmpty(eventData.Location, eventData.IpAddress),
                eventData.Browser,
                eventData.OperatingSystem,
                eventData.DeviceName),
            NotificationType.Security,
            "auth.login",
            eventData.SessionRecordId,
            "lucide:log-in",
            "/workbench/profile");
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

        // 登出提醒面向用户的其它在线设备（或下次登录时查看），按「其它设备登出」表述；
        // 登出事件本身只携带 IP/UA，设备与位置信息从会话记录补全
        var session = await GetSessionSnapshotAsync(eventData.SessionRecordId);
        await DispatchNotificationAsync(
            eventData.UserId,
            "账号在其它设备登出",
            BuildAuthNotificationContent(
                "您的账号在一台设备上退出登录。",
                eventData.LogoutTime,
                FirstNotEmpty(session?.Location, session?.IpAddress, eventData.IpAddress),
                session?.Browser,
                session?.OperatingSystem,
                session?.DeviceName),
            NotificationType.Security,
            "auth.logout",
            eventData.SessionRecordId,
            "lucide:log-out",
            "/workbench/profile");
    }

    /// <inheritdoc />
    public async Task HandleEventAsync(AuthSecurityAuditDomainEvent eventData)
    {
        ArgumentNullException.ThrowIfNull(eventData);

        // 认证审计事件（令牌刷新/密码修改/密码重置/绑定解绑MFA）统一落登录日志
        await WriteLoginLogAsync(
            eventData.TraceId,
            eventData.UserId,
            eventData.UserName,
            sessionId: null,
            eventData.AuditResult,
            eventData.Message,
            eventData.IpAddress,
            eventData.UserAgent,
            eventData.AuditTime);
    }

    private static string BuildAuthNotificationContent(
            string prefix,
            DateTimeOffset time,
            string? location,
            string? browser,
            string? operatingSystem,
            string? deviceName)
    {
        var parts = new List<string> { prefix, $"时间：{time:yyyy-MM-dd HH:mm:ss} UTC" };

        if (!string.IsNullOrWhiteSpace(location))
        {
            parts.Add($"位置：{location}");
        }

        var device = string.Join(
            " / ",
            new[] { browser, operatingSystem, deviceName }
                .Where(static item => !string.IsNullOrWhiteSpace(item))
                .Distinct(StringComparer.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(device))
        {
            parts.Add($"设备：{device}");
        }

        // 逐行拼接（标题 / 时间 / 位置 / 设备各占一行），前端以 white-space: pre-line 渲染，弹窗与详情更易读
        return string.Join("\n", parts);
    }

    private static string? FirstNotEmpty(params string?[] values)
    {
        return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim();
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

    /// <summary>
    /// 读取会话记录用于补全通知中的设备信息，查询失败时返回 null（不阻塞通知主流程）。
    /// </summary>
    private async Task<SysUserSession?> GetSessionSnapshotAsync(long sessionRecordId)
    {
        try
        {
            var db = _clientResolver.GetCurrentClient();
            return await db.Queryable<SysUserSession>()
                .Where(session => session.BasicId == sessionRecordId)
                .FirstAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "登出通知会话信息查询失败，会话：{SessionRecordId}", sessionRecordId);
            return null;
        }
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
