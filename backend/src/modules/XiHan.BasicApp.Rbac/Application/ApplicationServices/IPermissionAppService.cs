#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IPermissionAppService
// Guid:e775273e-95fb-4365-9279-88e791e78d8c
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/02/28 05:46:45
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Application.Dtos;
using XiHan.BasicApp.Rbac.Application.Queries;
using XiHan.BasicApp.Core.Dtos;
using XiHan.Framework.Application.Contracts.Services;

namespace XiHan.BasicApp.Rbac.Application.ApplicationServices;

/// <summary>
/// 权限应用服务
/// </summary>
public interface IPermissionAppService
    : ICrudApplicationService<PermissionDto, long, PermissionCreateDto, PermissionUpdateDto, BasicAppPRDto>
{
    /// <summary>
    /// 根据角色ID获取权限
    /// </summary>
    /// <param name="roleId"></param>
    /// <param name="tenantId"></param>
    /// <returns></returns>
    Task<IReadOnlyList<PermissionDto>> GetRolePermissionsAsync(long roleId, long? tenantId = null);

    /// <summary>
    /// 根据用户ID获取权限
    /// </summary>
    /// <param name="query"></param>
    /// <returns></returns>
    Task<IReadOnlyList<PermissionDto>> GetUserPermissionsAsync(UserPermissionQuery query);
}
