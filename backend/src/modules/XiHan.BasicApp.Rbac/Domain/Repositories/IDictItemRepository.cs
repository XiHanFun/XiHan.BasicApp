using XiHan.BasicApp.Rbac.Domain.Entities;
using XiHan.Framework.Domain.Repositories;

namespace XiHan.BasicApp.Rbac.Domain.Repositories;

/// <summary>
/// 平台字典项仓储接口
/// </summary>
public interface IDictItemRepository : IRepositoryBase<SysDictItem, long>
{
    /// <summary>
    /// 获取字典项列表
    /// </summary>
    Task<IReadOnlyList<SysDictItem>> GetByDictIdAsync(long dictId, long? tenantId = null, CancellationToken cancellationToken = default);
}
