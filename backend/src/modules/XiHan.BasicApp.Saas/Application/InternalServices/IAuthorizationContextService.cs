using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.InternalServices;

/// <summary>
/// 授权上下文内部服务。
/// </summary>
public interface IAuthorizationContextService
{
    /// <summary>
    /// 解析当前登录用户会话上下文。
    /// </summary>
    Task<CurrentUserSessionContext?> GetCurrentContextAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 解析当前登录用户实体。
    /// </summary>
    Task<SysUser?> GetCurrentUserAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取用户角色编码。
    /// </summary>
    Task<IReadOnlyCollection<string>> GetUserRoleCodesAsync(long userId, long? tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取用户权限编码。
    /// </summary>
    Task<IReadOnlyCollection<string>> GetUserPermissionCodesAsync(long userId, long? tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取用户数据范围部门 ID。
    /// </summary>
    Task<IReadOnlyCollection<long>> GetUserDataScopeDepartmentIdsAsync(long userId, long? tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 构建用户权限上下文。
    /// </summary>
    Task<AuthPermissionDto> BuildPermissionContextAsync(SysUser user, CancellationToken cancellationToken = default);
}
