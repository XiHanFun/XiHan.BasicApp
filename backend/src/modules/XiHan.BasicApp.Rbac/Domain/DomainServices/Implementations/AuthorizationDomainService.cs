using XiHan.BasicApp.Rbac.Domain.Repositories;

namespace XiHan.BasicApp.Rbac.Domain.DomainServices.Implementations;

/// <summary>
/// 授权领域服务实现
/// </summary>
public class AuthorizationDomainService : IAuthorizationDomainService
{
    private readonly IPermissionRepository _permissionRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="permissionRepository">权限仓储</param>
    public AuthorizationDomainService(IPermissionRepository permissionRepository)
    {
        _permissionRepository = permissionRepository;
    }

    /// <summary>
    /// 获取用户权限代码
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="tenantId">租户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户权限代码集合</returns>
    public async Task<IReadOnlyCollection<string>> GetUserPermissionCodesAsync(long userId, long? tenantId = null, CancellationToken cancellationToken = default)
    {
        var permissions = await _permissionRepository.GetUserPermissionsAsync(userId, tenantId, cancellationToken);
        return permissions
            .Where(permission => !string.IsNullOrWhiteSpace(permission.PermissionCode))
            .Select(permission => permission.PermissionCode)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    /// <summary>
    /// 判断用户是否有权限
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="permissionCode">权限代码</param>
    /// <param name="tenantId">租户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否有权限</returns>
    public async Task<bool> HasPermissionAsync(long userId, string permissionCode, long? tenantId = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(permissionCode);
        var codes = await GetUserPermissionCodesAsync(userId, tenantId, cancellationToken);
        return codes.Contains(permissionCode, StringComparer.OrdinalIgnoreCase);
    }
}
