#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysPermissionService
// Guid:5c2b3c4d-5e6f-7890-abcd-ef12345678ba
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/10/31 7:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Mapster;
using SqlSugar;
using XiHan.BasicApp.Core;
using XiHan.BasicApp.Rbac.Constants;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Extensions;
using XiHan.BasicApp.Rbac.Managers;
using XiHan.BasicApp.Rbac.Repositories.Permissions;
using XiHan.BasicApp.Rbac.Services.Permissions.Dtos;
using XiHan.Framework.Application.Services;

namespace XiHan.BasicApp.Rbac.Services.Permissions;

/// <summary>
/// 系统权限服务实现
/// </summary>
public class SysPermissionService : CrudApplicationServiceBase<SysPermission, PermissionDto, long, CreatePermissionDto, UpdatePermissionDto>, ISysPermissionService
{
    private readonly ISysPermissionRepository _permissionRepository;
    private readonly PermissionManager _permissionManager;

    /// <summary>
    /// 构造函数
    /// </summary>
    public SysPermissionService(
        ISysPermissionRepository permissionRepository,
        PermissionManager permissionManager) : base(permissionRepository)
    {
        _permissionRepository = permissionRepository;
        _permissionManager = permissionManager;
    }

    #region 重写基类方法

    /// <summary>
    /// 创建权限
    /// </summary>
    public override async Task<PermissionDto> CreateAsync(CreatePermissionDto input)
    {
        // 验证权限编码唯一性
        if (!await _permissionManager.IsPermissionCodeUniqueAsync(input.PermissionCode))
        {
            throw new InvalidOperationException(ErrorMessageConstants.PermissionCodeExists);
        }

        var permission = new SysPermission
        {
            PermissionCode = input.PermissionCode,
            PermissionName = input.PermissionName,
            PermissionDescription = input.PermissionDescription,
            PermissionType = input.PermissionType,
            PermissionValue = input.PermissionValue,
            Sort = input.Sort,
            Remark = input.Remark
        };

        await _permissionRepository.AddAsync(permission);

        return permission.Adapt<PermissionDto>();
    }

    /// <summary>
    /// 更新权限
    /// </summary>
    public override async Task<PermissionDto> UpdateAsync(long id, UpdatePermissionDto input)
    {
        var permission = await _permissionRepository.GetByIdAsync(id) ??
            throw new InvalidOperationException(ErrorMessageConstants.PermissionNotFound);

        // 更新权限信息
        if (input.PermissionName != null)
        {
            permission.PermissionName = input.PermissionName;
        }

        if (input.PermissionDescription != null)
        {
            permission.PermissionDescription = input.PermissionDescription;
        }

        if (input.PermissionType.HasValue)
        {
            permission.PermissionType = input.PermissionType.Value;
        }

        if (input.PermissionValue != null)
        {
            permission.PermissionValue = input.PermissionValue;
        }

        if (input.Status.HasValue)
        {
            permission.Status = input.Status.Value;
        }

        if (input.Sort.HasValue)
        {
            permission.Sort = input.Sort.Value;
        }

        if (input.Remark != null)
        {
            permission.Remark = input.Remark;
        }

        await _permissionRepository.UpdateAsync(permission);

        return permission.Adapt<PermissionDto>();
    }

    /// <summary>
    /// 删除权限
    /// </summary>
    public override async Task<bool> DeleteAsync(long id)
    {
        var permission = await _permissionRepository.GetByIdAsync(id) ??
            throw new InvalidOperationException(ErrorMessageConstants.PermissionNotFound);

        return await _permissionRepository.DeleteAsync(permission);
    }

    #endregion 重写基类方法

    #region 业务特定方法

    /// <summary>
    /// 根据权限编码获取权限
    /// </summary>
    public async Task<PermissionDto?> GetByPermissionCodeAsync(string permissionCode)
    {
        var permission = await _permissionRepository.GetByPermissionCodeAsync(permissionCode);
        return permission?.Adapt<PermissionDto>();
    }

    /// <summary>
    /// 获取角色的权限列表
    /// </summary>
    public async Task<List<PermissionDto>> GetByRoleIdAsync(long roleId)
    {
        var permissions = await _permissionRepository.GetByRoleIdAsync(roleId);
        return permissions.Adapt<List<PermissionDto>>();
    }

    /// <summary>
    /// 获取用户的权限列表
    /// </summary>
    public async Task<List<PermissionDto>> GetByUserIdAsync(long userId)
    {
        var permissions = await _permissionRepository.GetByUserIdAsync(userId);
        return permissions.Adapt<List<PermissionDto>>();
    }

    #endregion 业务特定方法
}
