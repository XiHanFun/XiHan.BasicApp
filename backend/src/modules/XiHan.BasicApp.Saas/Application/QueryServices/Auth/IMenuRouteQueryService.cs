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

    /// <summary>
    /// 按授权快照获取当前用户可用的按钮码
    /// </summary>
    /// <remarks>
    /// 按钮由各模块页面登记表播种成 <c>MenuType.Button</c> 的菜单行，行上带按钮码与权限码。
    /// 前端只认按钮码，权限码不出现在客户端。
    /// </remarks>
    /// <param name="snapshot">授权快照</param>
    /// <param name="deniedPermissionCodes">额外禁用的权限码（如模仿态禁用清单）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>可用按钮码</returns>
    Task<List<string>> GetGrantedButtonCodesAsync(
        AuthorizationSnapshot snapshot,
        IReadOnlySet<string>? deniedPermissionCodes = null,
        CancellationToken cancellationToken = default);
}
