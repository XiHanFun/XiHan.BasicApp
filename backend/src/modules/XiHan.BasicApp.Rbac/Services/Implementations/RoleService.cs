#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:RoleService
// Guid:4c2b3c4d-5e6f-7890-abcd-ef12345678b9
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/10/31 6:55:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Rbac.Constants;
using XiHan.BasicApp.Rbac.Dtos.Roles;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Extensions;
using XiHan.BasicApp.Rbac.Managers;
using XiHan.BasicApp.Rbac.Repositories.Abstractions;
using XiHan.BasicApp.Rbac.Services.Abstractions;
using XiHan.Framework.Application.Services;
using XiHan.Framework.Data.SqlSugar;
using XiHan.Framework.Domain.Paging.Dtos;

namespace XiHan.BasicApp.Rbac.Services.Implementations;

/// <summary>
/// 角色服务实现
/// </summary>
public class RoleService : ApplicationServiceBase, IRoleService
{
    private readonly IRoleRepository _roleRepository;
    private readonly RoleManager _roleManager;
    private readonly ISqlSugarDbContext _dbContext;

    /// <summary>
    /// 构造函数
    /// </summary>
    public RoleService(
        IRoleRepository roleRepository,
        RoleManager roleManager,
        ISqlSugarDbContext dbContext)
    {
        _roleRepository = roleRepository;
        _roleManager = roleManager;
        _dbContext = dbContext;
    }

    /// <summary>
    /// 根据ID获取角色
    /// </summary>
    public async Task<RoleDto?> GetByIdAsync(long id)
    {
        var role = await _roleRepository.GetByIdAsync(id);
        return role?.ToDto();
    }

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
        return role?.ToDto();
    }

    /// <summary>
    /// 创建角色
    /// </summary>
    public async Task<RoleDto> CreateAsync(CreateRoleDto input)
    {
        // 验证角色编码唯一性
        if (!await _roleManager.IsRoleCodeUniqueAsync(input.RoleCode))
        {
            throw new InvalidOperationException(ErrorMessageConstants.RoleCodeExists);
        }

        var role = new SysRole
        {
            RoleCode = input.RoleCode,
            RoleName = input.RoleName,
            RoleDescription = input.RoleDescription,
            RoleType = input.RoleType,
            Sort = input.Sort,
            Remark = input.Remark
        };

        await _roleRepository.InsertAsync(role);

        // 分配菜单
        if (input.MenuIds.Any())
        {
            await AssignMenusAsync(new AssignRoleMenusDto
            {
                RoleId = role.BasicId,
                MenuIds = input.MenuIds
            });
        }

        // 分配权限
        if (input.PermissionIds.Any())
        {
            await AssignPermissionsAsync(new AssignRolePermissionsDto
            {
                RoleId = role.BasicId,
                PermissionIds = input.PermissionIds
            });
        }

        return role.ToDto();
    }

    /// <summary>
    /// 更新角色
    /// </summary>
    public async Task<RoleDto> UpdateAsync(UpdateRoleDto input)
    {
        var role = await _roleRepository.GetByIdAsync(input.BasicId);
        if (role == null)
        {
            throw new InvalidOperationException(ErrorMessageConstants.RoleNotFound);
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

        return role.ToDto();
    }

    /// <summary>
    /// 删除角色
    /// </summary>
    public async Task<bool> DeleteAsync(long id)
    {
        var role = await _roleRepository.GetByIdAsync(id);
        if (role == null)
        {
            throw new InvalidOperationException(ErrorMessageConstants.RoleNotFound);
        }

        // 检查是否可以删除
        if (!await _roleManager.CanDeleteAsync(id))
        {
            throw new InvalidOperationException(ErrorMessageConstants.RoleHasUsers);
        }

        return await _roleRepository.DeleteAsync(role);
    }

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
        if (input.MenuIds.Any())
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
        if (input.PermissionIds.Any())
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

    /// <summary>
    /// 分页查询角色
    /// </summary>
    public async Task<PageResponse<RoleDto>> GetPagedListAsync(PageQuery query)
    {
        var queryable = _roleRepository.Queryable();

        // 应用筛选条件
        if (query.Conditions != null && query.Conditions.Any())
        {
            foreach (var condition in query.Conditions)
            {
                if (condition.Field == "RoleName" && !string.IsNullOrEmpty(condition.Value?.ToString()))
                {
                    queryable = queryable.Where(r => r.RoleName.Contains(condition.Value.ToString()!));
                }
            }
        }

        // 应用排序
        if (query.Sorts != null && query.Sorts.Any())
        {
            foreach (var sort in query.Sorts)
            {
                queryable = sort.Direction == Paging.Enums.SortDirection.Ascending
                    ? queryable.OrderBy($"{sort.Field} ASC")
                    : queryable.OrderBy($"{sort.Field} DESC");
            }
        }
        else
        {
            queryable = queryable.OrderBy(r => r.Sort);
        }

        // 分页
        var total = await queryable.CountAsync();
        var items = await queryable
            .Skip((query.PageIndex - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        return new PageResponse<RoleDto>
        {
            Items = items.ToDto(),
            PageInfo = new PageInfo
            {
                PageIndex = query.PageIndex,
                PageSize = query.PageSize,
                Total = total
            }
        };
    }
}
