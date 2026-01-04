#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ISysUserPermissionService
// Guid:dc2b3c4d-5e6f-7890-abcd-ef123456789d
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/12/28 19:35:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core;
using XiHan.BasicApp.Rbac.Enums;
using XiHan.BasicApp.Rbac.Services.UserPermissions.Dtos;
using XiHan.Framework.Application.Services.Abstracts;

namespace XiHan.BasicApp.Rbac.Services.UserPermissions;

/// <summary>
/// 系统用户权限服务接口
/// </summary>
public interface ISysUserPermissionService : ICrudApplicationService<UserPermissionDto, XiHanBasicAppIdType, CreateUserPermissionDto, UpdateUserPermissionDto>
{
    /// <summary>
    /// 根据用户ID获取用户权限列表
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns></returns>
    Task<List<UserPermissionDto>> GetByUserIdAsync(XiHanBasicAppIdType userId);

    /// <summary>
    /// 根据用户ID和权限操作类型获取用户权限列表
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="permissionAction">权限操作类型</param>
    /// <returns></returns>
    Task<List<UserPermissionDto>> GetByUserIdAndActionAsync(XiHanBasicAppIdType userId, PermissionAction permissionAction);

    /// <summary>
    /// 根据权限ID获取用户权限列表
    /// </summary>
    /// <param name="permissionId">权限ID</param>
    /// <returns></returns>
    Task<List<UserPermissionDto>> GetByPermissionIdAsync(XiHanBasicAppIdType permissionId);

    /// <summary>
    /// 检查用户是否有指定权限的直授记录
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="permissionId">权限ID</param>
    /// <returns></returns>
    Task<UserPermissionDto?> GetByUserAndPermissionAsync(XiHanBasicAppIdType userId, XiHanBasicAppIdType permissionId);

    /// <summary>
    /// 获取用户的有效权限（未过期）
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns></returns>
    Task<List<UserPermissionDto>> GetEffectivePermissionsAsync(XiHanBasicAppIdType userId);

    /// <summary>
    /// 批量授予用户权限
    /// </summary>
    /// <param name="input">批量授予DTO</param>
    /// <returns></returns>
    Task<bool> BatchGrantPermissionsAsync(BatchGrantUserPermissionsDto input);

    /// <summary>
    /// 批量删除用户的权限
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="permissionIds">权限ID列表</param>
    /// <returns></returns>
    Task<int> DeleteByUserAndPermissionsAsync(XiHanBasicAppIdType userId, List<XiHanBasicAppIdType> permissionIds);
}
