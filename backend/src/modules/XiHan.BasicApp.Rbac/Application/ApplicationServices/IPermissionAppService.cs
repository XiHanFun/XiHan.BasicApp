using XiHan.BasicApp.Rbac.Application.Dtos;
using XiHan.BasicApp.Rbac.Application.Queries;
using XiHan.Framework.Application.Contracts.Services;

namespace XiHan.BasicApp.Rbac.Application.ApplicationServices;

/// <summary>
/// 权限应用服务
/// </summary>
public interface IPermissionAppService : IApplicationService
{
    /// <summary>
    /// 根据权限ID获取权限
    /// </summary>
    /// <param name="permissionId"></param>
    /// <returns></returns>
    Task<PermissionDto?> GetByIdAsync(long permissionId);

    /// <summary>
    /// 根据角色ID获取权限
    /// </summary>
    /// <param name="roleId"></param>
    /// <param name="tenantId"></param>
    /// <returns></returns>
    Task<IReadOnlyList<PermissionDto>> GetRolePermissionsAsync(long roleId, long? tenantId = null);

    /// <summary>
    /// 根据用户ID获取权限
    /// </summary>
    /// <param name="query"></param>
    /// <returns></returns>
    Task<IReadOnlyList<PermissionDto>> GetUserPermissionsAsync(UserPermissionQuery query);
}
