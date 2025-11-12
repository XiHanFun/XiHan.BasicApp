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

using SqlSugar;
using XiHan.BasicApp.Rbac.Constants;
using XiHan.BasicApp.Rbac.Dtos.Permissions;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Extensions;
using XiHan.BasicApp.Rbac.Managers;
using XiHan.BasicApp.Rbac.Repositories.Abstractions;
using XiHan.BasicApp.Rbac.Services.Abstractions;
using XiHan.Framework.Application.Services;

namespace XiHan.BasicApp.Rbac.Services.Implementations;

/// <summary>
/// 权限服务实现
/// </summary>
public class SysPermissionService : CrudApplicationServiceBase<SysPermission, PermissionDto, RbacIdType, CreatePermissionDto, UpdatePermissionDto>, ISysPermissionService
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

        return permission.ToDto();
    }

    /// <summary>
    /// 更新权限
    /// </summary>
    public override async Task<PermissionDto> UpdateAsync(RbacIdType id, UpdatePermissionDto input)
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

        return permission.ToDto();
    }

    /// <summary>
    /// 删除权限
    /// </summary>
    public override async Task<bool> DeleteAsync(RbacIdType id)
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
        return permission?.ToDto();
    }

    /// <summary>
    /// 获取角色的权限列表
    /// </summary>
    public async Task<List<PermissionDto>> GetByRoleIdAsync(RbacIdType roleId)
    {
        var permissions = await _permissionRepository.GetByRoleIdAsync(roleId);
        return permissions.ToDto();
    }

    /// <summary>
    /// 获取用户的权限列表
    /// </summary>
    public async Task<List<PermissionDto>> GetByUserIdAsync(RbacIdType userId)
    {
        var permissions = await _permissionRepository.GetByUserIdAsync(userId);
        return permissions.ToDto();
    }

    #endregion 业务特定方法

    #region 映射方法实现

    /// <summary>
    /// 映射实体到DTO
    /// </summary>
    protected override Task<PermissionDto> MapToEntityDtoAsync(SysPermission entity)
    {
        return Task.FromResult(entity.ToDto());
    }

    /// <summary>
    /// 映射 PermissionDto 到实体（基类方法）
    /// </summary>
    protected override Task<SysPermission> MapToEntityAsync(PermissionDto dto)
    {
        var entity = new SysPermission
        {
            PermissionCode = dto.PermissionCode,
            PermissionName = dto.PermissionName,
            PermissionDescription = dto.PermissionDescription,
            PermissionType = dto.PermissionType,
            PermissionValue = dto.PermissionValue,
            Status = dto.Status,
            Sort = dto.Sort,
            Remark = dto.Remark
        };

        return Task.FromResult(entity);
    }

    /// <summary>
    /// 映射 PermissionDto 到现有实体（基类方法）
    /// </summary>
    protected override Task MapToEntityAsync(PermissionDto dto, SysPermission entity)
    {
        if (dto.PermissionName != null) entity.PermissionName = dto.PermissionName;
        if (dto.PermissionDescription != null) entity.PermissionDescription = dto.PermissionDescription;
        entity.PermissionType = dto.PermissionType;
        if (dto.PermissionValue != null) entity.PermissionValue = dto.PermissionValue;
        entity.Status = dto.Status;
        entity.Sort = dto.Sort;
        if (dto.Remark != null) entity.Remark = dto.Remark;

        return Task.CompletedTask;
    }

    /// <summary>
    /// 映射创建DTO到实体
    /// </summary>
    protected override Task<SysPermission> MapToEntityAsync(CreatePermissionDto createDto)
    {
        var entity = new SysPermission
        {
            PermissionCode = createDto.PermissionCode,
            PermissionName = createDto.PermissionName,
            PermissionDescription = createDto.PermissionDescription,
            PermissionType = createDto.PermissionType,
            PermissionValue = createDto.PermissionValue,
            Sort = createDto.Sort,
            Remark = createDto.Remark
        };

        return Task.FromResult(entity);
    }

    /// <summary>
    /// 映射更新DTO到现有实体
    /// </summary>
    protected override Task MapToEntityAsync(UpdatePermissionDto updateDto, SysPermission entity)
    {
        if (updateDto.PermissionName != null) entity.PermissionName = updateDto.PermissionName;
        if (updateDto.PermissionDescription != null) entity.PermissionDescription = updateDto.PermissionDescription;
        if (updateDto.PermissionType.HasValue) entity.PermissionType = updateDto.PermissionType.Value;
        if (updateDto.PermissionValue != null) entity.PermissionValue = updateDto.PermissionValue;
        if (updateDto.Status.HasValue) entity.Status = updateDto.Status.Value;
        if (updateDto.Sort.HasValue) entity.Sort = updateDto.Sort.Value;
        if (updateDto.Remark != null) entity.Remark = updateDto.Remark;

        return Task.CompletedTask;
    }

    #endregion 映射方法实现
}
