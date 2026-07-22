// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.Framework.Application.Contracts.Services;
using XiHan.Framework.Domain.Shared.Paging.Dtos;

namespace XiHan.BasicApp.Saas.Application.Contracts;

/// <summary>
/// 菜单查询应用服务接口
/// </summary>
public interface IMenuQueryService : IApplicationService
{
    /// <summary>
    /// 获取菜单分页列表
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>菜单分页列表</returns>
    Task<PageResultDtoBase<MenuListItemDto>> GetMenuPageAsync(MenuPageQueryDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取菜单列表（不分页，返回全部匹配项）
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>菜单列表</returns>
    Task<IReadOnlyList<MenuListItemDto>> GetMenuListAsync(MenuListQueryDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取菜单详情
    /// </summary>
    /// <param name="id">菜单主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>菜单详情</returns>
    Task<MenuDetailDto?> GetMenuDetailAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取菜单树
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>菜单树</returns>
    Task<IReadOnlyList<MenuTreeNodeDto>> GetMenuTreeAsync(MenuTreeQueryDto input, CancellationToken cancellationToken = default);
}
