#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysRoleService
// Guid:4c2b3c4d-5e6f-7890-abcd-ef12345678b9
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/10/31 6:55:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Mapster;
using SqlSugar;
using XiHan.BasicApp.Rbac.Constants;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Enums;
using XiHan.BasicApp.Rbac.Managers;
using XiHan.BasicApp.Rbac.Repositories.Menus;
using XiHan.BasicApp.Rbac.Repositories.Permissions;
using XiHan.BasicApp.Rbac.Repositories.Roles;
using XiHan.BasicApp.Rbac.Repositories.UserPermissions;
using XiHan.BasicApp.Rbac.Services.Roles.Dtos;
using XiHan.Framework.Application.Services;
using XiHan.Framework.Data.SqlSugar;

namespace XiHan.BasicApp.Rbac.Services.Roles;

/// <summary>
/// 系统角色服务实现
/// </summary>
public class SysRoleService : CrudApplicationServiceBase<SysRole, RoleDto, long, CreateRoleDto, UpdateRoleDto>, ISysRoleService
{
    private readonly ISysRoleRepository _roleRepository;
    private readonly ISysPermissionRepository _permissionRepository;
    private readonly ISysMenuRepository _menuRepository;
    private readonly ISysUserPermissionRepository _userPermissionRepository;
    private readonly RoleManager _roleManager;
    private readonly ISqlSugarDbContext _dbContext;

    /// <summary>
    /// 构造函数
    /// </summary>
    public SysRoleService(
        ISysRoleRepository roleRepository,
        ISysPermissionRepository permissionRepository,
        ISysMenuRepository menuRepository,
        ISysUserPermissionRepository userPermissionRepository,
        RoleManager roleManager,
        ISqlSugarDbContext dbContext) : base(roleRepository)
    {
        _roleRepository = roleRepository;
        _permissionRepository = permissionRepository;
        _menuRepository = menuRepository;
        _userPermissionRepository = userPermissionRepository;
        _roleManager = roleManager;
        _dbContext = dbContext;
    }

    #region 业务特定方法

    /// <summary>
    /// 获取角色详情
    /// </summary>
    public async Task<RoleDetailDto?> GetDetailAsync(long id)
    {
        var role = await _roleRepository.GetByIdAsync(id);
        if (role == null)
        {
            return null;
        }

        var menuIds = await _roleRepository.GetRoleMenuIdsAsync(id);
        var permissionIds = await _roleRepository.GetRolePermissionIdsAsync(id);
        var userCount = await _roleRepository.GetRoleUserCountAsync(id);

        return new RoleDetailDto
        {
            BasicId = role.BasicId,
            RoleCode = role.RoleCode,
            RoleName = role.RoleName,
            RoleDescription = role.RoleDescription,
            RoleType = role.RoleType,
            Status = role.Status,
            Sort = role.Sort,
            Remark = role.Remark,
            CreatedBy = role.CreatedBy,
            CreatedTime = role.CreatedTime,
            ModifiedBy = role.ModifiedBy,
            ModifiedTime = role.ModifiedTime,
            IsDeleted = role.IsDeleted,
            DeletedBy = role.DeletedBy,
            DeletedTime = role.DeletedTime,
            MenuIds = menuIds,
            PermissionIds = permissionIds,
            UserCount = userCount
        };
    }

    /// <summary>
    /// 根据角色编码获取角色
    /// </summary>
    public async Task<RoleDto?> GetByRoleCodeAsync(string roleCode)
    {
        var role = await _roleRepository.GetByRoleCodeAsync(roleCode);
        return role?.Adapt<RoleDto>();
    }

    #endregion 业务特定方法

    #region 重写基类方法

    /// <summary>
    /// 创建角色
    /// </summary>
    public override async Task<RoleDto> CreateAsync(CreateRoleDto input)
    {
        // 验证角色编码唯一性
        if (!await _roleManager.IsRoleCodeUniqueAsync(input.RoleCode))
        {
            throw new InvalidOperationException(ErrorMessageConstants.RoleCodeExists);
        }

        // 如果设置了父角色，验证父角色是否存在
        if (input.ParentRoleId.HasValue)
        {
            var parentRole = await _roleRepository.GetByIdAsync(input.ParentRoleId.Value) ?? throw new InvalidOperationException("指定的父角色不存在");
        }

        var role = new SysRole
        {
            ParentRoleId = input.ParentRoleId,
            RoleCode = input.RoleCode,
            RoleName = input.RoleName,
            RoleDescription = input.RoleDescription,
            RoleType = input.RoleType,
            Sort = input.Sort,
            Remark = input.Remark
        };

        await _roleRepository.AddAsync(role);

        // 分配菜单
        if (input.MenuIds.Count != 0)
        {
            await AssignMenusAsync(new AssignRoleMenusDto
            {
                RoleId = role.BasicId,
                MenuIds = input.MenuIds
            });
        }

        // 分配权限
        if (input.PermissionIds.Count != 0)
        {
            await AssignPermissionsAsync(new AssignRolePermissionsDto
            {
                RoleId = role.BasicId,
                PermissionIds = input.PermissionIds
            });
        }

        return role.Adapt<RoleDto>();
    }

