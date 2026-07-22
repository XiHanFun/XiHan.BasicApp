// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Data.SqlSugar.Clients;

namespace XiHan.BasicApp.Saas.Infrastructure.Repositories;

/// <summary>
/// 用户角色仓储实现
/// </summary>
public sealed class UserRoleRepository(ISqlSugarClientResolver clientResolver)
    : SaasRepository<SysUserRole>(clientResolver), IUserRoleRepository
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<SysUserRole>> GetValidByUserIdAsync(long userId, DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateQueryable()
            .Where(role => role.UserId == userId)
            .Where(role => role.Status == ValidityStatus.Valid)
            .Where(role => role.EffectiveTime == null || role.EffectiveTime <= now)
            .Where(role => role.ExpirationTime == null || role.ExpirationTime > now)
            .ToListAsync(cancellationToken);
    }
}
