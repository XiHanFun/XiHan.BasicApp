#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysConfigRepository
// Guid:f0a1b2c3-d4e5-6789-0123-456789f45678
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
            .Where(c => c.ConfigKey == configKey && c.Status == YesOrNo.Yes);

        if (tenantId.HasValue)
        {
            query = query.Where(c => c.TenantId == tenantId.Value);
        }

        return await query.FirstAsync(cancellationToken);
    }

    /// <summary>
    /// 根据配置组获取配置列表
    /// </summary>
    public async Task<List<SysConfig>> GetByConfigGroupAsync(string configGroup, long? tenantId = null, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.GetClient().Queryable<SysConfig>()
            .Where(c => c.ConfigGroup == configGroup && c.Status == YesOrNo.Yes);

        if (tenantId.HasValue)
        {
            query = query.Where(c => c.TenantId == tenantId.Value);
        }

        return await query.OrderBy(c => c.Sort).ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 获取所有配置
    /// </summary>
    public async Task<List<SysConfig>> GetAllConfigsAsync(long? tenantId = null, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.GetClient().Queryable<SysConfig>()
            .Where(c => c.Status == YesOrNo.Yes);

        if (tenantId.HasValue)
        {
            query = query.Where(c => c.TenantId == tenantId.Value);
        }

        return await query.OrderBy(c => c.Sort).ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 批量更新配置
    /// </summary>
    public async Task UpdateConfigsAsync(IEnumerable<SysConfig> configs, CancellationToken cancellationToken = default)
    {
        await _dbContext.GetClient().Updateable(configs.ToArray()).ExecuteCommandAsync(cancellationToken);
    }

    /// <summary>
    /// 检查配置键是否存在
    /// </summary>
    public async Task<bool> ExistsByConfigKeyAsync(string configKey, long? excludeConfigId = null, long? tenantId = null, CancellationToken cancellationToken = default)
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
}
