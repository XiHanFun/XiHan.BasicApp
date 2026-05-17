#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IMenuRouteQueryService
// Guid:d579391d-28ef-4224-a87d-f7a4d6b4772a
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/18 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

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
