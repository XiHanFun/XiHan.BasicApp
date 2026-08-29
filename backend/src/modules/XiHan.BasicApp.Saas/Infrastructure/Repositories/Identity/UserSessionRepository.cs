// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.Domain.Repositories;

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
    /// 吊销用户所有会话（跨租户）
    /// </summary>
    /// <remarks>
    /// 同一自然人在不同租户会落成不同租户戳的独立会话行，账号状态却是全局的，
    /// 因此吊销必须跨租户，否则在租户 A 停用的用户拿着租户 B 的会话照常可用。
    /// </remarks>
    public async Task<IReadOnlyList<string>> RevokeByUserIdAsync(long userId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var sessions = await CreateNoTenantQueryable()
            .Where(session => session.UserId == userId && session.Status == SessionStatus.Active)
            .ToListAsync(cancellationToken);

        return await RevokeAllAsync(sessions, cancellationToken);
    }

    /// <summary>
    /// 吊销由指定用户发起的全部模仿会话（跨租户）
    /// </summary>
    /// <remarks>
    /// 模仿会话行的租户戳是「被模仿者所在租户」，跨租户模仿又是允许的，
    /// 因此这里同样必须跨租户，否则发起人被停用后他借来的身份还活着。
    /// </remarks>
    /// <param name="impersonatorUserId">模仿者用户标识</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>被吊销的会话业务标识</returns>
    public async Task<IReadOnlyList<string>> RevokeByImpersonatorUserIdAsync(long impersonatorUserId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var sessions = await CreateNoTenantQueryable()
            .Where(session => session.ImpersonatorUserId == impersonatorUserId && session.Status == SessionStatus.Active)
            .ToListAsync(cancellationToken);

        return await RevokeAllAsync(sessions, cancellationToken);
    }

    /// <summary>
    /// 批量置为已吊销
    /// </summary>
    /// <remarks>
    /// 走对象式 Updateable：表达式式工厂会自动挂上全局租户过滤，
    /// 把 UPDATE 的 WHERE 收窄到当前租户，跨租户的会话行就改不动了。
    /// 对象式按主键写，再用写边界豁免放行别的租户戳的行。
    /// </remarks>
    /// <param name="sessions">待吊销的会话行</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>被吊销的会话业务标识，供调用方精确失效会话状态缓存</returns>
    private async Task<IReadOnlyList<string>> RevokeAllAsync(List<SysUserSession> sessions, CancellationToken cancellationToken)
    {
        if (sessions.Count == 0)
        {
            return [];
        }

        var now = DateTimeOffset.UtcNow;
        foreach (var session in sessions)
        {
            session.Status = SessionStatus.Revoked;
            session.RevokedTime = now;
        }

        // 写的是别的租户戳的行，显式声明写边界豁免
        using (TenantWriteGuard.Suppress())
        {
            _ = await DbClient.Updateable(sessions)
                .UpdateColumns(session => new { session.Status, session.RevokedTime })
                .ExecuteCommandAsync(cancellationToken);
        }

        return [.. sessions
            .Select(static session => session.UserSessionId)
            .Where(static id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.Ordinal)];
    }
}
