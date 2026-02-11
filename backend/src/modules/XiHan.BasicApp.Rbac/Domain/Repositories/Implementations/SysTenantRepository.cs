#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysTenantRepository
// Guid:d4e5f6a7-b8c9-0123-4567-890abcdef123
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/01/31 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Enums;
using XiHan.Framework.Data.SqlSugar;
using XiHan.Framework.Data.SqlSugar.Repository;
using XiHan.Framework.Uow;

namespace XiHan.BasicApp.Rbac.Domain.Repositories.Implementations;

/// <summary>
/// 系统租户仓储实现
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
            .Where(t => t.TenantCode == tenantCode)
            .FirstAsync(cancellationToken);
    }

    /// <summary>
    /// 根据域名获取租户
    /// </summary>
    public async Task<SysTenant?> GetByDomainAsync(string domain, CancellationToken cancellationToken = default)
    {
        return await _dbContext.GetClient().Queryable<SysTenant>()
            .Where(t => t.Domain == domain)
            .FirstAsync(cancellationToken);
    }

    /// <summary>
    /// 检查租户编码是否存在
    /// </summary>
    public async Task<bool> IsTenantCodeExistsAsync(string tenantCode, long? excludeTenantId = null, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.GetClient().Queryable<SysTenant>()
            .Where(t => t.TenantCode == tenantCode);

        if (excludeTenantId.HasValue)
        {
            query = query.Where(t => t.BasicId != excludeTenantId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    /// <summary>
    /// 获取所有活跃租户
    /// </summary>
    public async Task<List<SysTenant>> GetActivTenantsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.GetClient().Queryable<SysTenant>()
            .Where(t => t.Status == YesOrNo.Yes)
            .Where(t => t.TenantStatus == TenantStatus.Normal)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 获取即将过期的租户列表
    /// </summary>
    public async Task<List<SysTenant>> GetExpiringTenantsAsync(int days, CancellationToken cancellationToken = default)
    {
        var expiryDate = DateTimeOffset.Now.AddDays(days);
        return await _dbContext.GetClient().Queryable<SysTenant>()
            .Where(t => t.ExpireTime != null && t.ExpireTime <= expiryDate && t.ExpireTime > DateTimeOffset.Now)
            .Where(t => t.Status == YesOrNo.Yes)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 保存租户
    /// </summary>
    public async Task<SysTenant> SaveAsync(SysTenant tenant, CancellationToken cancellationToken = default)
    {
        if (tenant.IsTransient())
        {
            return await AddAsync(tenant, cancellationToken);
        }
        else
        {
            return await UpdateAsync(tenant, cancellationToken);
        }
    }

    /// <summary>
    /// 启用租户
    /// </summary>
    public async Task EnableTenantAsync(long tenantId, CancellationToken cancellationToken = default)
    {
        await _dbContext.GetClient().Updateable<SysTenant>()
            .SetColumns(t => new SysTenant
            {
                Status = YesOrNo.Yes,
                TenantStatus = TenantStatus.Normal
            })
            .Where(t => t.BasicId == tenantId)
            .ExecuteCommandAsync(cancellationToken);
    }

    /// <summary>
    /// 禁用租户
    /// </summary>
    public async Task DisableTenantAsync(long tenantId, CancellationToken cancellationToken = default)
    {
        await _dbContext.GetClient().Updateable<SysTenant>()
            .SetColumns(t => new SysTenant
            {
                Status = YesOrNo.No,
                TenantStatus = TenantStatus.Disabled
            })
            .Where(t => t.BasicId == tenantId)
            .ExecuteCommandAsync(cancellationToken);
    }

    /// <summary>
    /// 续期租户
    /// </summary>
    public async Task RenewTenantAsync(long tenantId, int days, CancellationToken cancellationToken = default)
    {
        var tenant = await GetByIdAsync(tenantId, cancellationToken);
        if (tenant == null)
        {
            throw new InvalidOperationException($"租户 {tenantId} 不存在");
        }

        var newExpireTime = tenant.ExpireTime.HasValue && tenant.ExpireTime > DateTimeOffset.Now
            ? tenant.ExpireTime.Value.AddDays(days)
            : DateTimeOffset.Now.AddDays(days);

        await _dbContext.GetClient().Updateable<SysTenant>()
            .SetColumns(t => t.ExpireTime == newExpireTime)
            .Where(t => t.BasicId == tenantId)
            .ExecuteCommandAsync(cancellationToken);
    }
}
