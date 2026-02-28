#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:RoleAppService
// Guid:e4cb6f6f-d53d-49fb-bfdd-6e57be306491
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/02/28 06:29:49
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Mapster;
using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Rbac.Application.Commands;
using XiHan.BasicApp.Rbac.Application.Dtos;
using XiHan.BasicApp.Rbac.Application.Queries;
using XiHan.BasicApp.Rbac.Domain.DomainServices;
using XiHan.BasicApp.Rbac.Domain.Enums;
using XiHan.BasicApp.Rbac.Domain.Repositories;
using XiHan.BasicApp.Rbac.Domain.Entities;
using XiHan.Framework.Application.Services;
using XiHan.Framework.Uow;
using XiHan.Framework.Uow.Options;

namespace XiHan.BasicApp.Rbac.Application.ApplicationServices.Implementations;

/// <summary>
/// 角色应用服务
/// </summary>
public class RoleAppService
    : CrudApplicationServiceBase<SysRole, RoleDto, long, RoleCreateDto, RoleUpdateDto, BasicAppPRDto>,
        IRoleAppService
{
    private readonly IRoleRepository _roleRepository;
    private readonly IRoleManager _roleManager;
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="roleRepository"></param>
    /// <param name="roleManager"></param>
    /// <param name="unitOfWorkManager"></param>
    public RoleAppService(
        IRoleRepository roleRepository,
        IRoleManager roleManager,
        IUnitOfWorkManager unitOfWorkManager)
        : base(roleRepository)
    {
        _roleRepository = roleRepository;
        _roleManager = roleManager;
        _unitOfWorkManager = unitOfWorkManager;
    }

    /// <summary>
    /// 根据角色编码获取角色
    /// </summary>
    /// <param name="query"></param>
    /// <returns></returns>
    public async Task<RoleDto?> GetByCodeAsync(RoleByCodeQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);
        var role = await _roleRepository.GetByRoleCodeAsync(query.RoleCode, query.TenantId);
        return role?.Adapt<RoleDto>();
    }

    /// <summary>
    /// 创建角色
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public override async Task<RoleDto> CreateAsync(RoleCreateDto input)
    {
        input.ValidateAnnotations();

        using var uow = _unitOfWorkManager.Begin(new XiHanUnitOfWorkOptions(), true);
        await _roleManager.EnsureRoleCodeUniqueAsync(input.RoleCode, null, input.TenantId);

        var role = new SysRole
        {
            TenantId = input.TenantId,
            RoleCode = input.RoleCode.Trim(),
            RoleName = input.RoleName.Trim(),
            RoleDescription = input.RoleDescription,
            RoleType = input.RoleType,
            DataScope = input.DataScope,
            Sort = input.Sort,
            Status = YesOrNo.Yes
        };

        var created = await _roleRepository.AddAsync(role);
        await uow.CompleteAsync();
        return created.Adapt<RoleDto>();
    }

    /// <summary>
    /// 更新角色
    /// </summary>
    /// <param name="id"></param>
    /// <param name="input"></param>
    /// <returns></returns>
    public override async Task<RoleDto> UpdateAsync(long id, RoleUpdateDto input)
    {
        input.ValidateAnnotations();

        using var uow = _unitOfWorkManager.Begin(new XiHanUnitOfWorkOptions(), true);
        var role = await _roleRepository.GetByIdAsync(id)
                   ?? throw new KeyNotFoundException($"未找到角色: {id}");

        role.RoleName = input.RoleName;
        role.RoleDescription = input.RoleDescription;
        role.DataScope = input.DataScope;
        role.Sort = input.Sort;
        role.Remark = input.Remark;

        if (input.Status == YesOrNo.Yes)
        {
            role.Enable();
        }
        else
        {
            role.Disable();
        }

        var updated = await _roleRepository.UpdateAsync(role);
        await uow.CompleteAsync();
        return updated.Adapt<RoleDto>();
    }

    /// <summary>
    /// 删除角色
    /// </summary>
    /// <param name="roleId"></param>
    /// <returns></returns>
    public override async Task<bool> DeleteAsync(long roleId)
    {
        if (roleId <= 0)
        {
            return false;
        }

        using var uow = _unitOfWorkManager.Begin(new XiHanUnitOfWorkOptions(), true);
        var role = await _roleRepository.GetByIdAsync(roleId);
        if (role is null)
        {
            return false;
        }

        await _roleRepository.DeleteAsync(role);
        await uow.CompleteAsync();
        return true;
    }

    /// <summary>
    /// 分配权限
    /// </summary>
    /// <param name="command"></param>
    /// <returns></returns>
    public async Task AssignPermissionsAsync(AssignRolePermissionsCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (command.RoleId <= 0)
        {
            throw new ArgumentException("角色 ID 无效");
        }

        using var uow = _unitOfWorkManager.Begin(new XiHanUnitOfWorkOptions(), true);
        var role = await _roleRepository.GetByIdAsync(command.RoleId)
                   ?? throw new KeyNotFoundException($"未找到角色: {command.RoleId}");

        await _roleManager.AssignPermissionsAsync(role, command.PermissionIds, command.TenantId ?? role.TenantId);
        await uow.CompleteAsync();
    }

    /// <summary>
    /// 分配菜单
    /// </summary>
    /// <param name="command"></param>
    /// <returns></returns>
    public async Task AssignMenusAsync(AssignRoleMenusCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (command.RoleId <= 0)
        {
            throw new ArgumentException("角色 ID 无效");
        }

        using var uow = _unitOfWorkManager.Begin(new XiHanUnitOfWorkOptions(), true);
        await _roleManager.AssignMenusAsync(command.RoleId, command.MenuIds, command.TenantId);
        await uow.CompleteAsync();
    }
}
