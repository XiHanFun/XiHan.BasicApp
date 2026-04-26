#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:TenantRepository
// Guid:b05b600a-b9dc-4666-8b73-e14f048118bf
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/26 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.Uow;

namespace XiHan.BasicApp.Saas.Infrastructure.Repositories;

/// <summary>
/// 租户仓储实现
/// </summary>
public sealed class TenantRepository(
    ISqlSugarClientResolver clientResolver,
    IUnitOfWorkManager unitOfWorkManager)
    : SaasAggregateRepository<SysTenant>(clientResolver, unitOfWorkManager), ITenantRepository
{
    /// <inheritdoc />
    protected override ISugarQueryable<SysTenant> CreateQueryable()
    {
        return CreateNoTenantQueryable();
    }

    /// <inheritdoc />
    public async Task<SysTenant?> GetByCodeAsync(string tenantCode, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantCode);
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateQueryable()
            .Where(tenant => tenant.TenantCode == tenantCode)
            .FirstAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<SysTenant?> GetByDomainAsync(string domain, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(domain);
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateQueryable()
            .Where(tenant => tenant.Domain == domain)
            .FirstAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> ExistsTenantCodeAsync(string tenantCode, long? excludeTenantId = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantCode);
        cancellationToken.ThrowIfCancellationRequested();

        var query = CreateQueryable().Where(tenant => tenant.TenantCode == tenantCode);
        if (excludeTenantId.HasValue)
        {
            query = query.Where(tenant => tenant.BasicId != excludeTenantId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }
}
