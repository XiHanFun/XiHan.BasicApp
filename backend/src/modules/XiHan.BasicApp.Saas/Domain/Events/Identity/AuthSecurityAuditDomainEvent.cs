#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:AuthSecurityAuditDomainEvent
// Guid:b3a2f6c1-9e74-4d2b-8f1a-5c6d7e8f9a01
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/06/11 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.Framework.Domain.Events;

namespace XiHan.BasicApp.Saas.Domain.Events;

/// <summary>
/// 认证安全审计事件
/// </summary>
/// <remarks>
/// 承载登录成功/失败/登出之外的认证审计行为：令牌刷新、密码修改、密码重置、绑定/解绑 MFA 等，
/// 统一落登录日志（认证审计）。
/// </remarks>
public sealed class AuthSecurityAuditDomainEvent : DomainEventBase
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public AuthSecurityAuditDomainEvent(
        long? tenantId,
        long? userId,
        string? userName,
        LoginResult auditResult,
        string? message,
        DateTimeOffset auditTime,
        string? traceId,
        string? ipAddress,
        string? userAgent)
    {
        TenantId = tenantId;
        UserId = userId;
        UserName = userName;
        AuditResult = auditResult;
        Message = message;
        AuditTime = auditTime;
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
    /// 审计结果（令牌刷新/密码修改/密码重置/绑定MFA/解绑MFA 等）
    /// </summary>
    public LoginResult AuditResult { get; }

    /// <summary>
    /// 审计说明
    /// </summary>
    public string? Message { get; }

    /// <summary>
    /// 审计时间
    /// </summary>
    public DateTimeOffset AuditTime { get; }

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
