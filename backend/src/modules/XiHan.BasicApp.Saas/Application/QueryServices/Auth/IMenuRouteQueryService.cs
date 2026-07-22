// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Application.Dtos;

namespace XiHan.BasicApp.Saas.Application.QueryServices;

/// <summary>
/// 菜单路由查询服务
/// </summary>
public interface IMenuRouteQueryService
{
    /// <summary>
    /// 按授权快照获取菜单路由
    /// </summary>
    Task<List<MenuRouteDto>> GetRoutesAsync(AuthorizationSnapshot snapshot, CancellationToken cancellationToken = default);
}
