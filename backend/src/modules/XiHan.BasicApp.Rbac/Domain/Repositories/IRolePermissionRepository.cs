using XiHan.BasicApp.Rbac.Entities;
using XiHan.Framework.Domain.Repositories;

namespace XiHan.BasicApp.Rbac.Domain.Repositories;

/// <summary>
/// 角色权限关系仓储接口
/// </summary>
public interface IRolePermissionRepository : IRepositoryBase<SysRolePermission, long>
{
    /// <summary>
    /// 获取角色权限关系
    /// </summary>
    Task<IReadOnlyList<SysRolePermission>> GetByRoleIdAsync(long roleId, long? tenantId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除角色权限关系
    /// </summary>
    Task<bool> RemoveByRoleIdAsync(long roleId, long? tenantId = null, CancellationToken cancellationToken = default);
}
