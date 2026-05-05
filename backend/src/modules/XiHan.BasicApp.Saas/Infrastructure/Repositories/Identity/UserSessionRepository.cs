#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:UserSessionRepository
// Guid:bca48584-699f-4e24-b9f4-0b2bd3b322b0
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/29 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

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
    /// <inheritdoc />
    public async Task<IReadOnlyList<SysUserSession>> GetActiveSessionsAsync(long userId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateQueryable()
            .Where(session => session.UserId == userId && session.IsOnline && !session.IsRevoked)
            .OrderByDescending(session => session.LastActivityTime)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> RevokeByUserIdAsync(long userId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await DbClient.Updateable<SysUserSession>()
            .SetColumns(session => session.IsRevoked == true)
            .SetColumns(session => session.IsOnline == false)
            .SetColumns(session => session.RevokedAt == DateTimeOffset.UtcNow)
            .Where(session => session.UserId == userId && session.IsOnline && !session.IsRevoked)
            .ExecuteCommandAsync(cancellationToken);
    }
}
