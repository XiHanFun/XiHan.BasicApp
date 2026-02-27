using XiHan.BasicApp.Rbac.Entities;
using XiHan.Framework.Domain.Repositories;

namespace XiHan.BasicApp.Rbac.Domain.Repositories;

/// <summary>
/// 平台配置仓储接口
/// </summary>
public interface IConfigRepository : IAggregateRootRepository<SysConfig, long>
{
    /// <summary>
    /// 根据配置键获取配置
    /// </summary>
    Task<SysConfig?> GetByConfigKeyAsync(string configKey, long? tenantId = null, CancellationToken cancellationToken = default);
}
