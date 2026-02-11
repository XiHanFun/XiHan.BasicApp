#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:RoleApplicationService
// Guid:d2e9b5aa-a5e2-4e35-95d0-0eff3ba6f20c
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/01/31 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Mapster;
using XiHan.BasicApp.Rbac.Application.Services.Roles.Dtos;
using XiHan.BasicApp.Rbac.Domain.Repositories;
using XiHan.BasicApp.Rbac.Domain.Services;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Enums;
using XiHan.Framework.Application.Services;
using XiHan.Framework.Domain.Shared.Paging.Dtos;
using XiHan.Framework.Domain.Shared.Paging.Models;
using XiHan.Framework.Uow;

namespace XiHan.BasicApp.Rbac.Application.Services.Roles;

/// <summary>
/// 角色应用服务
/// </summary>
public class RoleApplicationService : CrudApplicationServiceBase<SysRole, SysRoleDto, long, CreateSysRoleDto, UpdateSysRoleDto, SysRolePageRequestDto>
{
    private readonly ISysRoleRepository _roleRepository;
    private readonly IRoleManagementService _roleManagementService;
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    /// <summary>
    /// 构造函数
    /// </summary>
    public RoleApplicationService(
        ISysRoleRepository roleRepository,
        IRoleManagementService roleManagementService,
        IUnitOfWorkManager unitOfWorkManager)
        : base(roleRepository)
    {
        _roleRepository = roleRepository;
        _roleManagementService = roleManagementService;
        _unitOfWorkManager = unitOfWorkManager;
    }

    /// <summary>
    /// 创建角色
    /// </summary>
    public override async Task<SysRoleDto> CreateAsync(CreateSysRoleDto input)
    {
        //using var uow = _unitOfWorkManager.Begin();

        // 检查角色编码是否已存在
        var exists = await _roleManagementService.IsRoleCodeDuplicateAsync(input.RoleCode, input.TenantId);
        if (exists)
        {
            throw new InvalidOperationException($"角色编码 {input.RoleCode} 已存在");
        }

        // 创建角色实体
        var role = input.Adapt<SysRole>();
        role.Status = YesOrNo.Yes;
        role.CreatedTime = DateTimeOffset.Now;

        // 保存角色
        role = await _roleRepository.SaveAsync(role);

        //await uow.CompleteAsync();

        return role.Adapt<SysRoleDto>();
    }

    /// <summary>
    /// 更新角色
    /// </summary>
    public override async Task<SysRoleDto> UpdateAsync(long id, UpdateSysRoleDto input)
    {
        //using var uow = _unitOfWorkManager.Begin();

        var role = await _roleRepository.GetByIdAsync(id);
        if (role == null)
        {
            throw new KeyNotFoundException($"角色 {id} 不存在");
        }

        // 映射更新数据
        input.Adapt(role);
        role.ModifiedTime = DateTimeOffset.Now;

        // 保存角色
        role = await _roleRepository.SaveAsync(role);

        //await uow.CompleteAsync();

        return role.Adapt<SysRoleDto>();
    }

    /// <summary>
    /// 删除角色
    /// </summary>
    public override async Task<bool> DeleteAsync(long id)
    {
        // 检查角色是否可以被删除
        var canDelete = await _roleManagementService.CanDeleteRoleAsync(id);
        if (!canDelete)
        {
            throw new InvalidOperationException($"角色 {id} 正在被使用，无法删除");
        }

        //using var uow = _unitOfWorkManager.Begin();
        await _roleRepository.DeleteByIdAsync(id);
        //await uow.CompleteAsync();

        return true;
    }

    /// <summary>
    /// 启用角色
    /// </summary>
    public async Task<bool> EnableAsync(long roleId)
    {
        //using var uow = _unitOfWorkManager.Begin();
        await _roleRepository.EnableRoleAsync(roleId);
        //await uow.CompleteAsync();
        return true;
    }

    /// <summary>
    /// 禁用角色
    /// </summary>
    public async Task<bool> DisableAsync(long roleId)
    {
        //using var uow = _unitOfWorkManager.Begin();
        await _roleRepository.DisableRoleAsync(roleId);
        //await uow.CompleteAsync();
        return true;
    }

    /// <summary>
    /// 为角色分配权限
    /// </summary>
    public async Task<bool> AssignPermissionsAsync(AssignPermissionsToRoleDto input)
    {
        //using var uow = _unitOfWorkManager.Begin();
        await _roleManagementService.AssignPermissionsToRoleAsync(input.RoleId, input.PermissionIds);
        //await uow.CompleteAsync();
        return true;
    }

    /// <summary>
    /// 为角色分配菜单
    /// </summary>
    public async Task<bool> AssignMenusAsync(AssignMenusToRoleDto input)
    {
        //using var uow = _unitOfWorkManager.Begin();
        await _roleManagementService.AssignMenusToRoleAsync(input.RoleId, input.MenuIds);
        //await uow.CompleteAsync();
        return true;
    }

    /// <summary>
    /// 获取租户下的角色列表
    /// </summary>
    public async Task<PageResultDtoBase<SysRoleDto>> GetRolesByTenantAsync(long tenantId, PageRequestDtoBase input)
    {
        var roles = await _roleRepository.GetRolesByTenantAsync(tenantId);
        var dtos = roles.Adapt<List<SysRoleDto>>();

        return new PageResultDtoBase<SysRoleDto>(dtos, new PageResultMetadata
        {
            PageIndex = 1,
            PageSize = dtos.Count,
            TotalCount = dtos.Count
        });
    }

    /// <summary>
    /// 获取用户的角色列表
    /// </summary>
    public async Task<List<SysRoleDto>> GetRolesByUserIdAsync(long userId)
    {
        var roles = await _roleRepository.GetRolesByUserIdAsync(userId);
        return roles.Adapt<List<SysRoleDto>>();
    }

    /// <summary>
    /// 根据角色编码获取角色
    /// </summary>
    public async Task<SysRoleDto?> GetByRoleCodeAsync(string roleCode, long? tenantId = null)
    {
        var role = await _roleRepository.GetByRoleCodeAsync(roleCode, tenantId);
        return role?.Adapt<SysRoleDto>();
    }
}
