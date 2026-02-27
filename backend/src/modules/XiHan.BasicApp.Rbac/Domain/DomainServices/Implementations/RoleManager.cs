using XiHan.BasicApp.Rbac.Domain.Enums;
using XiHan.BasicApp.Rbac.Domain.Repositories;
using XiHan.BasicApp.Rbac.Domain.Entities;

namespace XiHan.BasicApp.Rbac.Domain.DomainServices.Implementations;

/// <summary>
/// 角色领域管理器实现
/// </summary>
public class RoleManager : IRoleManager
{
    private readonly IRoleRepository _roleRepository;
    private readonly IRolePermissionRepository _rolePermissionRepository;
    private readonly IRoleMenuRepository _roleMenuRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="roleRepository">角色仓储</param>
    /// <param name="rolePermissionRepository">角色权限仓储</param>
    /// <param name="roleMenuRepository">角色菜单仓储</param>
    public RoleManager(
        IRoleRepository roleRepository,
        IRolePermissionRepository rolePermissionRepository,
        IRoleMenuRepository roleMenuRepository)
    {
        _roleRepository = roleRepository;
        _rolePermissionRepository = rolePermissionRepository;
        _roleMenuRepository = roleMenuRepository;
    }

    /// <summary>
    /// 校验角色编码唯一性
    /// </summary>
    /// <param name="roleCode">角色编码</param>
    /// <param name="excludeRoleId">排除的角色ID</param>
    /// <param name="tenantId">租户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否唯一</returns>
    /// <exception cref="InvalidOperationException">角色编码已存在</exception>
    public async Task EnsureRoleCodeUniqueAsync(
        string roleCode,
        long? excludeRoleId = null,
        long? tenantId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(roleCode);
        var exists = await _roleRepository.IsRoleCodeExistsAsync(roleCode, excludeRoleId, tenantId, cancellationToken);
        if (exists)
        {
            throw new InvalidOperationException($"角色编码 '{roleCode}' 已存在");
        }
    }

    /// <summary>
    /// 分配角色权限
    /// </summary>
    /// <param name="role">角色</param>
    /// <param name="permissionIds">权限ID集合</param>
    /// <param name="tenantId">租户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    /// <exception cref="ArgumentNullException">角色为空</exception>
    public async Task AssignPermissionsAsync(
        SysRole role,
        IReadOnlyCollection<long> permissionIds,
        long? tenantId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(role);

        await _rolePermissionRepository.RemoveByRoleIdAsync(role.BasicId, tenantId ?? role.TenantId, cancellationToken);

        if (permissionIds.Count > 0)
        {
            var mappings = permissionIds
                .Distinct()
                .Select(permissionId => new SysRolePermission
                {
                    TenantId = tenantId ?? role.TenantId,
                    RoleId = role.BasicId,
                    PermissionId = permissionId,
                    Status = YesOrNo.Yes
                })
                .ToArray();

            await _rolePermissionRepository.AddRangeAsync(mappings, cancellationToken);
        }

        role.MarkPermissionsChanged(permissionIds.Distinct().ToArray());
        await _roleRepository.UpdateAsync(role, cancellationToken);
    }

    /// <summary>
    /// 分配角色菜单
    /// </summary>
    /// <param name="roleId">角色ID</param>
    /// <param name="menuIds">菜单ID集合</param>
    /// <param name="tenantId">租户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    /// <exception cref="ArgumentNullException">角色为空</exception>
    public async Task AssignMenusAsync(
        long roleId,
        IReadOnlyCollection<long> menuIds,
        long? tenantId = null,
        CancellationToken cancellationToken = default)
    {
        await _roleMenuRepository.RemoveByRoleIdAsync(roleId, tenantId, cancellationToken);

        if (menuIds.Count == 0)
        {
            return;
        }

        var mappings = menuIds
            .Distinct()
            .Select(menuId => new SysRoleMenu
            {
                TenantId = tenantId,
                RoleId = roleId,
                MenuId = menuId,
                Status = YesOrNo.Yes
            })
            .ToArray();

        await _roleMenuRepository.AddRangeAsync(mappings, cancellationToken);
    }
}
