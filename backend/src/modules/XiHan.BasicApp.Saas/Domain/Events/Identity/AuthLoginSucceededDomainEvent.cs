// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.Framework.Domain.Events;

namespace XiHan.BasicApp.Saas.Domain.Events;

/// <summary>
/// 认证登录成功事件
/// </summary>
public sealed class AuthLoginSucceededDomainEvent : DomainEventBase
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public AuthLoginSucceededDomainEvent(
        long? tenantId,
        long userId,
        string? userName,
        long sessionRecordId,
        string sessionBusinessId,
        DateTimeOffset loginTime,
        string? traceId,
        string? ipAddress,
        string? userAgent,
        string? location,
        string? browser,
        string? operatingSystem,
        string? deviceName)
    {
        TenantId = tenantId;
        UserId = userId;
        UserName = userName;
        SessionRecordId = sessionRecordId;
        SessionBusinessId = sessionBusinessId;
        LoginTime = loginTime;
        TraceId = traceId;
        IpAddress = ipAddress;
        UserAgent = userAgent;
        Location = location;
        Browser = browser;
        OperatingSystem = operatingSystem;
        DeviceName = deviceName;
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
    /// 登录时间
    /// </summary>
    public DateTimeOffset LoginTime { get; }

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

    /// <summary>
    /// 登录位置
    /// </summary>
    public string? Location { get; }

    /// <summary>
    /// 浏览器
    /// </summary>
    public string? Browser { get; }

    /// <summary>
    /// 操作系统
    /// </summary>
    public string? OperatingSystem { get; }

    /// <summary>
    /// 设备名称
    /// </summary>
    public string? DeviceName { get; }
}
