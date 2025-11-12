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

namespace XiHan.BasicApp.Rbac.Services.Implementations;

/// <summary>
/// 角色服务实现
/// </summary>
public class RoleService : CrudApplicationServiceBase<SysRole, RoleDto, RbacIdType, CreateRoleDto, UpdateRoleDto>, IRoleService
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
        ISqlSugarDbContext dbContext) : base(roleRepository)
    {
        _roleRepository = roleRepository;
        _roleManager = roleManager;
        _dbContext = dbContext;
    }

    #region 业务特定方法

    /// <summary>
    /// 获取角色详情
    /// </summary>
    public async Task<RoleDetailDto?> GetDetailAsync(RbacIdType id)
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

        var role = new SysRole
        {
            RoleCode = input.RoleCode,
            RoleName = input.RoleName,
            RoleDescription = input.RoleDescription,
            RoleType = input.RoleType,
            Sort = input.Sort,
            Remark = input.Remark
        };

        await _roleRepository.AddAsync(role);

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
    public override async Task<RoleDto> UpdateAsync(RbacIdType id, UpdateRoleDto input)
    {
        var role = await _roleRepository.GetByIdAsync(id) ??
            throw new InvalidOperationException(ErrorMessageConstants.RoleNotFound);

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
    public override async Task<bool> DeleteAsync(RbacIdType id)
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
    public async Task<List<RbacIdType>> GetRoleMenuIdsAsync(RbacIdType roleId)
    {
        return await _roleRepository.GetRoleMenuIdsAsync(roleId);
    }

    /// <summary>
    /// 获取角色的权限ID列表
    /// </summary>
    public async Task<List<RbacIdType>> GetRolePermissionIdsAsync(RbacIdType roleId)
    {
        return await _roleRepository.GetRolePermissionIdsAsync(roleId);
    }

    #endregion 角色菜单和权限管理

    #region 映射方法实现

    /// <summary>
    /// 映射实体到DTO
    /// </summary>
    protected override Task<RoleDto> MapToEntityDtoAsync(SysRole entity)
    {
        return Task.FromResult(entity.ToDto());
    }

    /// <summary>
    /// 映射 RoleDto 到实体（基类方法）
    /// </summary>
    protected override Task<SysRole> MapToEntityAsync(RoleDto dto)
    {
        var entity = new SysRole
        {
            RoleCode = dto.RoleCode,
            RoleName = dto.RoleName,
            RoleDescription = dto.RoleDescription,
            RoleType = dto.RoleType,
            Status = dto.Status,
            Sort = dto.Sort,
            Remark = dto.Remark
        };

        return Task.FromResult(entity);
    }

    /// <summary>
    /// 映射 RoleDto 到现有实体（基类方法）
    /// </summary>
    protected override Task MapToEntityAsync(RoleDto dto, SysRole entity)
    {
        if (dto.RoleName != null) entity.RoleName = dto.RoleName;
        if (dto.RoleDescription != null) entity.RoleDescription = dto.RoleDescription;
        entity.RoleType = dto.RoleType;
        entity.Status = dto.Status;
        entity.Sort = dto.Sort;
        if (dto.Remark != null) entity.Remark = dto.Remark;

        return Task.CompletedTask;
    }

    /// <summary>
    /// 映射创建DTO到实体
    /// </summary>
    protected override Task<SysRole> MapToEntityAsync(CreateRoleDto createDto)
    {
        var entity = new SysRole
        {
            RoleCode = createDto.RoleCode,
            RoleName = createDto.RoleName,
            RoleDescription = createDto.RoleDescription,
            RoleType = createDto.RoleType,
            Sort = createDto.Sort,
            Remark = createDto.Remark
        };

        return Task.FromResult(entity);
    }

    /// <summary>
    /// 映射更新DTO到现有实体
    /// </summary>
    protected override Task MapToEntityAsync(UpdateRoleDto updateDto, SysRole entity)
    {
        if (updateDto.RoleName != null) entity.RoleName = updateDto.RoleName;
        if (updateDto.RoleDescription != null) entity.RoleDescription = updateDto.RoleDescription;
        if (updateDto.RoleType.HasValue) entity.RoleType = updateDto.RoleType.Value;
        if (updateDto.Status.HasValue) entity.Status = updateDto.Status.Value;
        if (updateDto.Sort.HasValue) entity.Sort = updateDto.Sort.Value;
        if (updateDto.Remark != null) entity.Remark = updateDto.Remark;

        return Task.CompletedTask;
    }

    #endregion 映射方法实现
}
