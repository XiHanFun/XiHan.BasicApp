using XiHan.BasicApp.Rbac.Domain.Entities;
using XiHan.Framework.Domain.Repositories;

namespace XiHan.BasicApp.Rbac.Domain.Repositories;

/// <summary>
/// 用户直授权限关系仓储接口
/// </summary>
public interface IUserPermissionRepository : IRepositoryBase<SysUserPermission, long>
{
    /// <summary>
    /// 获取用户直授权限
    /// </summary>
    Task<IReadOnlyList<SysUserPermission>> GetByUserIdAsync(long userId, long? tenantId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除用户直授权限
    /// </summary>
    Task<bool> RemoveByUserIdAsync(long userId, long? tenantId = null, CancellationToken cancellationToken = default);
}
