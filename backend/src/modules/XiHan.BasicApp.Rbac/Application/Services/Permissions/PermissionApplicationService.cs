#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:PermissionApplicationService
// Guid:60075139-8957-44b8-8d57-ec103d552228
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/01/31 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Mapster;
using XiHan.BasicApp.Rbac.Application.Services.Permissions.Dtos;
using XiHan.BasicApp.Rbac.Domain.Repositories;
using XiHan.BasicApp.Rbac.Domain.Services;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Enums;
using XiHan.Framework.Application.Services;
using XiHan.Framework.Uow;

namespace XiHan.BasicApp.Rbac.Application.Services.Permissions;

/// <summary>
/// 权限应用服务
/// </summary>
public class PermissionApplicationService : CrudApplicationServiceBase<SysPermission, SysPermissionDto, long, CreateSysPermissionDto, UpdateSysPermissionDto, SysPermissionPageRequestDto>
{
    private readonly ISysPermissionRepository _permissionRepository;
    private readonly IPermissionAuthorizationService _authorizationService;
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    /// <summary>
    /// 构造函数
    /// </summary>
    public PermissionApplicationService(
        ISysPermissionRepository permissionRepository,
        IPermissionAuthorizationService authorizationService,
        IUnitOfWorkManager unitOfWorkManager)
        : base(permissionRepository)
    {
        _permissionRepository = permissionRepository;
        _authorizationService = authorizationService;
        _unitOfWorkManager = unitOfWorkManager;
    }

    /// <summary>
    /// 创建权限
    /// </summary>
    public override async Task<SysPermissionDto> CreateAsync(CreateSysPermissionDto input)
    {
        //using var uow = _unitOfWorkManager.Begin();

        // 检查权限编码是否已存在
        var exists = await _permissionRepository.IsPermissionCodeExistsAsync(input.PermissionCode);
        if (exists)
        {
            throw new InvalidOperationException($"权限编码 {input.PermissionCode} 已存在");
        }

        // 检查资源和操作组合是否已存在
        var existingPermission = await _permissionRepository.GetByResourceAndOperationAsync(input.ResourceId, input.OperationId);
        if (existingPermission != null)
        {
            throw new InvalidOperationException($"资源 {input.ResourceId} 和操作 {input.OperationId} 的权限已存在");
        }

        // 创建权限实体
        var permission = input.Adapt<SysPermission>();
        permission.Status = YesOrNo.Yes;
        permission.CreatedTime = DateTimeOffset.Now;

        // 保存权限
        permission = await _permissionRepository.SaveAsync(permission);

        //await uow.CompleteAsync();

        return permission.Adapt<SysPermissionDto>();
    }

    /// <summary>
    /// 更新权限
    /// </summary>
    public override async Task<SysPermissionDto> UpdateAsync(long id, UpdateSysPermissionDto input)
    {
        //using var uow = _unitOfWorkManager.Begin();

        var permission = await _permissionRepository.GetByIdAsync(id);
        if (permission == null)
        {
            throw new KeyNotFoundException($"权限 {id} 不存在");
        }

        // 映射更新数据
        input.Adapt(permission);
        permission.ModifiedTime = DateTimeOffset.Now;

        // 保存权限
        permission = await _permissionRepository.SaveAsync(permission);

        //await uow.CompleteAsync();

        return permission.Adapt<SysPermissionDto>();
    }

    /// <summary>
    /// 启用权限
    /// </summary>
    public async Task<bool> EnableAsync(long permissionId)
    {
        //using var uow = _unitOfWorkManager.Begin();
        await _permissionRepository.EnablePermissionAsync(permissionId);
        //await uow.CompleteAsync();
        return true;
    }

    /// <summary>
    /// 禁用权限
    /// </summary>
    public async Task<bool> DisableAsync(long permissionId)
    {
        //using var uow = _unitOfWorkManager.Begin();
        await _permissionRepository.DisablePermissionAsync(permissionId);
        //await uow.CompleteAsync();
        return true;
    }

    /// <summary>
    /// 检查用户是否拥有权限
    /// </summary>
    public async Task<bool> CheckUserPermissionAsync(long userId, string permissionCode)
    {
        return await _authorizationService.HasPermissionAsync(userId, permissionCode);
    }

    /// <summary>
    /// 获取用户的所有权限
    /// </summary>
    public async Task<List<SysPermissionDto>> GetUserPermissionsAsync(long userId)
    {
        var permissions = await _permissionRepository.GetPermissionsByUserIdAsync(userId);
        return permissions.Adapt<List<SysPermissionDto>>();
    }

    /// <summary>
    /// 获取角色的所有权限
    /// </summary>
    public async Task<List<SysPermissionDto>> GetRolePermissionsAsync(long roleId)
    {
        var permissions = await _permissionRepository.GetPermissionsByRoleIdAsync(roleId);
        return permissions.Adapt<List<SysPermissionDto>>();
    }

    /// <summary>
    /// 获取资源下的所有权限
    /// </summary>
    public async Task<List<SysPermissionDto>> GetResourcePermissionsAsync(long resourceId)
    {
        var permissions = await _permissionRepository.GetPermissionsByResourceIdAsync(resourceId);
        return permissions.Adapt<List<SysPermissionDto>>();
    }

    /// <summary>
    /// 根据权限编码获取权限
    /// </summary>
    public async Task<SysPermissionDto?> GetByPermissionCodeAsync(string permissionCode)
    {
        var permission = await _permissionRepository.GetByPermissionCodeAsync(permissionCode);
        return permission?.Adapt<SysPermissionDto>();
    }
}
