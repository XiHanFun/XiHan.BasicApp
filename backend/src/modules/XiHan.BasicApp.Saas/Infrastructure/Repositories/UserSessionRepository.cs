#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:UserSessionRepository
// Guid:0ff53e37-0f23-481d-a8ba-4fdaf3d317f5
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/03/04 12:22:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Data.SqlSugar;
using XiHan.Framework.Data.SqlSugar.Repository;
using XiHan.Framework.Data.SqlSugar.SplitTables;
using XiHan.Framework.Uow;

namespace XiHan.BasicApp.Saas.Infrastructure.Repositories;

/// <summary>
/// 用户会话仓储实现
/// </summary>
public class UserSessionRepository : SqlSugarAggregateRepository<SysUserSession, long>, IUserSessionRepository
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public UserSessionRepository(
        ISqlSugarDbContext dbContext,
        ISqlSugarSplitTableExecutor splitTableExecutor,
        IServiceProvider serviceProvider,
        IUnitOfWorkManager unitOfWorkManager)
        : base(dbContext, splitTableExecutor, serviceProvider, unitOfWorkManager)
    {
    }

    /// <summary>
    /// 根据会话ID获取会话
    /// </summary>
    public async Task<SysUserSession?> GetBySessionIdAsync(string sessionId, long? tenantId = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        var resolvedTenantId = tenantId;

        var query = CreateTenantQueryable()
            .Where(session => session.UserSessionId == sessionId);

        query = resolvedTenantId.HasValue
            ? query.Where(session => session.TenantId == resolvedTenantId.Value)
            : query.Where(session => session.TenantId == null);

        return await query.FirstAsync(cancellationToken);
    }

    /// <summary>
    /// 撤销用户的在线会话
    /// </summary>
    public async Task<int> RevokeUserSessionsAsync(long userId, string reason, long? tenantId = null, CancellationToken cancellationToken = default)
    {
        if (userId <= 0)
        {
            return 0;
        }

        var resolvedTenantId = tenantId;
        var query = CreateTenantQueryable()
            .Where(session => session.UserId == userId && session.IsOnline && !session.IsRevoked);

        query = resolvedTenantId.HasValue
            ? query.Where(session => session.TenantId == resolvedTenantId.Value)
            : query.Where(session => session.TenantId == null);

        var sessions = await query.ToListAsync(cancellationToken);
        if (sessions.Count == 0)
        {
            return 0;
        }

        var now = DateTimeOffset.UtcNow;
        foreach (var session in sessions)
        {
            session.IsRevoked = true;
            session.IsOnline = false;
            session.RevokedAt = now;
            session.LogoutTime = now;
            session.RevokedReason = reason;
        }

        await DbClient.Updateable(sessions).ExecuteCommandAsync(cancellationToken);
        return sessions.Count;
    }

    /// <summary>
    /// 获取用户在线会话列表
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="tenantId">租户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns></returns>
    public async Task<IReadOnlyList<SysUserSession>> GetOnlineSessionsAsync(long userId, long? tenantId = null, CancellationToken cancellationToken = default)
    {
        if (userId <= 0)
        {
            return [];
        }

        var resolvedTenantId = tenantId;
        var query = CreateTenantQueryable()
            .Where(session => session.UserId == userId && session.IsOnline && !session.IsRevoked);

        query = resolvedTenantId.HasValue
            ? query.Where(session => session.TenantId == resolvedTenantId.Value)
            : query.Where(session => session.TenantId == null);

        var sessions = await query
            .OrderBy(session => session.LastActivityTime)
            .ToListAsync(cancellationToken);
        return sessions;
    }

    /// <summary>
    /// 批量撤销指定会话
    /// </summary>
    /// <param name="sessionIds">会话ID列表</param>
    /// <param name="reason">撤销原因</param>
    /// <param name="tenantId">租户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns></returns>
    public async Task<int> RevokeSessionsAsync(IReadOnlyCollection<string> sessionIds, string reason, long? tenantId = null, CancellationToken cancellationToken = default)
    {
        if (sessionIds.Count == 0)
        {
            return 0;
        }

        var normalizedSessionIds = sessionIds
            .Where(static sessionId => !string.IsNullOrWhiteSpace(sessionId))
            .Select(static sessionId => sessionId.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        if (normalizedSessionIds.Length == 0)
        {
            return 0;
        }

        var resolvedTenantId = tenantId;
        var query = CreateTenantQueryable()
            .Where(session =>
                normalizedSessionIds.Contains(session.UserSessionId) &&
                session.IsOnline &&
                !session.IsRevoked);

        query = resolvedTenantId.HasValue
            ? query.Where(session => session.TenantId == resolvedTenantId.Value)
            : query.Where(session => session.TenantId == null);

        var sessions = await query.ToListAsync(cancellationToken);
        if (sessions.Count == 0)
        {
            return 0;
        }

        var now = DateTimeOffset.UtcNow;
        foreach (var session in sessions)
        {
            session.IsRevoked = true;
            session.IsOnline = false;
            session.RevokedAt = now;
            session.LogoutTime = now;
            session.RevokedReason = reason;
        }

        await DbClient.Updateable(sessions).ExecuteCommandAsync(cancellationToken);
        return sessions.Count;
    }
}
