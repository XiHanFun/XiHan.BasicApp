#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysConfigRepository
// Guid:b7b2c3d4-e5f6-7890-abcd-ef1234567896
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/12/28 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Enums;
using XiHan.Framework.Data.SqlSugar;
using XiHan.Framework.Data.SqlSugar.Repository;

namespace XiHan.BasicApp.Rbac.Repositories.Configs;

/// <summary>
/// 系统配置仓储实现
/// </summary>
public class SysConfigRepository : SqlSugarRepositoryBase<SysConfig, long>, ISysConfigRepository
{
    private readonly ISqlSugarDbContext _dbContext;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="dbContext">数据库上下文</param>
    public SysConfigRepository(ISqlSugarDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// 根据配置键获取配置
    /// </summary>
    public async Task<SysConfig?> GetByKeyAsync(string configKey)
    {
        return await GetFirstAsync(config => config.ConfigKey == configKey);
    }

    /// <summary>
    /// 根据配置类型获取配置列表
    /// </summary>
    public async Task<List<SysConfig>> GetByTypeAsync(ConfigType configType)
    {
        var result = await GetListAsync(config => config.ConfigType == configType);
        return [.. result.OrderBy(config => config.Sort)];
    }

    /// <summary>
    /// 根据租户ID获取配置列表
    /// </summary>
    public async Task<List<SysConfig>> GetByTenantIdAsync(long tenantId)
    {
        var result = await GetListAsync(config => config.TenantId == tenantId);
        return [.. result.OrderBy(config => config.Sort)];
    }

    /// <summary>
    /// 检查配置键是否存在
    /// </summary>
    public async Task<bool> ExistsByKeyAsync(string configKey, long? excludeId = null)
    {
        var query = _dbContext.GetClient().Queryable<SysConfig>().Where(config => config.ConfigKey == configKey);
        if (excludeId.HasValue)
        {
            query = query.Where(config => config.BasicId != excludeId.Value);
        }
        return await query.AnyAsync();
    }
}
