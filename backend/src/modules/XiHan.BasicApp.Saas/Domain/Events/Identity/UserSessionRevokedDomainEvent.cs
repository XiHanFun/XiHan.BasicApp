#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:UserSessionRevokedDomainEvent
// Guid:06e3f3e0-8045-4112-af72-02a547c4522b
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/26 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

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
