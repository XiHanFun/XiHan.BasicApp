#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ISysPermissionService
// Guid:0b2b3c4d-5e6f-7890-abcd-ef12345678a5
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/10/31 5:15:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Services.Permissions.Dtos;
using XiHan.Framework.Application.Services;
using XiHan.Framework.Application.Services.Abstracts;
using XiHan.Framework.Domain.Paging.Dtos;

namespace XiHan.BasicApp.Rbac.Services.Permissions;

/// <summary>
/// 系统权限服务接口
/// </summary>
public interface ISysPermissionService : ICrudApplicationService<PermissionDto, long, CreatePermissionDto, UpdatePermissionDto>
{
    /// <summary>
    /// 根据权限编码获取权限
    /// </summary>
    /// <param name="permissionCode">权限编码</param>
    /// <returns></returns>
    Task<PermissionDto?> GetByPermissionCodeAsync(string permissionCode);

    /// <summary>
    /// 获取角色的权限列表
    /// </summary>
    /// <param name="roleId">角色ID</param>
    /// <returns></returns>
    Task<List<PermissionDto>> GetByRoleIdAsync(long roleId);

    /// <summary>
    /// 获取用户的权限列表
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns></returns>
    Task<List<PermissionDto>> GetByUserIdAsync(long userId);
}
