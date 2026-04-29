#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IRoleHierarchyQueryService
// Guid:dceb7ccc-1f5b-4181-9707-ec3ebba9cce0
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/30 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

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
