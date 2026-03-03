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

using XiHan.BasicApp.Rbac.Domain.Entities;
using XiHan.BasicApp.Rbac.Domain.Repositories;
using XiHan.Framework.Data.SqlSugar;
using XiHan.Framework.Data.SqlSugar.Repository;
using XiHan.Framework.MultiTenancy.Abstractions;
using XiHan.Framework.Uow;

namespace XiHan.BasicApp.Rbac.Infrastructure.Repositories;

/// <summary>
/// 用户会话仓储实现
/// </summary>
public class UserSessionRepository : SqlSugarAggregateRepository<SysUserSession, long>, IUserSessionRepository
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public UserSessionRepository(
        ISqlSugarClientProvider clientProvider,
        ICurrentTenant currentTenant,
        IServiceProvider serviceProvider,
        IUnitOfWorkManager unitOfWorkManager)
        : base(clientProvider, currentTenant, serviceProvider, unitOfWorkManager)
    {
    }

    /// <summary>
    /// 根据会话ID获取会话
    /// </summary>
    public async Task<SysUserSession?> GetBySessionIdAsync(string sessionId, long? tenantId = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        var resolvedTenantId = tenantId ?? CurrentTenantId;

        var query = CreateTenantQueryable()
            .Where(session => session.SessionId == sessionId);

        if (resolvedTenantId.HasValue)
        {
            query = query.Where(session => session.TenantId == resolvedTenantId.Value);
        }
        else
        {
            query = query.Where(session => session.TenantId == null);
        }

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

        var resolvedTenantId = tenantId ?? CurrentTenantId;
        var query = CreateTenantQueryable()
            .Where(session => session.UserId == userId && session.IsOnline && !session.IsRevoked);

        if (resolvedTenantId.HasValue)
        {
            query = query.Where(session => session.TenantId == resolvedTenantId.Value);
        }
        else
        {
            query = query.Where(session => session.TenantId == null);
        }

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
