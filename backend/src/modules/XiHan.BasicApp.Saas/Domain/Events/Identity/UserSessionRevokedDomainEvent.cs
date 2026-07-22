// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Saas.Domain.Events;

/// <summary>
/// 用户会话撤销事件
/// </summary>
public sealed class UserSessionRevokedDomainEvent : SaasDomainEventBase
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public UserSessionRevokedDomainEvent(
        long tenantId,
        long userId,
        long? sessionId,
        string? userSessionId,
        string? accessTokenJti,
        bool revokeAllUserSessions = false,
        long? operatorUserId = null,
        string? reason = null)
        : base(tenantId, operatorUserId, reason)
    {
        UserId = userId;
        SessionId = sessionId;
        UserSessionId = userSessionId;
        AccessTokenJti = accessTokenJti;
        RevokeAllUserSessions = revokeAllUserSessions;
    }

    /// <summary>
    /// 用户ID
    /// </summary>
    public long UserId { get; }

    /// <summary>
    /// 会话记录ID
    /// </summary>
    public long? SessionId { get; }

    /// <summary>
    /// 业务会话标识
    /// </summary>
    public string? UserSessionId { get; }

    /// <summary>
    /// 访问令牌JTI
    /// </summary>
    public string? AccessTokenJti { get; }

    /// <summary>
    /// 是否撤销该用户全部会话
    /// </summary>
    public bool RevokeAllUserSessions { get; }
}
