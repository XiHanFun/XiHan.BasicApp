#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:AuthSessionManager
// Guid:bb55c0df-c5d0-47a3-93bc-d93f1cc13943
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/05 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Web.Core.Clients;

namespace XiHan.BasicApp.Saas.Application.AppServices.Implementations;

/// <summary>
/// 认证会话管理服务实现
/// </summary>
public class AuthSessionManager(IUserSessionRepository userSessionRepository) : IAuthSessionManager
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> EnforceSessionPolicyAsync(SysUser user, SysUserSecurity security, long? effectiveTenantId)
    {
        var onlineSessions = await userSessionRepository.GetOnlineSessionsAsync(user.BasicId, effectiveTenantId);
        if (onlineSessions.Count == 0)
        {
            return [];
        }

        List<string> toRevokeSessionIds;
        if (!security.AllowMultiLogin)
        {
            toRevokeSessionIds = [.. onlineSessions.Select(static session => session.UserSessionId)];
        }
        else if (security.MaxLoginDevices > 0 && onlineSessions.Count >= security.MaxLoginDevices)
        {
            var overflowCount = onlineSessions.Count - security.MaxLoginDevices + 1;
            toRevokeSessionIds = [..
                onlineSessions
                    .OrderBy(static session => session.LastActivityTime)
                    .ThenBy(static session => session.LoginTime)
                    .Take(overflowCount)
                    .Select(static session => session.UserSessionId)];
        }
        else
        {
            return [];
        }

        if (toRevokeSessionIds.Count == 0)
        {
            return [];
        }

        await userSessionRepository.RevokeSessionsAsync(toRevokeSessionIds, "触发多端登录策略", effectiveTenantId);
        return toRevokeSessionIds;
    }

    /// <inheritdoc />
    public async Task SaveOrUpdateSessionAsync(SysUser user, long? effectiveTenantId, string sessionId, string accessTokenJti, ClientInfo clientInfo)
    {
        var now = DateTimeOffset.UtcNow;
        var userSession = await userSessionRepository.GetBySessionIdAsync(sessionId, effectiveTenantId);
        if (userSession is null)
        {
            userSession = new SysUserSession
            {
                TenantId = effectiveTenantId ?? 0,
                UserId = user.BasicId,
                CurrentAccessTokenJti = accessTokenJti,
                UserSessionId = sessionId,
                DeviceType = DeviceType.Web,
                DeviceName = clientInfo.DeviceName ?? "Web Browser",
                Browser = clientInfo.Browser,
                OperatingSystem = clientInfo.OperatingSystem,
                IpAddress = clientInfo.IpAddress,
                Location = clientInfo.Location,
                LoginTime = now,
                LastActivityTime = now,
                IsOnline = true,
                IsRevoked = false
            };
            await userSessionRepository.AddAsync(userSession);
            return;
        }

        userSession.CurrentAccessTokenJti = accessTokenJti;
        userSession.LastActivityTime = now;
        userSession.IsOnline = true;
        userSession.IsRevoked = false;
        userSession.RevokedAt = null;
        userSession.RevokedReason = null;
        userSession.LogoutTime = null;
        userSession.DeviceName = clientInfo.DeviceName ?? userSession.DeviceName;
        userSession.IpAddress = clientInfo.IpAddress;
        userSession.Browser = clientInfo.Browser;
        userSession.OperatingSystem = clientInfo.OperatingSystem;
        userSession.Location = clientInfo.Location;
        await userSessionRepository.UpdateAsync(userSession);
    }

    /// <inheritdoc />
    public async Task MarkSessionRevokedAsync(string sessionId, long? tenantId, string reason)
    {
        var session = await userSessionRepository.GetBySessionIdAsync(sessionId, tenantId);
        if (session is null)
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        session.IsOnline = false;
        session.IsRevoked = true;
        session.RevokedAt = now;
        session.LogoutTime = now;
        session.RevokedReason = reason;
        await userSessionRepository.UpdateAsync(session);
    }

    /// <inheritdoc />
    public async Task RevokeUserSessionsAsync(long userId, string reason, long? tenantId)
    {
        await userSessionRepository.RevokeUserSessionsAsync(userId, reason, tenantId);
    }

    /// <inheritdoc />
    public async Task<bool> IsSessionValidAsync(string sessionId, long? tenantId)
    {
        var session = await userSessionRepository.GetBySessionIdAsync(sessionId, tenantId);
        return session is not null && !session.IsRevoked && session.IsOnline;
    }
}
