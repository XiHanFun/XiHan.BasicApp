// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Data.SqlSugar.Clients;

namespace XiHan.BasicApp.Saas.Infrastructure.Repositories;

/// <summary>
/// 会话角色仓储实现
/// </summary>
public sealed class SessionRoleRepository(ISqlSugarClientResolver clientResolver)
    : SaasRepository<SysSessionRole>(clientResolver), ISessionRoleRepository
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<SysSessionRole>> GetBySessionIdAsync(long sessionId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateQueryable()
            .Where(role => role.SessionId == sessionId)
            .ToListAsync(cancellationToken);
    }
}