    /// <summary>
    /// 更新角色
    /// </summary>
    public override async Task<RoleDto> UpdateAsync(long id, UpdateRoleDto input)
    {
        var role = await _roleRepository.GetByIdAsync(id) ??
            throw new InvalidOperationException(ErrorMessageConstants.RoleNotFound);

        // 如果要更新父角色，需要检查循环继承
        if (input.ParentRoleId.HasValue)
        {
            if (await _roleRepository.WouldCreateCycleAsync(id, input.ParentRoleId.Value))
            {
                throw new InvalidOperationException("设置该父角色将形成循环继承，操作被拒绝");
            }

            // 检查父角色是否存在
            var parentRole = await _roleRepository.GetByIdAsync(input.ParentRoleId.Value) ?? throw new InvalidOperationException("指定的父角色不存在");
            role.ParentRoleId = input.ParentRoleId;
        }

        // 更新角色信息
        if (input.RoleName != null)
        {
            role.RoleName = input.RoleName;
        }

        if (input.RoleDescription != null)
        {
            role.RoleDescription = input.RoleDescription;
        }

        if (input.RoleType.HasValue)
        {
            role.RoleType = input.RoleType.Value;
        }

        if (input.Status.HasValue)
        {
            role.Status = input.Status.Value;
        }

        if (input.Sort.HasValue)
        {
            role.Sort = input.Sort.Value;
        }

        if (input.Remark != null)
        {
            role.Remark = input.Remark;
        }

        await _roleRepository.UpdateAsync(role);

        return role.Adapt<RoleDto>();
    }

    /// <summary>
    /// 删除角色
    /// </summary>
    public override async Task<bool> DeleteAsync(long id)
    {
        var role = await _roleRepository.GetByIdAsync(id) ??
            throw new InvalidOperationException(ErrorMessageConstants.RoleNotFound);

        // 检查是否可以删除
        if (!await _roleManager.CanDeleteAsync(id))
        {
            throw new InvalidOperationException(ErrorMessageConstants.RoleHasUsers);
        }

        return await _roleRepository.DeleteAsync(role);
    }

    #endregion 重写基类方法

    #region 角色菜单和权限管理

    /// <summary>
    /// 分配菜单
    /// </summary>
    public async Task<bool> AssignMenusAsync(AssignRoleMenusDto input)
    {
        var client = _dbContext.GetClient();

        // 删除原有菜单
        await client.Deleteable<SysRoleMenu>()
            .Where(rm => rm.RoleId == input.RoleId)
            .ExecuteCommandAsync();

        // 添加新菜单
        if (input.MenuIds.Count != 0)
        {
            var roleMenus = input.MenuIds.Select(menuId => new SysRoleMenu
            {
                RoleId = input.RoleId,
                MenuId = menuId
            }).ToList();

            await client.Insertable(roleMenus).ExecuteCommandAsync();
        }

        return true;
    }

    /// <summary>
    /// 分配权限
    /// </summary>
    public async Task<bool> AssignPermissionsAsync(AssignRolePermissionsDto input)
    {
        var client = _dbContext.GetClient();

        // 删除原有权限
        await client.Deleteable<SysRolePermission>()
            .Where(rp => rp.RoleId == input.RoleId)
            .ExecuteCommandAsync();

        // 添加新权限
        if (input.PermissionIds.Count != 0)
        {
            var rolePermissions = input.PermissionIds.Select(permissionId => new SysRolePermission
            {
                RoleId = input.RoleId,
                PermissionId = permissionId
            }).ToList();

            await client.Insertable(rolePermissions).ExecuteCommandAsync();
        }

        return true;
    }

    /// <summary>
    /// 获取角色的菜单ID列表
    /// </summary>
    public async Task<List<long>> GetRoleMenuIdsAsync(long roleId)
    {
        return await _roleRepository.GetRoleMenuIdsAsync(roleId);
    }

    /// <summary>
    /// 获取角色的权限ID列表
    /// </summary>
    public async Task<List<long>> GetRolePermissionIdsAsync(long roleId)
    {
        return await _roleRepository.GetRolePermissionIdsAsync(roleId);
    }

