#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ConfigRepository
// Guid:b8c9d0e1-f2a3-4b5c-4d5e-7f8a9b0c1d2e
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/1/8 0:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Repositories.Abstracts;
using XiHan.Framework.Data.SqlSugar;
using XiHan.Framework.Data.SqlSugar.Repository;
using XiHan.Framework.Uow;

namespace XiHan.BasicApp.Rbac.Repositories;

/// <summary>
/// 配置仓储实现
/// </summary>
public class ConfigRepository : SqlSugarAggregateRepository<SysConfig, long>, IConfigRepository
{
    private readonly ISqlSugarClient _dbClient;

    /// <summary>
    /// 构造函数
    /// </summary>
    public ConfigRepository(ISqlSugarDbContext dbContext, IUnitOfWorkManager unitOfWorkManager)
        : base(dbContext, unitOfWorkManager)
    {
        _dbClient = dbContext.GetClient();
    }

    /// <summary>
    /// 根据配置键查询配置
    /// </summary>
    public async Task<SysConfig?> GetByConfigKeyAsync(string configKey, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await _dbClient.Queryable<SysConfig>()
            .FirstAsync(c => c.ConfigKey == configKey, cancellationToken);
    }

    /// <summary>
    /// 检查配置键是否存在
    /// </summary>
    public async Task<bool> ExistsByConfigKeyAsync(string configKey, long? excludeConfigId = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var query = _dbClient.Queryable<SysConfig>()
            .Where(c => c.ConfigKey == configKey);

        if (excludeConfigId.HasValue)
        {
            query = query.Where(c => c.BasicId != excludeConfigId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    /// <summary>
    /// 根据配置组获取配置列表
    /// </summary>
    public async Task<List<SysConfig>> GetByConfigGroupAsync(string configGroup, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await _dbClient.Queryable<SysConfig>()
            .Where(c => c.ConfigGroup == configGroup)
            .OrderBy(c => c.Sort)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 获取所有系统配置
    /// </summary>
    public async Task<Dictionary<string, string?>> GetAllConfigsAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var configs = await _dbClient.Queryable<SysConfig>()
            .ToListAsync(cancellationToken);

        return configs.ToDictionary(c => c.ConfigKey, c => c.ConfigValue);
    }

    /// <summary>
    /// 批量更新配置
    /// </summary>
    public async Task<bool> BatchUpdateAsync(Dictionary<string, string> configs, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        foreach (var config in configs)
        {
            await _dbClient.Updateable<SysConfig>()
                .SetColumns(c => new SysConfig { ConfigValue = config.Value })
                .Where(c => c.ConfigKey == config.Key)
                .ExecuteCommandAsync(cancellationToken);
        }

        return true;
    }
}
