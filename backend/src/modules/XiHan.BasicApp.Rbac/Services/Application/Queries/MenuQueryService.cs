#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:MenuQueryService
// Guid:a8b9c0d1-e2f3-4a5b-4c5d-7e8f9a0b1c2d
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/1/7 0:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Mapster;
using XiHan.BasicApp.Rbac.Dtos.Base;
using XiHan.BasicApp.Rbac.Repositories.Abstracts;
using XiHan.BasicApp.Rbac.Services.Domain;
using XiHan.Framework.Application.Services;
using XiHan.Framework.Domain.Paging.Dtos;

namespace XiHan.BasicApp.Rbac.Services.Application.Queries;

/// <summary>
/// 菜单查询服务（处理菜单的读操作 - CQRS）
/// </summary>
public class MenuQueryService : ApplicationServiceBase
{
    private readonly IMenuRepository _menuRepository;
    private readonly MenuDomainService _menuDomainService;

    /// <summary>
    /// 构造函数
    /// </summary>
    public MenuQueryService(
        IMenuRepository menuRepository,
        MenuDomainService menuDomainService)
    {
        _menuRepository = menuRepository;
        _menuDomainService = menuDomainService;
    }

    /// <summary>
    /// 根据ID获取菜单
    /// </summary>
    /// <param name="id">菜单ID</param>
    /// <returns>菜单DTO</returns>
    public async Task<RbacDtoBase?> GetByIdAsync(long id)
    {
        var menu = await _menuRepository.GetByIdAsync(id);
        return menu?.Adapt<RbacDtoBase>();
    }

    /// <summary>
    /// 根据菜单编码获取菜单
    /// </summary>
    /// <param name="menuCode">菜单编码</param>
    /// <returns>菜单DTO</returns>
    public async Task<RbacDtoBase?> GetByMenuCodeAsync(string menuCode)
    {
        var menu = await _menuRepository.GetByMenuCodeAsync(menuCode);
        return menu?.Adapt<RbacDtoBase>();
    }

    /// <summary>
    /// 获取用户的所有菜单
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns>菜单DTO列表</returns>
    public async Task<List<RbacDtoBase>> GetByUserIdAsync(long userId)
    {
        var menus = await _menuRepository.GetByUserIdAsync(userId);
        return menus.Adapt<List<RbacDtoBase>>();
    }

    /// <summary>
    /// 获取角色的所有菜单
    /// </summary>
    /// <param name="roleId">角色ID</param>
    /// <returns>菜单DTO列表</returns>
    public async Task<List<RbacDtoBase>> GetByRoleIdAsync(long roleId)
    {
        var menus = await _menuRepository.GetByRoleIdAsync(roleId);
        return menus.Adapt<List<RbacDtoBase>>();
    }

    /// <summary>
    /// 获取父菜单的所有子菜单
    /// </summary>
    /// <param name="parentId">父菜单ID</param>
    /// <returns>子菜单DTO列表</returns>
    public async Task<List<RbacDtoBase>> GetByParentIdAsync(long? parentId)
    {
        var menus = await _menuRepository.GetByParentIdAsync(parentId);
        return menus.Adapt<List<RbacDtoBase>>();
    }

    /// <summary>
    /// 获取所有根菜单
    /// </summary>
    /// <returns>根菜单DTO列表</returns>
    public async Task<List<RbacDtoBase>> GetRootMenusAsync()
    {
        var menus = await _menuRepository.GetRootMenusAsync();
        return menus.Adapt<List<RbacDtoBase>>();
    }

    /// <summary>
    /// 获取菜单树（包含所有子菜单）
    /// </summary>
    /// <param name="parentId">父菜单ID（null表示从根节点开始）</param>
    /// <returns>菜单树DTO列表</returns>
    public async Task<List<RbacDtoBase>> GetMenuTreeAsync(long? parentId = null)
    {
        var menus = await _menuDomainService.BuildMenuTreeAsync(parentId);
        return menus.Adapt<List<RbacDtoBase>>();
    }

    /// <summary>
    /// 获取用户的菜单树
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns>菜单树DTO列表</returns>
    public async Task<List<RbacDtoBase>> GetUserMenuTreeAsync(long userId)
    {
        // 通过 AuthorizationDomainService 获取用户菜单树
        // 需要在 AuthorizationDomainService 中实现完整逻辑
        var menus = await _menuRepository.GetByUserIdAsync(userId);
        return menus.Adapt<List<RbacDtoBase>>();
    }

    /// <summary>
    /// 获取菜单路径（从根到当前节点）
    /// </summary>
    /// <param name="menuId">菜单ID</param>
    /// <returns>菜单路径DTO列表（从根到叶子）</returns>
    public async Task<List<RbacDtoBase>> GetMenuPathAsync(long menuId)
    {
        var menuPath = await _menuDomainService.GetMenuPathAsync(menuId);
        return menuPath.Adapt<List<RbacDtoBase>>();
    }

    /// <summary>
    /// 检查是否有子菜单
    /// </summary>
    /// <param name="menuId">菜单ID</param>
    /// <returns>是否有子菜单</returns>
    public async Task<bool> HasChildrenAsync(long menuId)
    {
        return await _menuRepository.HasChildrenAsync(menuId);
    }

    /// <summary>
    /// 获取分页列表
    /// </summary>
    /// <param name="input">分页查询参数</param>
    /// <returns>分页响应</returns>
    public async Task<PageResponse<RbacDtoBase>> GetPagedAsync(PageQuery input)
    {
        var result = await _menuRepository.GetPagedAsync(input);
        var dtos = result.Items.Adapt<List<RbacDtoBase>>();
        return new PageResponse<RbacDtoBase>(dtos, result.PageData);
    }
}
