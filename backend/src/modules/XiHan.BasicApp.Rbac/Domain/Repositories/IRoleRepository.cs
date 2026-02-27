using XiHan.BasicApp.Rbac.Entities;
using XiHan.Framework.Domain.Repositories;

namespace XiHan.BasicApp.Rbac.Domain.Repositories;

/// <summary>
/// 角色聚合仓储接口
/// </summary>
public interface IRoleRepository : IAggregateRootRepository<SysRole, long>
{
    /// <summary>
    /// 根据角色编码获取角色
    /// </summary>
    Task<SysRole?> GetByRoleCodeAsync(string roleCode, long? tenantId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 校验角色编码是否已存在
    /// </summary>
    Task<bool> IsRoleCodeExistsAsync(string roleCode, long? excludeRoleId = null, long? tenantId = null, CancellationToken cancellationToken = default);
}
