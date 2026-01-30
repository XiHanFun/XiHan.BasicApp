#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysTenantRepository
// Guid:f4a5b6c7-d8e9-0123-4567-890123f89012
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/1/30 0:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Enums;
using XiHan.BasicApp.Rbac.Repositories.Abstracts;
using XiHan.Framework.Data.SqlSugar;
using XiHan.Framework.Data.SqlSugar.Repository;
using XiHan.Framework.Uow;

namespace XiHan.BasicApp.Rbac.Repositories;

/// <summary>
/// 租户仓储实现
/// </summary>
public class SysTenantRepository : SqlSugarAggregateRepository<SysTenant, long>, ISysTenantRepository
{
    private readonly ISqlSugarDbContext _dbContext;

    /// <summary>
    /// 构造函数
    /// </summary>
    public SysTenantRepository(ISqlSugarDbContext dbContext, IUnitOfWorkManager unitOfWorkManager)
        : base(dbContext, unitOfWorkManager)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// 根据租户编码获取租户
    /// </summary>
    public async Task<SysTenant?> GetByTenantCodeAsync(string tenantCode, CancellationToken cancellationToken = default)
    {
        return await _dbContext.GetClient().Queryable<SysTenant>()
            .Where(t => t.TenantCode == tenantCode && t.Status == YesOrNo.Yes)
            .FirstAsync(cancellationToken);
    }

    /// <summary>
    /// 根据域名获取租户
    /// </summary>
    public async Task<SysTenant?> GetByDomainAsync(string domain, CancellationToken cancellationToken = default)
    {
        return await _dbContext.GetClient().Queryable<SysTenant>()
            .Where(t => t.Domain == domain && t.Status == YesOrNo.Yes)
            .FirstAsync(cancellationToken);
    }

    /// <summary>
    /// 获取所有启用的租户
    /// </summary>
    public async Task<List<SysTenant>> GetActiveTenantsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.GetClient().Queryable<SysTenant>()
            .Where(t => t.Status == YesOrNo.Yes && t.TenantStatus == TenantStatus.Normal)
            .OrderBy(t => t.Sort)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 检查租户编码是否存在
    /// </summary>
    public async Task<bool> ExistsByTenantCodeAsync(string tenantCode, long? excludeTenantId = null, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.GetClient().Queryable<SysTenant>()
            .Where(t => t.TenantCode == tenantCode);

        if (excludeTenantId.HasValue)
        {
            query = query.Where(t => t.BasicId != excludeTenantId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }
}
