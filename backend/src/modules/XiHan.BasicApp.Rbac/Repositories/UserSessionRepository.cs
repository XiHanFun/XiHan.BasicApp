#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:UserSessionRepository
// Guid:e3f4a5b6-c7d8-4e5f-9a0b-2c3d4e5f6a7b
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/1/8 0:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Repositories.Abstracts;
using XiHan.Framework.Data.SqlSugar;
using XiHan.Framework.Data.SqlSugar.Repository;
using XiHan.Framework.Uow;

namespace XiHan.BasicApp.Rbac.Repositories;

/// <summary>
/// 用户会话仓储实现
/// </summary>
public class UserSessionRepository : SqlSugarAggregateRepository<SysUserSession, long>, IUserSessionRepository
{
    private readonly ISqlSugarClient _dbClient;

    /// <summary>
    /// 构造函数
    /// </summary>
    public UserSessionRepository(ISqlSugarDbContext dbContext, IUnitOfWorkManager unitOfWorkManager)
        : base(dbContext, unitOfWorkManager)
    {
        _dbClient = dbContext.GetClient();
    }

    /// <summary>
    /// 根据会话令牌获取会话
    /// </summary>
    public async Task<SysUserSession?> GetBySessionTokenAsync(string sessionToken, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await _dbClient.Queryable<SysUserSession>()
            .FirstAsync(s => s.Token == sessionToken, cancellationToken);
    }

    /// <summary>
    /// 根据用户ID获取所有会话
    /// </summary>
    public async Task<List<SysUserSession>> GetByUserIdAsync(long userId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await _dbClient.Queryable<SysUserSession>()
            .Where(s => s.UserId == userId)
            .OrderBy(s => s.CreatedTime, OrderByType.Desc)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 获取用户的有效会话列表
    /// </summary>
    public async Task<List<SysUserSession>> GetActiveSessionsByUserIdAsync(long userId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var currentTime = DateTimeOffset.UtcNow;
        return await _dbClient.Queryable<SysUserSession>()
            .Where(s => s.UserId == userId
                && !s.IsRevoked
                && s.TokenExpiresAt > currentTime)
            .OrderBy(s => s.CreatedTime, OrderByType.Desc)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 验证会话令牌
    /// </summary>
    public async Task<SysUserSession?> ValidateSessionTokenAsync(string sessionToken, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var currentTime = DateTimeOffset.UtcNow;
        return await _dbClient.Queryable<SysUserSession>()
            .FirstAsync(s => s.Token == sessionToken
                && !s.IsRevoked
                && s.TokenExpiresAt > currentTime, cancellationToken);
    }

    /// <summary>
    /// 更新会话最后活动时间
    /// </summary>
    public async Task<bool> UpdateLastActivityTimeAsync(string sessionToken, DateTimeOffset lastActivityTime, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var affectedRows = await _dbClient.Updateable<SysUserSession>()
            .SetColumns(s => new SysUserSession { LastActivityTime = lastActivityTime })
            .Where(s => s.Token == sessionToken)
            .ExecuteCommandAsync(cancellationToken);

        return affectedRows > 0;
    }

    /// <summary>
    /// 撤销会话
    /// </summary>
    public async Task<bool> RevokeSessionAsync(string sessionToken, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var affectedRows = await _dbClient.Updateable<SysUserSession>()
            .SetColumns(s => new SysUserSession { IsRevoked = true })
            .Where(s => s.Token == sessionToken)
            .ExecuteCommandAsync(cancellationToken);

        return affectedRows > 0;
    }

    /// <summary>
    /// 撤销用户的所有会话
    /// </summary>
    public async Task<int> RevokeUserSessionsAsync(long userId, long? excludeSessionId = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var query = _dbClient.Updateable<SysUserSession>()
            .SetColumns(s => new SysUserSession { IsRevoked = true })
            .Where(s => s.UserId == userId);

        if (excludeSessionId.HasValue)
        {
            query = query.Where(s => s.BasicId != excludeSessionId.Value);
        }

        return await query.ExecuteCommandAsync(cancellationToken);
    }

    /// <summary>
    /// 清理过期会话
    /// </summary>
    public async Task<int> CleanExpiredSessionsAsync(DateTimeOffset currentTime, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await _dbClient.Deleteable<SysUserSession>()
            .Where(s => s.TokenExpiresAt < currentTime)
            .ExecuteCommandAsync(cancellationToken);
    }

    /// <summary>
    /// 获取在线用户数量
    /// </summary>
    public async Task<int> GetOnlineUserCountAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var currentTime = DateTimeOffset.UtcNow;
        return await _dbClient.Queryable<SysUserSession>()
            .Where(s => !s.IsRevoked && s.TokenExpiresAt > currentTime)
            .Select(s => s.UserId)
            .Distinct()
            .CountAsync(cancellationToken);
    }
}
