// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 用户会话领域服务实现
/// </summary>
public sealed class UserSessionDomainService : IUserSessionDomainService
{
    /// <inheritdoc />
    public bool IsSessionValid(SysUserSession session, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(session);

        if (session.Status != SessionStatus.Active)
        {
            return false;
        }

        if (session.ExpirationTime.HasValue && session.ExpirationTime.Value <= now)
        {
            return false;
        }

        return true;
    }

    /// <inheritdoc />
    public IReadOnlyList<SysUserSession> GetSessionsToRevoke(IReadOnlyList<SysUserSession> activeSessions, int maxConcurrent)
    {
        ArgumentNullException.ThrowIfNull(activeSessions);

        if (maxConcurrent <= 0 || activeSessions.Count <= maxConcurrent)
        {
            return [];
        }

        // 按最后活跃时间降序排序，保留最近的 maxConcurrent 个，其余需要吊销
        return activeSessions
            .OrderByDescending(session => session.LastActivityTime)
            .Skip(maxConcurrent)
            .ToList();
    }
}
