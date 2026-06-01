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

        await DispatchNotificationAsync(
            eventData.UserId,
            "登录成功",
            BuildAuthNotificationContent("您的账号已成功登录。", eventData),
            NotificationType.User,
            "auth.login",
            eventData.SessionRecordId,
            "lucide:log-in",
            "/workbench/profile");

        // 新设备登录安全告警：当前设备/IP 此前从未成功登录过时额外提醒
        if (await IsNewDeviceLoginAsync(eventData))
        {
            await DispatchNotificationAsync(
                eventData.UserId,
                "新设备登录提醒",
                BuildAuthNotificationContent("检测到您的账号在新设备或新位置登录。如非本人操作，请立即修改密码。", eventData),
                NotificationType.Warning,
                "auth.login.new_device",
                eventData.SessionRecordId,
                "lucide:shield-alert",
                "/workbench/profile");
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

    /// <summary>
    /// 判定是否新设备登录：当前 (IP + 浏览器 + 操作系统) 组合在本次登录之前从未成功登录过。
    /// 首次登录（无任何历史成功记录）不视为"新设备告警"，避免每个新用户首登都收告警。
    /// </summary>
    private async Task<bool> IsNewDeviceLoginAsync(AuthLoginSucceededDomainEvent eventData)
    {
        try
        {
            var client = _clientResolver.GetCurrentClient();
            // 本次登录时间之前的成功登录历史（排除当前这条，规避写入时序）
            var history = await client.Queryable<SysLoginLog>()
                .Where(log => log.UserId == eventData.UserId
                    && log.LoginResult == LoginResult.Success
                    && log.LoginTime < eventData.LoginTime)
                .SplitTable()
                .Select(log => new { log.LoginIp, log.Browser, log.Os })
                .ToListAsync();

            // 无历史成功登录 = 首次登录，不告警
            if (history.Count == 0)
            {
                return false;
            }

            // 当前设备指纹此前是否出现过
            return !history.Any(log =>
                string.Equals(log.LoginIp, eventData.IpAddress, StringComparison.OrdinalIgnoreCase)
                && string.Equals(log.Browser, eventData.Browser, StringComparison.OrdinalIgnoreCase)
                && string.Equals(log.Os, eventData.OperatingSystem, StringComparison.OrdinalIgnoreCase));
        }
        catch (Exception ex)
        {
            // 判定失败不阻断登录流程，且不误发告警
            _logger.LogWarning(ex, "新设备登录判定失败，用户：{UserId}", eventData.UserId);
            return false;
        }
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
