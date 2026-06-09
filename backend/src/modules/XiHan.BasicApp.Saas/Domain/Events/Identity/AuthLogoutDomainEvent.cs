#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:AuthLogoutDomainEvent
// Guid:566e940d-4f11-46da-b469-70756297f5b5
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/17 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.Framework.Domain.Events;

namespace XiHan.BasicApp.Saas.Domain.Events;

/// <summary>
/// 认证退出登录事件
/// </summary>
public sealed class AuthLogoutDomainEvent : DomainEventBase
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public AuthLogoutDomainEvent(
        long? tenantId,
        long userId,
        string? userName,
        long sessionRecordId,
        string sessionBusinessId,
        DateTimeOffset logoutTime,
        string? traceId,
        string? ipAddress,
        string? userAgent)
    {
        TenantId = tenantId;
        UserId = userId;
        UserName = userName;
        SessionRecordId = sessionRecordId;
        SessionBusinessId = sessionBusinessId;
        LogoutTime = logoutTime;
        TraceId = traceId;
        IpAddress = ipAddress;
        UserAgent = userAgent;
    }

    /// <summary>
    /// 租户标识
    /// </summary>
    public long? TenantId { get; }

    /// <summary>
    /// 用户标识
    /// </summary>
    public long UserId { get; }

    /// <summary>
    /// 用户名
    /// </summary>
    public string? UserName { get; }

    /// <summary>
    /// 会话记录标识
    /// </summary>
    public long SessionRecordId { get; }

    /// <summary>
    /// 业务会话标识
    /// </summary>
    public string SessionBusinessId { get; }

    /// <summary>
    /// 退出时间
    /// </summary>
    public DateTimeOffset LogoutTime { get; }

    /// <summary>
    /// 链路标识
    /// </summary>
    public string? TraceId { get; }

    /// <summary>
    /// IP 地址
    /// </summary>
    public string? IpAddress { get; }

    /// <summary>
    /// 用户代理
    /// </summary>
    public string? UserAgent { get; }
}
