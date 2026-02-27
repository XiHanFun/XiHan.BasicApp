using XiHan.BasicApp.Rbac.Entities;
using XiHan.Framework.Domain.Repositories;

namespace XiHan.BasicApp.Rbac.Domain.Repositories;

/// <summary>
/// 租户聚合仓储接口
/// </summary>
public interface ITenantRepository : IAggregateRootRepository<SysTenant, long>
{
    /// <summary>
    /// 根据租户编码获取租户
    /// </summary>
    Task<SysTenant?> GetByTenantCodeAsync(string tenantCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// 校验租户编码是否已存在
    /// </summary>
    Task<bool> IsTenantCodeExistsAsync(string tenantCode, long? excludeTenantId = null, CancellationToken cancellationToken = default);
}
