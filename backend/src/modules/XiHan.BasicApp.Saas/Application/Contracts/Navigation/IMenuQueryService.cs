#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IMenuQueryService
// Guid:409ed48a-4cc3-4fb3-88cf-9fcf9a199b6b
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/30 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

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
