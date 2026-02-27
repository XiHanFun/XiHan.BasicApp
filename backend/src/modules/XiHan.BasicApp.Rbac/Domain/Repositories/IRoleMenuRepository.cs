using XiHan.BasicApp.Rbac.Domain.Entities;
using XiHan.Framework.Domain.Repositories;

namespace XiHan.BasicApp.Rbac.Domain.Repositories;

/// <summary>
/// 角色菜单关系仓储接口
/// </summary>
public interface IRoleMenuRepository : IRepositoryBase<SysRoleMenu, long>
{
    /// <summary>
    /// 获取角色菜单关系
    /// </summary>
    Task<IReadOnlyList<SysRoleMenu>> GetByRoleIdAsync(long roleId, long? tenantId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除角色菜单关系
    /// </summary>
    Task<bool> RemoveByRoleIdAsync(long roleId, long? tenantId = null, CancellationToken cancellationToken = default);
}
