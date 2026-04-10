#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:MenuDomainService
// Guid:4c5d6e7f-8091-0123-cdef-012345678904
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/05 06:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Events;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Core.DependencyInjection.ServiceLifetimes;
using XiHan.Framework.EventBus.Abstractions.Local;

namespace XiHan.BasicApp.Saas.Domain.DomainServices.Implementations;

/// <summary>
/// 菜单领域服务
/// </summary>
public class MenuDomainService : IMenuDomainService, ITransientDependency
{
    private readonly IMenuRepository _menuRepository;
    private readonly ILocalEventBus _localEventBus;

    /// <summary>
    /// 构造函数
    /// </summary>
    public MenuDomainService(IMenuRepository menuRepository, ILocalEventBus localEventBus)
    {
        _menuRepository = menuRepository;
        _localEventBus = localEventBus;
    }

    /// <inheritdoc />
    public async Task<SysMenu> CreateAsync(SysMenu menu)
    {
        var created = await _menuRepository.AddAsync(menu);
        await _localEventBus.PublishAsync(new MenuChangedDomainEvent(created.BasicId));
        return created;
    }

    /// <inheritdoc />
    public async Task<SysMenu> UpdateAsync(SysMenu menu)
    {
        var updated = await _menuRepository.UpdateAsync(menu);
        await _localEventBus.PublishAsync(new MenuChangedDomainEvent(updated.BasicId));
        return updated;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(long id)
    {
        var menu = await _menuRepository.GetByIdAsync(id);
        if (menu == null)
        {
            return false;
        }

        var result = await _menuRepository.DeleteAsync(menu);
        if (result)
        {
            await _localEventBus.PublishAsync(new MenuChangedDomainEvent(id));
        }
        return result;
    }
}