    #endregion 角色菜单和权限管理

    #region 角色继承

    /// <summary>
    /// 设置父角色（建立继承关系）
    /// </summary>
    public async Task<bool> SetParentRoleAsync(long roleId, long? parentRoleId)
    {
        var role = await _roleRepository.GetByIdAsync(roleId) ??
            throw new InvalidOperationException(ErrorMessageConstants.RoleNotFound);

        // 如果要设置父角色，需要检查循环继承
        if (parentRoleId.HasValue)
        {
            if (await _roleRepository.WouldCreateCycleAsync(roleId, parentRoleId.Value))
            {
                throw new InvalidOperationException("设置该父角色将形成循环继承，操作被拒绝");
            }

            // 检查父角色是否存在
            var parentRole = await _roleRepository.GetByIdAsync(parentRoleId.Value) ?? throw new InvalidOperationException("指定的父角色不存在");
        }

        role.ParentRoleId = parentRoleId;
        await _roleRepository.UpdateAsync(role);

        return true;
    }

    /// <summary>
    /// 获取角色的所有权限（包括继承的权限）
    /// </summary>
    public async Task<List<SysPermission>> GetRolePermissionsWithInheritanceAsync(long roleId, bool includeInherited = true)
    {
        var permissions = new List<SysPermission>();

        // 1. 获取角色直接拥有的权限
        var directPermissions = await _permissionRepository.GetByRoleIdAsync(roleId);
        permissions.AddRange(directPermissions);

        // 2. 如果需要包括继承的权限
        if (includeInherited)
        {
            var inheritedPermissions = await GetInheritedPermissionsAsync(roleId);
            permissions.AddRange(inheritedPermissions);
        }

        return [.. permissions.DistinctBy(p => p.BasicId)];
    }

    /// <summary>
    /// 获取角色的所有菜单（包括继承的菜单）
    /// </summary>
    public async Task<List<SysMenu>> GetRoleMenusWithInheritanceAsync(long roleId, bool includeInherited = true)
    {
        var menus = new List<SysMenu>();

        // 1. 获取角色直接拥有的菜单
        var menuIds = await _roleRepository.GetRoleMenuIdsAsync(roleId);
        if (menuIds.Count != 0)
        {
            var directMenus = await _menuRepository.GetListAsync(m => menuIds.Contains(m.BasicId));
            menus.AddRange(directMenus);
        }

        // 2. 如果需要包括继承的菜单
        if (includeInherited)
        {
            var inheritedMenus = await GetInheritedMenusAsync(roleId);
            menus.AddRange(inheritedMenus);
        }

        return [.. menus.DistinctBy(m => m.BasicId)];
    }

    /// <summary>
    /// 获取用户的所有权限（包括角色继承的权限）
    /// </summary>
    public async Task<List<SysPermission>> GetUserPermissionsAsync(long userId)
    {
        var allPermissions = new List<SysPermission>();

        // 1. 获取用户的所有角色
        var userRoles = await _roleRepository.GetByUserIdAsync(userId);

        // 2. 获取每个角色的权限（包括继承）
        foreach (var role in userRoles)
        {
            var rolePermissions = await GetRolePermissionsWithInheritanceAsync(role.BasicId, true);
            allPermissions.AddRange(rolePermissions);
        }

        // 3. 获取用户直接授予的权限并处理授予/禁用逻辑
        var userPermissions = await _dbContext.GetClient()
            .Queryable<SysUserPermission>()
            .Where(up => up.UserId == userId && up.Status == YesOrNo.Yes)
            .LeftJoin<SysPermission>((up, p) => up.PermissionId == p.BasicId)
            .Where((up, p) => p.Status == YesOrNo.Yes)
            .Select((up, p) => new { Permission = p, up.PermissionAction })
            .ToListAsync();

        // 4. 处理授予和禁用操作
        foreach (var item in userPermissions)
        {
            if (item.PermissionAction == PermissionAction.Grant)
            {
                // 添加授予的权限
                if (!allPermissions.Any(p => p.BasicId == item.Permission.BasicId))
                {
                    allPermissions.Add(item.Permission);
                }
            }
            else if (item.PermissionAction == PermissionAction.Deny)
            {
                // 移除被禁用的权限
                allPermissions.RemoveAll(p => p.BasicId == item.Permission.BasicId);
            }
        }

        return [.. allPermissions.DistinctBy(p => p.BasicId)];
    }

