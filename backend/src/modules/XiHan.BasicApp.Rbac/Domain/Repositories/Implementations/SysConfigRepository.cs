#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysConfigRepository
// Guid:d0e1f2a3-b4c5-6789-abcd-ef1234567890
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/1/31 0:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.Framework.Data.SqlSugar;
using XiHan.Framework.Data.SqlSugar.Repository;
using XiHan.Framework.Uow;

namespace XiHan.BasicApp.Rbac.Domain.Repositories.Implementations;

/// <summary>
/// 系统配置仓储实现
/// </summary>
public class SysConfigRepository : SqlSugarAggregateRepository<SysConfig, long>, ISysConfigRepository
{
    private readonly ISqlSugarDbContext _dbContext;

    /// <summary>
    /// 构造函数
    /// </summary>
    public SysConfigRepository(ISqlSugarDbContext dbContext, IUnitOfWorkManager unitOfWorkManager)
        : base(dbContext, unitOfWorkManager)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// 根据配置键获取配置
    /// </summary>
    public async Task<SysConfig?> GetByConfigKeyAsync(string configKey, long? tenantId = null, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.GetClient().Queryable<SysConfig>()
            .Where(c => c.ConfigKey == configKey);

        if (tenantId.HasValue)
        {
            query = query.Where(c => c.TenantId == tenantId.Value);
        }
        else
        {
            query = query.Where(c => c.TenantId == null);
        }

        return await query.FirstAsync(cancellationToken);
    }

    /// <summary>
    /// 检查配置键是否存在
    /// </summary>
    public async Task<bool> IsConfigKeyExistsAsync(string configKey, long? excludeConfigId = null, long? tenantId = null, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.GetClient().Queryable<SysConfig>()
            .Where(c => c.ConfigKey == configKey);

        if (excludeConfigId.HasValue)
        {
            query = query.Where(c => c.BasicId != excludeConfigId.Value);
        }

        if (tenantId.HasValue)
        {
            query = query.Where(c => c.TenantId == tenantId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    /// <summary>
    /// 获取配置分组下的所有配置
    /// </summary>
    public async Task<List<SysConfig>> GetConfigsByGroupAsync(string configGroup, long? tenantId = null, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.GetClient().Queryable<SysConfig>()
            .Where(c => c.ConfigGroup == configGroup);

        if (tenantId.HasValue)
        {
            query = query.Where(c => c.TenantId == tenantId.Value);
        }

        return await query.OrderBy(c => c.Sort).ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 获取租户的所有配置
    /// </summary>
    public async Task<List<SysConfig>> GetConfigsByTenantAsync(long tenantId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.GetClient().Queryable<SysConfig>()
            .Where(c => c.TenantId == tenantId)
            .OrderBy(c => c.Sort)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 保存配置
    /// </summary>
    public async Task<SysConfig> SaveAsync(SysConfig config, CancellationToken cancellationToken = default)
    {
        if (config.IsTransient())
        {
            return await AddAsync(config, cancellationToken);
        }
        else
        {
            return await UpdateAsync(config, cancellationToken);
        }
    }

    /// <summary>
    /// 批量保存配置
    /// </summary>
    public async Task SaveBatchAsync(List<SysConfig> configs, CancellationToken cancellationToken = default)
    {
        await _dbContext.GetClient().Storageable(configs)
            .ExecuteCommandAsync(cancellationToken);
    }
}
