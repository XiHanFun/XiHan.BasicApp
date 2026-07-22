// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.Framework.Domain.Events;

namespace XiHan.BasicApp.Saas.Domain.Events;

/// <summary>
/// 认证登录失败事件
/// </summary>
public sealed class AuthLoginFailedDomainEvent : DomainEventBase
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public AuthLoginFailedDomainEvent(
        long? tenantId,
        long? userId,
        string? userName,
        LoginResult loginResult,
        string? message,
        DateTimeOffset loginTime,
        string? traceId,
        string? ipAddress,
        string? userAgent)
    {
        TenantId = tenantId;
        UserId = userId;
        UserName = userName;
        LoginResult = loginResult;
        Message = message;
        LoginTime = loginTime;
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
    public long? UserId { get; }

    /// <summary>
    /// 用户名
    /// </summary>
    public string? UserName { get; }

    /// <summary>
    /// 登录结果
    /// </summary>
    public LoginResult LoginResult { get; }

    /// <summary>
    /// 消息
    /// </summary>
    public string? Message { get; }

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
}
