#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IMenuAppService
// Guid:28ed5346-9b0c-4d80-9a84-2030c6471240
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/30 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

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
