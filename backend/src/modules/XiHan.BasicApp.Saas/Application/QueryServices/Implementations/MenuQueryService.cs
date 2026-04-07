#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:MenuQueryService
// Guid:4c5d6e7f-8091-0123-cdef-012345678902
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/05 06:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Mapster;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Caching.Attributes;
using XiHan.Framework.Core.DependencyInjection.ServiceLifetimes;

namespace XiHan.BasicApp.Saas.Application.QueryServices;

/// <summary>
/// 菜单查询服务
/// </summary>
public class MenuQueryService : IMenuQueryService, ITransientDependency
{
    private readonly IMenuRepository _menuRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public MenuQueryService(IMenuRepository menuRepository)
    {
        _menuRepository = menuRepository;
    }

    /// <inheritdoc />
    [Cacheable(Key = "menu:id:{id}", ExpireSeconds = 300)]
    public async Task<MenuDto?> GetByIdAsync(long id)
    {
        var entity = await _menuRepository.GetByIdAsync(id);
        return entity?.Adapt<MenuDto>();
    }
}
