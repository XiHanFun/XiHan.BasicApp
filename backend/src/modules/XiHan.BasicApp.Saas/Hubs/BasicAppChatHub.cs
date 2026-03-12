#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:BasicAppChatHub
// Guid:bf2a3b4c-5d6e-4f7a-9c3d-be4f5a6b7c8d
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2024/12/06 05:25:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.AspNetCore.SignalR;
using XiHan.Framework.Web.RealTime.Attributes;
using XiHan.Framework.Web.RealTime.Hubs;
using XiHan.Framework.Web.RealTime.Models;
using XiHan.Framework.Web.RealTime.Services;

namespace XiHan.BasicApp.Saas.Hubs;

/// <summary>
/// 曦寒基础应用聊天 Hub
/// </summary>
[AuthorizeHub]
public class BasicAppChatHub : XiHanHub
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="connectionManager">连接管理器</param>
    public BasicAppChatHub(IConnectionManager connectionManager)
        : base(connectionManager)
    {
    }

    /// <summary>
    /// 连接时触发
    /// </summary>
    /// <returns></returns>
    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();

        // 通知其他用户有新用户上线
        await Clients.Others.SendAsync("UserOnline", new
        {
            UserId,
            UserName,
            Timestamp = DateTime.UtcNow
        });

        // 向当前用户发送欢迎消息
        await Clients.Caller.SendAsync("Welcome", new
        {
            Message = $"欢迎, {UserName}!",
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// 断开连接时触发
    /// </summary>
    /// <param name="exception">异常信息</param>
    /// <returns></returns>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        // 通知其他用户有用户下线
        await Clients.Others.SendAsync("UserOffline", new
        {
            UserId,
            UserName,
            Timestamp = DateTime.UtcNow
        });

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// 发送私聊消息
    /// </summary>
    /// <param name="toUserId">接收者用户 ID</param>
    /// <param name="message">消息内容</param>
    /// <returns></returns>
    public async Task SendPrivateMessage(string toUserId, string message)
    {
        var notificationMessage = new NotificationMessage
        {
            SenderId = UserId,
            ReceiverId = toUserId,
            Type = "PrivateMessage",
            Content = message,
            CreatedTime = DateTime.UtcNow
        };

        // 获取接收者的所有连接
        var connections = await ConnectionManager.GetConnectionsAsync(toUserId);
        if (connections.Count > 0)
        {
            // 发送给接收者
            await Clients.Clients(connections)
                .SendAsync("ReceivePrivateMessage", notificationMessage);

            // 发送给发送者（所有设备）
            var senderConnections = await ConnectionManager.GetConnectionsAsync(UserId!);
            await Clients.Clients(senderConnections)
                .SendAsync("MessageSent", notificationMessage);
        }
        else
        {
            // 用户不在线，返回错误
            await Clients.Caller.SendAsync("Error", new
            {
                Message = "用户不在线",
                UserId = toUserId
            });
        }
    }

    /// <summary>
    /// 发送群聊消息
    /// </summary>
    /// <param name="roomId">房间 ID</param>
    /// <param name="message">消息内容</param>
    /// <returns></returns>
    public async Task SendRoomMessage(string roomId, string message)
    {
        var notificationMessage = new NotificationMessage
        {
            SenderId = UserId,
            Type = "RoomMessage",
            Content = message,
            Data = new { RoomId = roomId },
            CreatedTime = DateTime.UtcNow
        };

        await Clients.Group(roomId).SendAsync("ReceiveRoomMessage", notificationMessage);
    }

    /// <summary>
    /// 加入聊天室
    /// </summary>
    /// <param name="roomId">房间 ID</param>
    /// <returns></returns>
    public async Task JoinRoom(string roomId)
    {
        await Groups.AddToGroupAsync(ConnectionId!, roomId);

        // 通知房间内的其他用户
        await Clients.OthersInGroup(roomId).SendAsync("UserJoinedRoom", new
        {
            UserId,
            UserName,
            RoomId = roomId,
            Timestamp = DateTime.UtcNow
        });

        // 向当前用户确认加入成功
        await Clients.Caller.SendAsync("JoinedRoom", new
        {
            RoomId = roomId,
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// 离开聊天室
    /// </summary>
    /// <param name="roomId">房间 ID</param>
    /// <returns></returns>
    public async Task LeaveRoom(string roomId)
    {
        await Groups.RemoveFromGroupAsync(ConnectionId!, roomId);

        // 通知房间内的其他用户
        await Clients.OthersInGroup(roomId).SendAsync("UserLeftRoom", new
        {
            UserId,
            UserName,
            RoomId = roomId,
            Timestamp = DateTime.UtcNow
        });

        // 向当前用户确认离开成功
        await Clients.Caller.SendAsync("LeftRoom", new
        {
            RoomId = roomId,
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// 输入中状态
    /// </summary>
    /// <param name="roomId">房间 ID</param>
    /// <returns></returns>
    public async Task Typing(string roomId)
    {
        await Clients.OthersInGroup(roomId).SendAsync("UserTyping", new
        {
            UserId,
            UserName,
            RoomId = roomId,
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// 获取在线用户列表
    /// </summary>
    /// <returns></returns>
    public async Task<IReadOnlyList<string>> GetOnlineUsers()
    {
        return await ConnectionManager.GetOnlineUsersAsync();
    }

    /// <summary>
    /// 检查用户是否在线
    /// </summary>
    /// <param name="userId">用户 ID</param>
    /// <returns></returns>
    public async Task<bool> CheckUserOnline(string userId)
    {
        return await ConnectionManager.IsUserOnlineAsync(userId);
    }
}
