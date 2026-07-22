// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.Framework.Application.Contracts.Services;

namespace XiHan.BasicApp.Saas.Application.Contracts;

/// <summary>
/// 角色继承查询应用服务接口
/// </summary>
public interface IRoleHierarchyQueryService : IApplicationService
{
    /// <summary>
    /// 获取角色祖先链
    /// </summary>
    /// <param name="roleId">角色主键</param>
    /// <param name="includeSelf">是否包含自己</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>角色祖先链</returns>
    Task<IReadOnlyList<RoleHierarchyListItemDto>> GetRoleAncestorsAsync(long roleId, bool includeSelf = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取角色后代链
    /// </summary>
    /// <param name="roleId">角色主键</param>
    /// <param name="includeSelf">是否包含自己</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>角色后代链</returns>
    Task<IReadOnlyList<RoleHierarchyListItemDto>> GetRoleDescendantsAsync(long roleId, bool includeSelf = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取角色继承详情
    /// </summary>
    /// <param name="id">角色继承主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>角色继承详情</returns>
    Task<RoleHierarchyDetailDto?> GetRoleHierarchyDetailAsync(long id, CancellationToken cancellationToken = default);
}
