using XiHan.BasicApp.Rbac.Application.Dtos;
using XiHan.BasicApp.Rbac.Application.Queries;
using XiHan.Framework.Application.Contracts.Services;

namespace XiHan.BasicApp.Rbac.Application.ApplicationServices;

/// <summary>
/// 菜单应用服务
/// </summary>
public interface IMenuAppService : IApplicationService
{
    /// <summary>
    /// 根据菜单ID获取菜单
    /// </summary>
    /// <param name="menuId"></param>
    /// <returns></returns>
    Task<MenuDto?> GetByIdAsync(long menuId);

    /// <summary>
    /// 根据角色ID获取菜单
    /// </summary>
    /// <param name="roleId"></param>
    /// <param name="tenantId"></param>
    /// <returns></returns>
    Task<IReadOnlyList<MenuDto>> GetRoleMenusAsync(long roleId, long? tenantId = null);

    /// <summary>
    /// 根据用户ID获取菜单
    /// </summary>
    /// <param name="query"></param>
    /// <returns></returns>
    Task<IReadOnlyList<MenuDto>> GetUserMenusAsync(UserMenuQuery query);
}
