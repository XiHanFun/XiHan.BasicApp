// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Data.SqlSugar.Clients;

namespace XiHan.BasicApp.Saas.Infrastructure.Repositories;

/// <summary>
/// 租户成员仓储实现
/// </summary>
public sealed class TenantUserRepository(ISqlSugarClientResolver clientResolver)
    : SaasRepository<SysTenantUser>(clientResolver), ITenantUserRepository
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<SysTenantUser>> GetActiveByUserIdAsync(long userId, DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateNoTenantQueryable()
            .Where(user => user.UserId == userId)
            .Where(user => user.InviteStatus == TenantMemberInviteStatus.Accepted)
            .Where(user => user.Status == ValidityStatus.Valid)
            .Where(user => user.EffectiveTime == null || user.EffectiveTime <= now)
            .Where(user => user.ExpirationTime == null || user.ExpirationTime > now)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<SysTenantUser?> GetMembershipAsync(long userId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateQueryable()
            .Where(user => user.UserId == userId)
            .FirstAsync(cancellationToken);
    }
}
