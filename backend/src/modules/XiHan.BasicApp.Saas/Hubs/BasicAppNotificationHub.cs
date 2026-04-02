#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:BasicAppNotificationHub
// Guid:df3a4b5c-6d7e-4f8a-9b1c-ce5f6a7b8c9d
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/02 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.AspNetCore.SignalR;
using XiHan.Framework.Web.RealTime.Attributes;
using XiHan.Framework.Web.RealTime.Hubs;
using XiHan.Framework.Web.RealTime.Services;

namespace XiHan.BasicApp.Saas.Hubs;

/// <summary>
/// 曦寒基础应用通知 Hub（实时通知 + 踢下线）
/// </summary>
[AuthorizeHub]
public class BasicAppNotificationHub : XiHanHub
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="connectionManager">连接管理器</param>
    public BasicAppNotificationHub(IConnectionManager connectionManager)
        : base(connectionManager)
    {
    }

    /// <summary>
    /// 连接时触发
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();

        // 通知当前用户连接成功
        await Clients.Caller.SendAsync("Connected", new
        {
            UserId,
            Timestamp = DateTime.UtcNow
        });
    }
}
