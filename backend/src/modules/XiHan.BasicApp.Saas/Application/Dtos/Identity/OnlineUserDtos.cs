// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 在线用户分页查询 DTO
/// </summary>
public sealed class OnlineUserPageQueryDto : BasicAppPRDto
{
    /// <summary>
    /// 关键字（匹配会话标识/设备名称/操作系统/浏览器）
    /// </summary>
    public string? Keyword { get; set; }

    /// <summary>
    /// 用户主键
    /// </summary>
    public long? UserId { get; set; }

    /// <summary>
    /// 设备类型
    /// </summary>
    public DeviceType? DeviceType { get; set; }
}

/// <summary>
/// 在线用户列表项 DTO
/// </summary>
/// <remarks>
/// 一行 = 一个活跃会话（同一用户多端登录会出现多行）；
/// IsRealtimeOnline 标注该用户当前是否持有实时（SignalR）连接。
/// </remarks>
public class OnlineUserListItemDto : BasicAppDto
{
    /// <summary>
    /// 用户主键
    /// </summary>
    public long UserId { get; set; }

    /// <summary>
    /// 用户名
    /// </summary>
    public string? UserName { get; set; }

    /// <summary>
    /// 昵称
    /// </summary>
    public string? NickName { get; set; }

    /// <summary>
    /// 头像（文件主键或直链）
    /// </summary>
    public string? Avatar { get; set; }

    /// <summary>
    /// 会话标识
    /// </summary>
    public string UserSessionId { get; set; } = string.Empty;

    /// <summary>
    /// 设备类型
    /// </summary>
    public DeviceType DeviceType { get; set; }

    /// <summary>
    /// 设备名称
    /// </summary>
    public string? DeviceName { get; set; }

    /// <summary>
    /// 操作系统
    /// </summary>
    public string? OperatingSystem { get; set; }

    /// <summary>
    /// 浏览器
    /// </summary>
    public string? Browser { get; set; }

    /// <summary>
    /// IP 地址脱敏值
    /// </summary>
    public string? IpAddressMasked { get; set; }

    /// <summary>
    /// 登录地点
    /// </summary>
    public string? Location { get; set; }

    /// <summary>
    /// 登录时间
    /// </summary>
    public DateTimeOffset LoginTime { get; set; }

    /// <summary>
    /// 最后活动时间
    /// </summary>
    public DateTimeOffset LastActivityTime { get; set; }

    /// <summary>
    /// 本次会话在线时长（秒，登录时间至今）
    /// </summary>
    public long OnlineDurationSeconds { get; set; }

    /// <summary>
    /// 是否持有实时连接（SignalR 在线）
    /// </summary>
    public bool IsRealtimeOnline { get; set; }
}

/// <summary>
/// 在线用户概览 DTO
/// </summary>
public sealed class OnlineUserSummaryDto
{
    /// <summary>
    /// 实时在线用户数（持有 SignalR 连接的去重用户数，跨租户全局）
    /// </summary>
    public int RealtimeOnlineUsers { get; set; }

    /// <summary>
    /// 活跃会话数（当前租户范围）
    /// </summary>
    public int ActiveSessions { get; set; }

    /// <summary>
    /// 活跃用户数（活跃会话去重用户数，当前租户范围）
    /// </summary>
    public int ActiveUsers { get; set; }
}
