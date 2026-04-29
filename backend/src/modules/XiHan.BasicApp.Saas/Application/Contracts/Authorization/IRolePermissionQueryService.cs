#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IRolePermissionQueryService
// Guid:c5e72718-9c62-44f7-8584-c4ac75fb1fde
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/30 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.Framework.Application.Contracts.Services;

namespace XiHan.BasicApp.Saas.Application.Contracts;

/// <summary>
/// 角色权限查询应用服务接口
/// </summary>
public interface IRolePermissionQueryService : IApplicationService
{
    /// <summary>
    /// 获取角色权限列表
    /// </summary>
    /// <param name="roleId">角色主键</param>
    /// <param name="onlyValid">是否仅返回有效授权</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>角色权限列表</returns>
    Task<IReadOnlyList<RolePermissionListItemDto>> GetRolePermissionsAsync(long roleId, bool onlyValid = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取角色权限详情
    /// </summary>
    /// <param name="id">角色权限绑定主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>角色权限详情</returns>
    Task<RolePermissionDetailDto?> GetRolePermissionDetailAsync(long id, CancellationToken cancellationToken = default);
}
