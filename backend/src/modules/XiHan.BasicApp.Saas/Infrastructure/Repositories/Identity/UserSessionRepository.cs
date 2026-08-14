// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Data.SqlSugar.Clients;

namespace XiHan.BasicApp.Saas.Infrastructure.Repositories;

/// <summary>
/// 用户会话仓储实现
/// </summary>
public sealed class UserSessionRepository(ISqlSugarClientResolver clientResolver)
    : SaasRepository<SysUserSession>(clientResolver), IUserSessionRepository
{
    /// <summary>
    /// 获取用户活跃会话列表
    /// </summary>
    public async Task<IReadOnlyList<SysUserSession>> GetActiveSessionsAsync(long userId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateQueryable()
            .Where(session => session.UserId == userId && session.Status == SessionStatus.Active)
            .OrderByDescending(session => session.LastActivityTime)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 按会话业务标识查询会话（跨租户，标识全局唯一；用于请求期会话有效性校验，不依赖当前租户上下文）
    /// </summary>
    public async Task<SysUserSession?> GetByUserSessionIdAsync(string userSessionId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(userSessionId))
        {
            return null;
        }

        // 会话业务标识全局唯一，跨租户查询（请求期会话有效性校验不依赖当前租户上下文）
        return await CreateNoTenantQueryable()
            .Where(session => session.UserSessionId == userSessionId)
            .FirstAsync(cancellationToken);
    }

    /// <summary>
    /// 跨租户获取用户在指定设备上的活跃会话（会话行带发起登录时租户戳，同设备旧会话下线须忽略租户过滤）
    /// </summary>
    public async Task<IReadOnlyList<SysUserSession>> GetActiveByUserAndDeviceIgnoreTenantAsync(long userId, string deviceId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(deviceId);
        cancellationToken.ThrowIfCancellationRequested();

        // 会话行带「发起登录时租户」的戳，同一设备的历史会话可能散落在不同租户戳下，须跨租户查询
        return await CreateNoTenantQueryable()
            .Where(session => session.UserId == userId && session.DeviceId == deviceId && session.Status == SessionStatus.Active)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 吊销用户所有会话
    /// </summary>
    public async Task<int> RevokeByUserIdAsync(long userId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await DbClient.Updateable<SysUserSession>()
            .SetColumns(session => session.Status == SessionStatus.Revoked)
            .SetColumns(session => session.RevokedTime == DateTimeOffset.UtcNow)
            .Where(session => session.UserId == userId && session.Status == SessionStatus.Active)
            .ExecuteCommandAsync(cancellationToken);
    }
}
