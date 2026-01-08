#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:TenantRepository
// Guid:f6a7b8c9-d0e1-4f5a-2b3c-5d6e7f8a9b0c
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/1/8 0:00:00
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
public class TenantRepository : SqlSugarAggregateRepository<SysTenant, long>, ITenantRepository
{
    private readonly ISqlSugarClient _dbClient;

    /// <summary>
    /// 构造函数
    /// </summary>
    public TenantRepository(ISqlSugarDbContext dbContext, IUnitOfWorkManager unitOfWorkManager)
        : base(dbContext, unitOfWorkManager)
    {
        _dbClient = dbContext.GetClient();
    }

    /// <summary>
    /// 根据租户编码查询租户
    /// </summary>
    public async Task<SysTenant?> GetByTenantCodeAsync(string tenantCode, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await _dbClient.Queryable<SysTenant>()
            .FirstAsync(t => t.TenantCode == tenantCode, cancellationToken);
    }

    /// <summary>
    /// 根据租户域名查询租户
    /// </summary>
    public async Task<SysTenant?> GetByDomainAsync(string domain, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await _dbClient.Queryable<SysTenant>()
            .FirstAsync(t => t.Domain == domain, cancellationToken);
    }

    /// <summary>
    /// 检查租户编码是否存在
    /// </summary>
    public async Task<bool> ExistsByTenantCodeAsync(string tenantCode, long? excludeTenantId = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var query = _dbClient.Queryable<SysTenant>()
            .Where(t => t.TenantCode == tenantCode);

        if (excludeTenantId.HasValue)
        {
            query = query.Where(t => t.BasicId != excludeTenantId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    /// <summary>
    /// 检查域名是否存在
    /// </summary>
    public async Task<bool> ExistsByDomainAsync(string domain, long? excludeTenantId = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var query = _dbClient.Queryable<SysTenant>()
            .Where(t => t.Domain == domain);

        if (excludeTenantId.HasValue)
        {
            query = query.Where(t => t.BasicId != excludeTenantId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    /// <summary>
    /// 获取启用的租户列表
    /// </summary>
    public async Task<List<SysTenant>> GetActiveTenantsAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await _dbClient.Queryable<SysTenant>()
            .Where(t => t.Status == YesOrNo.Yes)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 获取租户的用户数量
    /// </summary>
    public async Task<int> GetUserCountAsync(long tenantId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await _dbClient.Queryable<SysUser>()
            .Where(u => u.TenantId == tenantId)
            .CountAsync(cancellationToken);
    }

    /// <summary>
    /// 获取租户已使用的存储空间大小（MB）
    /// </summary>
    public async Task<long> GetUsedStorageAsync(long tenantId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // 计算租户下所有文件的总大小
        var totalSize = await _dbClient.Queryable<SysFile>()
            .Where(f => f.TenantId == tenantId)
            .SumAsync(f => f.FileSize);

        // 转换为 MB
        return totalSize / (1024 * 1024);
    }
}
