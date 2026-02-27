namespace XiHan.BasicApp.Rbac.Domain.DomainServices;

/// <summary>
/// 授权领域服务
/// </summary>
public interface IAuthorizationDomainService
{
    /// <summary>
    /// 获取用户权限编码
    /// </summary>
    Task<IReadOnlyCollection<string>> GetUserPermissionCodesAsync(long userId, long? tenantId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 判断用户是否具备某权限
    /// </summary>
    Task<bool> HasPermissionAsync(long userId, string permissionCode, long? tenantId = null, CancellationToken cancellationToken = default);
}
