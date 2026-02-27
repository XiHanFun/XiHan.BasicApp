using XiHan.BasicApp.Rbac.Domain.Repositories;
using XiHan.BasicApp.Rbac.Domain.Entities;
using XiHan.Framework.Data.SqlSugar;
using XiHan.Framework.Data.SqlSugar.Repository;

namespace XiHan.BasicApp.Rbac.Infrastructure.Repositories;

/// <summary>
/// 字典项仓储实现
/// </summary>
public class DictItemRepository : SqlSugarRepositoryBase<SysDictItem, long>, IDictItemRepository
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="dbContext"></param>
    public DictItemRepository(ISqlSugarDbContext dbContext)
        : base(dbContext)
    {
    }

    /// <summary>
    /// 根据字典ID获取字典项
    /// </summary>
    /// <param name="dictId"></param>
    /// <param name="tenantId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<IReadOnlyList<SysDictItem>> GetByDictIdAsync(long dictId, long? tenantId = null, CancellationToken cancellationToken = default)
    {
        if (tenantId.HasValue)
        {
            return GetListAsync(item => item.DictId == dictId && item.TenantId == tenantId.Value, item => item.Sort, cancellationToken);
        }

        return GetListAsync(item => item.DictId == dictId, item => item.Sort, cancellationToken);
    }
}
