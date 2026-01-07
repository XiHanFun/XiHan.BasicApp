#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:MenuCommandService
// Guid:f7a8b9c0-d1e2-4f5a-3b4c-6d7e8f9a0b1c
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/1/7 0:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Mapster;
using XiHan.BasicApp.Rbac.Dtos.Base;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Repositories.Abstracts;
using XiHan.BasicApp.Rbac.Services.Domain;
using XiHan.Framework.Application.Services;

namespace XiHan.BasicApp.Rbac.Services.Application.Commands;

/// <summary>
/// 菜单命令服务（处理菜单的写操作）
/// </summary>
public class MenuCommandService : CrudApplicationServiceBase<SysMenu, RbacDtoBase, long, RbacDtoBase, RbacDtoBase>
{
    private readonly IMenuRepository _menuRepository;
    private readonly MenuDomainService _menuDomainService;

    /// <summary>
    /// 构造函数
    /// </summary>
    public MenuCommandService(
        IMenuRepository menuRepository,
        MenuDomainService menuDomainService)
        : base(menuRepository)
    {
        _menuRepository = menuRepository;
        _menuDomainService = menuDomainService;
    }

    /// <summary>
    /// 创建菜单（重写以添加业务逻辑）
    /// </summary>
    public override async Task<RbacDtoBase> CreateAsync(RbacDtoBase input)
    {
        // 1. 业务验证
        if (!await _menuDomainService.IsMenuCodeUniqueAsync(input.MenuCode))
        {
            throw new InvalidOperationException($"菜单编码 {input.MenuCode} 已存在");
        }

        // 2. 验证父菜单存在
        if (input.ParentId.HasValue)
        {
            var parentMenu = await _menuRepository.GetByIdAsync(input.ParentId.Value);
            if (parentMenu == null)
            {
                throw new InvalidOperationException($"父菜单 {input.ParentId} 不存在");
            }
        }

        // 3. 映射并创建
        var menu = input.Adapt<SysMenu>();

        // 4. 保存
        menu = await _menuRepository.AddAsync(menu);

        return await MapToEntityDtoAsync(menu);
    }

    /// <summary>
    /// 更新菜单（重写以添加业务逻辑）
    /// </summary>
    public override async Task<RbacDtoBase> UpdateAsync(long id, RbacDtoBase input)
    {
        // 1. 获取菜单
        var menu = await _menuRepository.GetByIdAsync(id);
        if (menu == null)
        {
            throw new KeyNotFoundException($"菜单 {id} 不存在");
        }

        // 2. 业务验证
        if (menu.MenuCode != input.MenuCode &&
            !await _menuDomainService.IsMenuCodeUniqueAsync(input.MenuCode, id))
        {
            throw new InvalidOperationException($"菜单编码 {input.MenuCode} 已存在");
        }

        // 3. 验证不能将自己设置为父菜单
        if (input.ParentId.HasValue && input.ParentId.Value == id)
        {
            throw new InvalidOperationException("不能将自己设置为父菜单");
        }

        // 4. 验证父菜单存在
        if (input.ParentId.HasValue)
        {
            var parentMenu = await _menuRepository.GetByIdAsync(input.ParentId.Value);
            if (parentMenu == null)
            {
                throw new InvalidOperationException($"父菜单 {input.ParentId} 不存在");
            }
        }

        // 5. 更新实体
        input.Adapt(menu);

        // 6. 保存
        menu = await _menuRepository.UpdateAsync(menu);

        return await MapToEntityDtoAsync(menu);
    }

    /// <summary>
    /// 删除菜单（重写以添加业务逻辑）
    /// </summary>
    public override async Task<bool> DeleteAsync(long id)
    {
        // 1. 验证是否可以删除
        await _menuDomainService.CanDeleteMenuAsync(id);

        // 2. 删除
        return await _menuRepository.DeleteByIdAsync(id);
    }

    /// <summary>
    /// 更新菜单排序
    /// </summary>
    /// <param name="menuId">菜单ID</param>
    /// <param name="sort">排序值</param>
    /// <returns>是否成功</returns>
    public async Task<bool> UpdateSortAsync(long menuId, int sort)
    {
        var menu = await _menuRepository.GetByIdAsync(menuId);
        if (menu == null)
        {
            throw new KeyNotFoundException($"菜单 {menuId} 不存在");
        }

        menu.Sort = sort;
        await _menuRepository.UpdateAsync(menu);
        return true;
    }

    /// <summary>
    /// 更新菜单状态
    /// </summary>
    /// <param name="menuId">菜单ID</param>
    /// <param name="status">状态</param>
    /// <returns>是否成功</returns>
    public async Task<bool> UpdateStatusAsync(long menuId, Enums.YesOrNo status)
    {
        var menu = await _menuRepository.GetByIdAsync(menuId);
        if (menu == null)
        {
            throw new KeyNotFoundException($"菜单 {menuId} 不存在");
        }

        menu.Status = status;
        await _menuRepository.UpdateAsync(menu);
        return true;
    }

    /// <summary>
    /// 移动菜单到新的父节点
    /// </summary>
    /// <param name="menuId">菜单ID</param>
    /// <param name="newParentId">新父菜单ID</param>
    /// <returns>是否成功</returns>
    public async Task<bool> MoveToAsync(long menuId, long? newParentId)
    {
        // 1. 获取菜单
        var menu = await _menuRepository.GetByIdAsync(menuId);
        if (menu == null)
        {
            throw new KeyNotFoundException($"菜单 {menuId} 不存在");
        }

        // 2. 验证不能移动到自己下面
        if (newParentId.HasValue && newParentId.Value == menuId)
        {
            throw new InvalidOperationException("不能将菜单移动到自己下面");
        }

        // 3. 验证新父菜单存在
        if (newParentId.HasValue)
        {
            var newParentMenu = await _menuRepository.GetByIdAsync(newParentId.Value);
            if (newParentMenu == null)
            {
                throw new InvalidOperationException($"目标父菜单 {newParentId} 不存在");
            }

            // 验证新父菜单不是当前菜单的子孙节点（防止循环引用）
            // 实际实现中需要递归检查
        }

        // 4. 更新父菜单ID
        menu.ParentId = newParentId;
        await _menuRepository.UpdateAsync(menu);

        return true;
    }
}
