using XiHan.BasicApp.Rbac.Application.Commands;
using XiHan.BasicApp.Rbac.Application.Dtos;
using XiHan.BasicApp.Rbac.Application.Queries;
using XiHan.Framework.Application.Contracts.Services;

namespace XiHan.BasicApp.Rbac.Application.ApplicationServices;

/// <summary>
/// 角色应用服务
/// </summary>
public interface IRoleAppService : IApplicationService
{
    /// <summary>
    /// 根据角色ID获取角色
    /// </summary>
    /// <param name="roleId"></param>
    /// <returns></returns>
    Task<RoleDto?> GetByIdAsync(long roleId);

    /// <summary>
    /// 根据角色编码获取角色
    /// </summary>
    /// <param name="query"></param>
    /// <returns></returns>
    Task<RoleDto?> GetByCodeAsync(RoleByCodeQuery query);

    /// <summary>
    /// 创建角色
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    Task<RoleDto> CreateAsync(RoleCreateDto input);

    /// <summary>
    /// 更新角色
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    Task<RoleDto> UpdateAsync(RoleUpdateDto input);

    /// <summary>
    /// 删除角色
    /// </summary>
    /// <param name="roleId"></param>
    /// <returns></returns>
    Task<bool> DeleteAsync(long roleId);

    /// <summary>
    /// 分配权限
    /// </summary>
    /// <param name="command"></param>
    /// <returns></returns>
    Task AssignPermissionsAsync(AssignRolePermissionsCommand command);

    /// <summary>
    /// 分配菜单
    /// </summary>
    /// <param name="command"></param>
    /// <returns></returns>
    Task AssignMenusAsync(AssignRoleMenusCommand command);
}
