#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IAuthNotificationService
// Guid:bc864afd-8932-4673-966b-159b336b1242
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/05 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.Framework.Web.Core.Clients;

namespace XiHan.BasicApp.Saas.Application.AppServices;

/// <summary>
/// 认证通知服务（SignalR 推送的基础设施端口）
/// </summary>
public interface IAuthNotificationService
{
    /// <summary>
    /// 通知用户有新设备登录
    /// </summary>
    Task NotifyNewDeviceLoginAsync(string userId, ClientInfo clientInfo);

    /// <summary>
    /// 通知用户有设备登出
    /// </summary>
    Task NotifyDeviceLogoutAsync(string userId, ClientInfo clientInfo);

    /// <summary>
    /// 向指定用户推送强制下线消息
    /// </summary>
    Task NotifyForceLogoutAsync(string userId, string reason, IReadOnlyCollection<string>? targetSessionIds = null);
}
