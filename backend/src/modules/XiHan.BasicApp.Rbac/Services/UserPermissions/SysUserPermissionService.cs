#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysUserPermissionService
// Guid:ec2b3c4d-5e6f-7890-abcd-ef123456789e
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/12/28 19:40:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Mapster;
using XiHan.BasicApp.Core;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Enums;
using XiHan.BasicApp.Rbac.Extensions;
using XiHan.BasicApp.Rbac.Repositories.UserPermissions;
using XiHan.BasicApp.Rbac.Services.UserPermissions.Dtos;
using XiHan.Framework.Application.Services;

namespace XiHan.BasicApp.Rbac.Services.UserPermissions;

/// <summary>
/// 系统用户权限服务实现
/// </summary>
public class SysUserPermissionService : CrudApplicationServiceBase<SysUserPermission, UserPermissionDto, XiHanBasicAppIdType, CreateUserPermissionDto, UpdateUserPermissionDto>, ISysUserPermissionService
{
    private readonly ISysUserPermissionRepository _userPermissionRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public SysUserPermissionService(ISysUserPermissionRepository userPermissionRepository) : base(userPermissionRepository)
    {
        _userPermissionRepository = userPermissionRepository;
    }

    #region 业务特定方法

    /// <summary>
    /// 根据用户ID获取用户权限列表
    /// </summary>
    public async Task<List<UserPermissionDto>> GetByUserIdAsync(XiHanBasicAppIdType userId)
    {
        var userPermissions = await _userPermissionRepository.GetByUserIdAsync(userId);
        return userPermissions.Adapt<List<UserPermissionDto>>();
    }

    /// <summary>
    /// 根据用户ID和权限操作类型获取用户权限列表
    /// </summary>
    public async Task<List<UserPermissionDto>> GetByUserIdAndActionAsync(XiHanBasicAppIdType userId, PermissionAction permissionAction)
    {
        var userPermissions = await _userPermissionRepository.GetByUserIdAndActionAsync(userId, permissionAction);
        return userPermissions.Adapt<List<UserPermissionDto>>();
    }

    /// <summary>
    /// 根据权限ID获取用户权限列表
    /// </summary>
    public async Task<List<UserPermissionDto>> GetByPermissionIdAsync(XiHanBasicAppIdType permissionId)
    {
        var userPermissions = await _userPermissionRepository.GetByPermissionIdAsync(permissionId);
        return userPermissions.Adapt<List<UserPermissionDto>>();
    }

    /// <summary>
    /// 检查用户是否有指定权限的直授记录
    /// </summary>
    public async Task<UserPermissionDto?> GetByUserAndPermissionAsync(XiHanBasicAppIdType userId, XiHanBasicAppIdType permissionId)
    {
        var userPermission = await _userPermissionRepository.GetByUserAndPermissionAsync(userId, permissionId);
        return userPermission?.Adapt<UserPermissionDto>();
    }

    /// <summary>
    /// 获取用户的有效权限（未过期）
    /// </summary>
    public async Task<List<UserPermissionDto>> GetEffectivePermissionsAsync(XiHanBasicAppIdType userId)
    {
        var userPermissions = await _userPermissionRepository.GetEffectivePermissionsAsync(userId);
        return userPermissions.Adapt<List<UserPermissionDto>>();
    }

    /// <summary>
    /// 批量授予用户权限
    /// </summary>
    public async Task<bool> BatchGrantPermissionsAsync(BatchGrantUserPermissionsDto input)
    {
        foreach (var permissionId in input.PermissionIds)
        {
            // 检查是否已存在
            var existing = await _userPermissionRepository.GetByUserAndPermissionAsync(input.UserId, permissionId);

            if (existing != null)
            {
                // 更新现有记录
                existing.PermissionAction = input.PermissionAction;
                existing.EffectiveTime = input.EffectiveTime;
                existing.ExpirationTime = input.ExpirationTime;
                existing.Remark = input.Remark;
                existing.Status = YesOrNo.Yes;
                await _userPermissionRepository.UpdateAsync(existing);
            }
            else
            {
                // 创建新记录
                var userPermission = new SysUserPermission
                {
                    UserId = input.UserId,
                    PermissionId = permissionId,
                    PermissionAction = input.PermissionAction,
                    EffectiveTime = input.EffectiveTime,
                    ExpirationTime = input.ExpirationTime,
                    Remark = input.Remark
                };
                await _userPermissionRepository.AddAsync(userPermission);
            }
        }

        return true;
    }

    /// <summary>
    /// 批量删除用户的权限
    /// </summary>
    public async Task<int> DeleteByUserAndPermissionsAsync(XiHanBasicAppIdType userId, List<XiHanBasicAppIdType> permissionIds)
    {
        return await _userPermissionRepository.DeleteByUserAndPermissionsAsync(userId, permissionIds);
    }

    #endregion 业务特定方法
}
