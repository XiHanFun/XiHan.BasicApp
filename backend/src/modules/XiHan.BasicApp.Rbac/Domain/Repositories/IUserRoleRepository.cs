using XiHan.BasicApp.Rbac.Entities;
using XiHan.Framework.Domain.Repositories;

namespace XiHan.BasicApp.Rbac.Domain.Repositories;

/// <summary>
/// 用户角色关系仓储接口
/// </summary>
public interface IUserRoleRepository : IRepositoryBase<SysUserRole, long>
{
    /// <summary>
    /// 获取用户角色关系
    /// </summary>
    Task<IReadOnlyList<SysUserRole>> GetByUserIdAsync(long userId, long? tenantId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除用户所有角色关系
    /// </summary>
    Task<bool> RemoveByUserIdAsync(long userId, long? tenantId = null, CancellationToken cancellationToken = default);
}
