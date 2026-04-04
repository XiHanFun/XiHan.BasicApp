#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:AuthNotificationService
// Guid:b2c3d4e5-f6a7-8901-bcde-notify00002
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/05 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Hubs;
using XiHan.Framework.Web.Core.Clients;
using XiHan.Framework.Web.RealTime.Constants;
using XiHan.Framework.Web.RealTime.Services;

namespace XiHan.BasicApp.Saas.Application.AppServices.Implementations;

/// <summary>
/// 认证通知服务实现（基于 SignalR）
/// </summary>
public class AuthNotificationService(
    IRealtimeNotificationService<BasicAppNotificationHub> realtimeNotifier) : IAuthNotificationService
{
    /// <inheritdoc />
    public async Task NotifyNewDeviceLoginAsync(string userId, ClientInfo clientInfo)
    {
        try
        {
            var deviceDesc = !string.IsNullOrWhiteSpace(clientInfo.Browser)
                ? $"{clientInfo.Browser} ({clientInfo.OperatingSystem})"
                : clientInfo.OperatingSystem ?? "未知设备";

            await realtimeNotifier.SendToUserAsync(
                userId,
                SignalRConstants.ClientMethods.ReceiveNotification,
                new
                {
                    Type = "Warning",
                    Title = "新设备登录",
                    Content = $"您的账号刚刚在新设备上登录：{deviceDesc}，IP：{clientInfo.IpAddress ?? "未知"}。如非本人操作，请及时修改密码。"
                });
        }
        catch
        {
            // 推送失败不影响登录流程
        }
    }

    /// <inheritdoc />
    public async Task NotifyDeviceLogoutAsync(string userId, ClientInfo clientInfo)
    {
        try
        {
            var deviceDesc = !string.IsNullOrWhiteSpace(clientInfo.Browser)
                ? $"{clientInfo.Browser} ({clientInfo.OperatingSystem})"
                : clientInfo.OperatingSystem ?? "未知设备";

            await realtimeNotifier.SendToUserAsync(
                userId,
                SignalRConstants.ClientMethods.ReceiveNotification,
                new
                {
                    Type = "Info",
                    Title = "设备已登出",
                    Content = $"您的账号已在一台设备上登出：{deviceDesc}，IP：{clientInfo.IpAddress ?? "未知"}。"
                });
        }
        catch
        {
            // 推送失败不影响登出流程
        }
    }

    /// <inheritdoc />
    public async Task NotifyForceLogoutAsync(string userId, string reason, IReadOnlyCollection<string>? targetSessionIds = null)
    {
        try
        {
            await realtimeNotifier.SendToUserAsync(
                userId,
                SignalRConstants.ClientMethods.ForceLogout,
                new { Reason = reason, Timestamp = DateTimeOffset.UtcNow, TargetSessionIds = targetSessionIds });
        }
        catch
        {
            // SignalR 推送失败不影响主流程
        }
    }
}
