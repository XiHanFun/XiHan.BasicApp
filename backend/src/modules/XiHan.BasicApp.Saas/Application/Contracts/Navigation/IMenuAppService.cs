// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.Framework.Application.Contracts.Services;

namespace XiHan.BasicApp.Saas.Application.Contracts;

/// <summary>
/// 菜单命令应用服务接口
/// </summary>
public interface IMenuAppService : IApplicationService
{
    /// <summary>
    /// 创建菜单
    /// </summary>
    /// <param name="input">创建参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>菜单详情</returns>
    Task<MenuDetailDto> CreateMenuAsync(MenuCreateDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新菜单
    /// </summary>
    /// <param name="input">更新参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>菜单详情</returns>
    Task<MenuDetailDto> UpdateMenuAsync(MenuUpdateDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新菜单状态
    /// </summary>
    /// <param name="input">状态更新参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>菜单详情</returns>
    Task<MenuDetailDto> UpdateMenuStatusAsync(MenuStatusUpdateDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除菜单
    /// </summary>
    /// <param name="id">菜单主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task DeleteMenuAsync(long id, CancellationToken cancellationToken = default);
}
