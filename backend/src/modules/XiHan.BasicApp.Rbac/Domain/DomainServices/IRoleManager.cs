using XiHan.BasicApp.Rbac.Domain.Entities;

namespace XiHan.BasicApp.Rbac.Domain.DomainServices;

/// <summary>
/// 角色领域管理器
/// </summary>
public interface IRoleManager
{
    /// <summary>
    /// 校验角色编码唯一性
    /// </summary>
    Task EnsureRoleCodeUniqueAsync(string roleCode, long? excludeRoleId = null, long? tenantId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 分配角色权限
    /// </summary>
    Task AssignPermissionsAsync(SysRole role, IReadOnlyCollection<long> permissionIds, long? tenantId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 分配角色菜单
    /// </summary>
    Task AssignMenusAsync(long roleId, IReadOnlyCollection<long> menuIds, long? tenantId = null, CancellationToken cancellationToken = default);
}
