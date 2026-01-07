#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:RoleCommandService
// Guid:a2b3c4d5-e6f7-4a5b-9c0d-1e2f3a4b5c6d
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/1/7 0:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Mapster;
using XiHan.BasicApp.Rbac.Dtos.Base;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Repositories.Abstracts;
using XiHan.BasicApp.Rbac.Services.Domain;
using XiHan.Framework.Application.Services;

namespace XiHan.BasicApp.Rbac.Services.Application.Commands;

/// <summary>
/// 角色命令服务（处理角色的写操作）
/// </summary>
public class RoleCommandService : CrudApplicationServiceBase<SysRole, RbacDtoBase, long, RbacDtoBase, RbacDtoBase>
{
    private readonly IRoleRepository _roleRepository;
    private readonly RoleDomainService _roleDomainService;

    /// <summary>
    /// 构造函数
    /// </summary>
    public RoleCommandService(
        IRoleRepository roleRepository,
        RoleDomainService roleDomainService)
        : base(roleRepository)
    {
        _roleRepository = roleRepository;
        _roleDomainService = roleDomainService;
    }

    /// <summary>
    /// 创建角色（重写以添加业务逻辑）
    /// </summary>
    public override async Task<RbacDtoBase> CreateAsync(RbacDtoBase input)
    {
        // 1. 业务验证
        if (!await _roleDomainService.IsRoleCodeUniqueAsync(input.RoleCode))
        {
            throw new InvalidOperationException($"角色编码 {input.RoleCode} 已存在");
        }

        // 2. 映射并创建
        var role = input.Adapt<SysRole>();

        // 3. 保存
        role = await _roleRepository.AddAsync(role);

        return await MapToEntityDtoAsync(role);
    }

    /// <summary>
    /// 更新角色（重写以添加业务逻辑）
    /// </summary>
    public override async Task<RbacDtoBase> UpdateAsync(long id, RbacDtoBase input)
    {
        // 1. 获取角色
        var role = await _roleRepository.GetByIdAsync(id);
        if (role == null)
        {
            throw new KeyNotFoundException($"角色 {id} 不存在");
        }

        // 2. 业务验证
        if (role.RoleCode != input.RoleCode && !await _roleDomainService.IsRoleCodeUniqueAsync(input.RoleCode, id))
        {
            throw new InvalidOperationException($"角色编码 {input.RoleCode} 已存在");
        }

        // 3. 更新实体
        input.Adapt(role);

        // 4. 保存
        role = await _roleRepository.UpdateAsync(role);

        return await MapToEntityDtoAsync(role);
    }

    /// <summary>
    /// 删除角色（重写以添加业务逻辑）
    /// </summary>
    public override async Task<bool> DeleteAsync(long id)
    {
        // 1. 验证是否可以删除
        await _roleDomainService.CanDeleteRoleAsync(id);

        // 2. 删除
        return await _roleRepository.DeleteByIdAsync(id);
    }

    /// <summary>
    /// 分配权限给角色
    /// </summary>
    /// <param name="roleId">角色ID</param>
    /// <param name="permissionIds">权限ID列表</param>
    /// <returns>是否成功</returns>
    public async Task<bool> AssignPermissionsToRoleAsync(long roleId, List<long> permissionIds)
    {
        // 1. 领域服务验证
        await _roleDomainService.AssignPermissionsToRoleAsync(roleId, permissionIds);

        // 2. 实际的关系维护（需要通过仓储或关系表服务）
        // 这里只是示例，实际实现需要根据具体的关系表处理

        return true;
    }

    /// <summary>
    /// 分配菜单给角色
    /// </summary>
    /// <param name="roleId">角色ID</param>
    /// <param name="menuIds">菜单ID列表</param>
    /// <returns>是否成功</returns>
    public async Task<bool> AssignMenusToRoleAsync(long roleId, List<long> menuIds)
    {
        // 1. 领域服务验证
        await _roleDomainService.AssignMenusToRoleAsync(roleId, menuIds);

        // 2. 实际的关系维护

        return true;
    }

    /// <summary>
    /// 更新角色状态
    /// </summary>
    /// <param name="roleId">角色ID</param>
    /// <param name="status">状态</param>
    /// <returns>是否成功</returns>
    public async Task<bool> UpdateStatusAsync(long roleId, Enums.YesOrNo status)
    {
        var role = await _roleRepository.GetByIdAsync(roleId);
        if (role == null)
        {
            throw new KeyNotFoundException($"角色 {roleId} 不存在");
        }

        role.Status = status;
        await _roleRepository.UpdateAsync(role);
        return true;
    }
}
