#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:TenantRepository
// Guid:4b22682c-49cc-4eac-8199-8b197b4c440d
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/02/28 05:53:52
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Domain.Repositories;
using XiHan.BasicApp.Rbac.Domain.Entities;
using XiHan.Framework.Data.SqlSugar;
using XiHan.Framework.Data.SqlSugar.Repository;
using XiHan.Framework.Uow;

namespace XiHan.BasicApp.Rbac.Infrastructure.Repositories;

/// <summary>
/// 租户仓储实现
/// </summary>
public class TenantRepository : SqlSugarAggregateRepository<SysTenant, long>, ITenantRepository
{
    private readonly ISqlSugarDbContext _dbContext;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="dbContext"></param>
    /// <param name="unitOfWorkManager"></param>
    public TenantRepository(ISqlSugarDbContext dbContext, IUnitOfWorkManager unitOfWorkManager)
        : base(dbContext, unitOfWorkManager)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// 根据租户编码获取租户
    /// </summary>
    /// <param name="tenantCode"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<SysTenant?> GetByTenantCodeAsync(string tenantCode, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantCode);
        var tenant = await _dbContext.CreateQueryable<SysTenant>()
            .Where(tenant => tenant.TenantCode == tenantCode)
            .FirstAsync(cancellationToken);
        return tenant;
    }

    /// <summary>
    /// 判断租户编码是否存在
    /// </summary>
    /// <param name="tenantCode"></param>
    /// <param name="excludeTenantId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<bool> IsTenantCodeExistsAsync(string tenantCode, long? excludeTenantId = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantCode);
        var query = _dbContext.CreateQueryable<SysTenant>()
            .Where(tenant => tenant.TenantCode == tenantCode);

        if (excludeTenantId.HasValue)
        {
            query = query.Where(tenant => tenant.BasicId != excludeTenantId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }
}
