#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysTenantRepository
// Guid:2c2b3c4d-5e6f-7890-abcd-ef12345678b7
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/10/31 6:45:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Repositories.Abstractions;
using XiHan.Framework.Data.SqlSugar;
using XiHan.Framework.Data.SqlSugar.Repository;

namespace XiHan.BasicApp.Rbac.Repositories.Implementations;

/// <summary>
/// 系统租户仓储实现
/// </summary>
public class SysTenantRepository : SqlSugarRepositoryBase<SysTenant, RbacIdType>, ISysTenantRepository
{
    private readonly ISqlSugarDbContext _dbContext;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="dbContext">数据库上下文</param>
    public SysTenantRepository(ISqlSugarDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// 根据租户编码获取租户
    /// </summary>
    /// <param name="tenantCode">租户编码</param>
    /// <returns></returns>
    public async Task<SysTenant?> GetByTenantCodeAsync(string tenantCode)
    {
        return await GetFirstAsync(t => t.TenantCode == tenantCode);
    }

    /// <summary>
    /// 根据域名获取租户
    /// </summary>
    /// <param name="domain">域名</param>
    /// <returns></returns>
    public async Task<SysTenant?> GetByDomainAsync(string domain)
    {
        return await GetFirstAsync(t => t.Domain == domain);
    }

    /// <summary>
    /// 检查租户编码是否存在
    /// </summary>
    /// <param name="tenantCode">租户编码</param>
    /// <param name="excludeId">排除的租户ID</param>
    /// <returns></returns>
    public async Task<bool> ExistsByTenantCodeAsync(string tenantCode, RbacIdType? excludeId = null)
    {
        var query = _dbContext.GetClient().Queryable<SysTenant>().Where(t => t.TenantCode == tenantCode);
        if (excludeId.HasValue)
        {
            query = query.Where(t => t.BasicId != excludeId.Value);
        }
        return await query.AnyAsync();
    }

    /// <summary>
    /// 检查域名是否存在
    /// </summary>
    /// <param name="domain">域名</param>
    /// <param name="excludeId">排除的租户ID</param>
    /// <returns></returns>
    public async Task<bool> ExistsByDomainAsync(string domain, RbacIdType? excludeId = null)
    {
        var query = _dbContext.GetClient().Queryable<SysTenant>().Where(t => t.Domain == domain);
        if (excludeId.HasValue)
        {
            query = query.Where(t => t.BasicId != excludeId.Value);
        }
        return await query.AnyAsync();
    }

    /// <summary>
    /// 获取租户的用户数量
    /// </summary>
    /// <param name="tenantId">租户ID</param>
    /// <returns></returns>
    public async Task<int> GetTenantUserCountAsync(RbacIdType tenantId)
    {
        return await _dbContext.GetClient()
            .Queryable<SysUser>()
            .Where(u => u.TenantId == tenantId)
            .CountAsync();
    }

    /// <summary>
    /// 获取租户的已使用存储空间(MB)
    /// </summary>
    /// <param name="tenantId">租户ID</param>
    /// <returns></returns>
    public async Task<RbacIdType> GetTenantUsedStorageAsync(RbacIdType tenantId)
    {
        // 这里需要根据实际的文件存储实现来计算
        // 示例：统计租户相关的文件大小
        var totalSize = await _dbContext.GetClient()
            .Queryable<SysFile>()
            .Where(f => f.TenantId == tenantId)
            .Select(f => f.FileSize)
            .ToListAsync();

        return totalSize.Sum() / (1024 * 1024); // 转换为 MB
    }
}
