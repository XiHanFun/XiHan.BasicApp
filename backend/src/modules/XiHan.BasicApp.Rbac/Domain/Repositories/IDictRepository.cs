using XiHan.BasicApp.Rbac.Entities;
using XiHan.Framework.Domain.Repositories;

namespace XiHan.BasicApp.Rbac.Domain.Repositories;

/// <summary>
/// 平台字典仓储接口
/// </summary>
public interface IDictRepository : IAggregateRootRepository<SysDict, long>
{
    /// <summary>
    /// 根据字典编码获取字典
    /// </summary>
    Task<SysDict?> GetByDictCodeAsync(string dictCode, long? tenantId = null, CancellationToken cancellationToken = default);
}
