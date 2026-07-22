// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Data.SqlSugar.Clients;

namespace XiHan.BasicApp.Saas.Infrastructure.Repositories;

/// <summary>
/// 权限委托仓储实现
/// </summary>
public sealed class PermissionDelegationRepository(ISqlSugarClientResolver clientResolver)
    : SaasRepository<SysPermissionDelegation>(clientResolver), IPermissionDelegationRepository
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<SysPermissionDelegation>> GetActiveByDelegateeIdAsync(long delegateeUserId, DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateQueryable()
            .Where(d => d.DelegateeUserId == delegateeUserId
                && d.DelegationStatus == DelegationStatus.Active
                && (d.EffectiveTime == null || d.EffectiveTime <= now)
                && d.ExpirationTime >= now)
            .ToListAsync(cancellationToken);
    }
}
