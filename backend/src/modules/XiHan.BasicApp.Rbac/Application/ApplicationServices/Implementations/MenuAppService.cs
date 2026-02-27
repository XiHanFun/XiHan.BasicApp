using Mapster;
using XiHan.BasicApp.Rbac.Application.Dtos;
using XiHan.BasicApp.Rbac.Application.Queries;
using XiHan.BasicApp.Rbac.Domain.Repositories;
using XiHan.Framework.Application.Services;

namespace XiHan.BasicApp.Rbac.Application.ApplicationServices.Implementations;

/// <summary>
/// 菜单应用服务
/// </summary>
public class MenuAppService : ApplicationServiceBase, IMenuAppService
{
    private readonly IMenuRepository _menuRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="menuRepository"></param>
    public MenuAppService(IMenuRepository menuRepository)
    {
        _menuRepository = menuRepository;
    }

    /// <summary>
    /// 根据菜单ID获取菜单
    /// </summary>
    /// <param name="menuId"></param>
    /// <returns></returns>
    public async Task<MenuDto?> GetByIdAsync(long menuId)
    {
        var menu = await _menuRepository.GetByIdAsync(menuId);
        return menu?.Adapt<MenuDto>();
    }

    /// <summary>
    /// 根据角色ID获取菜单
    /// </summary>
    /// <param name="roleId"></param>
    /// <param name="tenantId"></param>
    /// <returns></returns>
    public async Task<IReadOnlyList<MenuDto>> GetRoleMenusAsync(long roleId, long? tenantId = null)
    {
        var menus = await _menuRepository.GetRoleMenusAsync(roleId, tenantId);
        return menus.Select(static menu => menu.Adapt<MenuDto>()).ToArray();
    }

    /// <summary>
    /// 根据用户ID获取菜单
    /// </summary>
    /// <param name="query"></param>
    /// <returns></returns>
    public async Task<IReadOnlyList<MenuDto>> GetUserMenusAsync(UserMenuQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);
        var menus = await _menuRepository.GetUserMenusAsync(query.UserId, query.TenantId);
        return menus.Select(static menu => menu.Adapt<MenuDto>()).ToArray();
    }
}