    /// <summary>
    /// 获取用户的所有菜单（包括角色继承的菜单）
    /// </summary>
    public async Task<List<SysMenu>> GetUserMenusAsync(long userId)
    {
        var allMenus = new List<SysMenu>();

        // 获取用户的所有角色
        var userRoles = await _roleRepository.GetByUserIdAsync(userId);

        // 获取每个角色的菜单（包括继承）
        foreach (var role in userRoles)
        {
            var roleMenus = await GetRoleMenusWithInheritanceAsync(role.BasicId, true);
            allMenus.AddRange(roleMenus);
        }

        return [.. allMenus.DistinctBy(m => m.BasicId)];
    }

    /// <summary>
    /// 检查用户是否拥有指定权限（考虑继承和直接授予/禁用）
    /// </summary>
    public async Task<bool> HasPermissionAsync(long userId, string permissionCode)
    {
        // 1. 检查用户直接禁用的权限（最高优先级）
        var userDeniedPermissions = await _dbContext.GetClient()
            .Queryable<SysUserPermission>()
            .Where(up => up.UserId == userId
                && up.PermissionAction == PermissionAction.Deny
                && up.Status == YesOrNo.Yes)
            .LeftJoin<SysPermission>((up, p) => up.PermissionId == p.BasicId)
            .Where((up, p) => p.PermissionCode == permissionCode)
            .AnyAsync();

        if (userDeniedPermissions) return false;

        // 2. 检查用户直接授予的权限
        var userGrantedPermissions = await _dbContext.GetClient()
            .Queryable<SysUserPermission>()
            .Where(up => up.UserId == userId
                && up.PermissionAction == PermissionAction.Grant
                && up.Status == YesOrNo.Yes)
            .LeftJoin<SysPermission>((up, p) => up.PermissionId == p.BasicId)
            .Where((up, p) => p.PermissionCode == permissionCode)
            .AnyAsync();

        if (userGrantedPermissions) return true;

        // 3. 检查角色权限（含继承）
        var userPermissions = await GetUserPermissionsAsync(userId);
        return userPermissions.Any(p => p.PermissionCode == permissionCode);
    }

    /// <summary>
    /// 批量检查用户权限
    /// </summary>
    public async Task<Dictionary<string, bool>> BatchCheckPermissionsAsync(long userId, params string[] permissionCodes)
    {
        var result = new Dictionary<string, bool>();

        foreach (var permissionCode in permissionCodes)
        {
            result[permissionCode] = await HasPermissionAsync(userId, permissionCode);
        }

        return result;
    }

    /// <summary>
    /// 获取角色继承链（从当前角色到根角色）
    /// </summary>
    public async Task<List<RoleDto>> GetRoleInheritanceChainAsync(long roleId)
    {
        var chain = new List<RoleDto>();
        var parentRoles = await _roleRepository.GetParentRolesAsync(roleId);

        foreach (var role in parentRoles)
        {
            chain.Add(role.Adapt<RoleDto>());
        }

        return chain;
    }

    /// <summary>
    /// 获取角色树（包含子角色）
    /// </summary>
    public async Task<List<RoleDto>> GetRoleTreeAsync(long? parentRoleId = null)
    {
        var roles = await _roleRepository.GetRoleTreeAsync(parentRoleId);
        return [.. roles.Select(r => r.Adapt<RoleDto>())];
    }

    /// <summary>
    /// 递归获取继承的权限
    /// </summary>
    private async Task<List<SysPermission>> GetInheritedPermissionsAsync(long roleId)
    {
        var inheritedPermissions = new List<SysPermission>();

        // 获取父角色ID列表
        var parentRoleIds = await _roleRepository.GetParentRoleIdsAsync(roleId);

        foreach (var parentRoleId in parentRoleIds)
        {
            // 获取父角色的直接权限
            var parentPermissions = await _permissionRepository.GetByRoleIdAsync(parentRoleId);
            inheritedPermissions.AddRange(parentPermissions);
        }

        return inheritedPermissions;
    }

    /// <summary>
    /// 递归获取继承的菜单
    /// </summary>
    private async Task<List<SysMenu>> GetInheritedMenusAsync(long roleId)
    {
        var inheritedMenus = new List<SysMenu>();

        // 获取父角色ID列表
        var parentRoleIds = await _roleRepository.GetParentRoleIdsAsync(roleId);

        foreach (var parentRoleId in parentRoleIds)
        {
            // 获取父角色的菜单
            var menuIds = await _roleRepository.GetRoleMenuIdsAsync(parentRoleId);
            if (menuIds.Count != 0)
            {
                var parentMenus = await _menuRepository.GetListAsync(m => menuIds.Contains(m.BasicId));
                inheritedMenus.AddRange(parentMenus);
            }
        }

        return inheritedMenus;
    }

    #endregion 角色继承
}
