// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

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
