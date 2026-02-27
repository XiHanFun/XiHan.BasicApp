using XiHan.BasicApp.Rbac.Entities;
using XiHan.Framework.Domain.Repositories;

namespace XiHan.BasicApp.Rbac.Domain.Repositories;

/// <summary>
/// 菜单聚合仓储接口
/// </summary>
public interface IMenuRepository : IAggregateRootRepository<SysMenu, long>
{
    /// <summary>
    /// 根据菜单编码获取菜单
    /// </summary>
    Task<SysMenu?> GetByMenuCodeAsync(string menuCode, long? tenantId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取角色菜单
    /// </summary>
    Task<IReadOnlyList<SysMenu>> GetRoleMenusAsync(long roleId, long? tenantId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取用户菜单
    /// </summary>
    Task<IReadOnlyList<SysMenu>> GetUserMenusAsync(long userId, long? tenantId = null, CancellationToken cancellationToken = default);
}
