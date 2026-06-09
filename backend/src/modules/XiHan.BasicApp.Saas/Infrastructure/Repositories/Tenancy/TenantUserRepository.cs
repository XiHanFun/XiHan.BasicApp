#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:TenantUserRepository
// Guid:2f30fd4b-ef2c-4353-8a92-e742ed856ed5
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/26 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

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
