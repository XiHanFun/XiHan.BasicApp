// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Data.SqlSugar.Clients;

namespace XiHan.BasicApp.Saas.Infrastructure.Repositories;

/// <summary>
/// 用户直授权限仓储实现
/// </summary>
public sealed class UserPermissionRepository(ISqlSugarClientResolver clientResolver)
    : SaasRepository<SysUserPermission>(clientResolver), IUserPermissionRepository
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<SysUserPermission>> GetValidByUserIdAsync(long userId, DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateQueryable()
            .Where(permission => permission.UserId == userId)
            .Where(permission => permission.Status == ValidityStatus.Valid)
            .Where(permission => permission.EffectiveTime == null || permission.EffectiveTime <= now)
            .Where(permission => permission.ExpirationTime == null || permission.ExpirationTime > now)
            .ToListAsync(cancellationToken);
    }
}
