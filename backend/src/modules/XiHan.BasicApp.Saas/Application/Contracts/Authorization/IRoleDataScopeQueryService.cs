// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.Framework.Application.Contracts.Services;

namespace XiHan.BasicApp.Saas.Application.Contracts;

/// <summary>
/// 角色数据范围查询应用服务接口
/// </summary>
public interface IRoleDataScopeQueryService : IApplicationService
{
    /// <summary>
    /// 获取角色数据范围列表
    /// </summary>
    /// <param name="roleId">角色主键</param>
    /// <param name="onlyValid">是否仅返回当前有效数据范围</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>角色数据范围列表</returns>
    Task<IReadOnlyList<RoleDataScopeListItemDto>> GetRoleDataScopesAsync(long roleId, bool onlyValid = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取角色数据范围详情
    /// </summary>
    /// <param name="id">角色数据范围主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>角色数据范围详情</returns>
    Task<RoleDataScopeDetailDto?> GetRoleDataScopeDetailAsync(long id, CancellationToken cancellationToken = default);
}
