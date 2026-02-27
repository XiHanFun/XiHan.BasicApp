using XiHan.BasicApp.Rbac.Domain.Repositories;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.Framework.Data.SqlSugar;
using XiHan.Framework.Data.SqlSugar.Repository;
using XiHan.Framework.Uow;

namespace XiHan.BasicApp.Rbac.Infrastructure.Repositories;

/// <summary>
/// 配置仓储实现
/// </summary>
public class ConfigRepository : SqlSugarAggregateRepository<SysConfig, long>, IConfigRepository
{
    private readonly ISqlSugarDbContext _dbContext;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="dbContext"></param>
    /// <param name="unitOfWorkManager"></param>
    public ConfigRepository(ISqlSugarDbContext dbContext, IUnitOfWorkManager unitOfWorkManager)
        : base(dbContext, unitOfWorkManager)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// 根据配置键获取配置
    /// </summary>
    /// <param name="configKey"></param>
    /// <param name="tenantId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<SysConfig?> GetByConfigKeyAsync(string configKey, long? tenantId = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(configKey);
        var query = _dbContext.CreateQueryable<SysConfig>()
            .Where(config => config.ConfigKey == configKey);

        if (tenantId.HasValue)
        {
            query = query.Where(config => config.TenantId == tenantId.Value);
        }
        else
        {
            query = query.Where(config => config.TenantId == null);
        }

        return await query.FirstAsync(cancellationToken);
    }
}
