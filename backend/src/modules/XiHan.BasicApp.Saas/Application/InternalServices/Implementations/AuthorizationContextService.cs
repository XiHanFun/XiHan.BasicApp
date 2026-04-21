using XiHan.BasicApp.Saas.Application.Caching;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.Helpers;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Core.DependencyInjection.ServiceLifetimes;
using XiHan.Framework.Security.Users;

namespace XiHan.BasicApp.Saas.Application.InternalServices.Implementations;

/// <summary>
/// 授权上下文内部服务实现。
/// </summary>
public class AuthorizationContextService : IAuthorizationContextService, IScopedDependency
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleResolutionDomainService _roleResolutionDomainService;
    private readonly IPermissionDomainService _permissionDomainService;
    private readonly IRbacAuthorizationCacheService _authorizationCacheService;
    private readonly IMenuRepository _menuRepository;
    private readonly ICurrentUser _currentUser;

    public AuthorizationContextService(
        IUserRepository userRepository,
        IRoleResolutionDomainService roleResolutionDomainService,
        IPermissionDomainService permissionDomainService,
        IRbacAuthorizationCacheService authorizationCacheService,
        IMenuRepository menuRepository,
        ICurrentUser currentUser)
    {
        _userRepository = userRepository;
        _roleResolutionDomainService = roleResolutionDomainService;
        _permissionDomainService = permissionDomainService;
        _authorizationCacheService = authorizationCacheService;
        _menuRepository = menuRepository;
        _currentUser = currentUser;
    }

    public async Task<SysUser?> GetCurrentUserAsync(CancellationToken cancellationToken = default)
    {
        if (!_currentUser.UserId.HasValue)
        {
            return null;
        }

        var user = await _userRepository.GetByIdAsync(_currentUser.UserId.Value, cancellationToken);
        return user is not null && user.Status == YesOrNo.Yes ? user : null;
    }

    public async Task<IReadOnlyCollection<string>> GetUserRoleCodesAsync(long userId, long? tenantId, CancellationToken cancellationToken = default)
    {
        var roleCodes = await _roleResolutionDomainService.GetUserRoleCodesAsync(userId, tenantId, cancellationToken);
        return roleCodes
            .Where(static code => !string.IsNullOrWhiteSpace(code))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static code => code, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public async Task<IReadOnlyCollection<string>> GetUserPermissionCodesAsync(long userId, long? tenantId, CancellationToken cancellationToken = default)
    {
        return await _authorizationCacheService.GetUserPermissionCodesAsync(
            userId,
            tenantId,
            token => _permissionDomainService.GetUserPermissionCodesAsync(userId, tenantId, token),
            cancellationToken);
    }

    public async Task<IReadOnlyCollection<long>> GetUserDataScopeDepartmentIdsAsync(long userId, long? tenantId, CancellationToken cancellationToken = default)
    {
        return await _authorizationCacheService.GetUserDataScopeDepartmentIdsAsync(
            userId,
            tenantId,
            token => _permissionDomainService.GetUserDataScopeDepartmentIdsAsync(userId, tenantId, token),
            cancellationToken);
    }

    public async Task<AuthPermissionDto> BuildPermissionContextAsync(SysUser user, CancellationToken cancellationToken = default)
    {
        var roleCodes = await GetUserRoleCodesAsync(user.BasicId, user.TenantId, cancellationToken);
        var permissionCodes = await GetUserPermissionCodesAsync(user.BasicId, user.TenantId, cancellationToken);
        var userMenus = await _menuRepository.GetUserMenusAsync(user.BasicId, user.TenantId, cancellationToken);

        var permissionCodeSet = permissionCodes
            .Where(static code => !string.IsNullOrWhiteSpace(code))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return new AuthPermissionDto
        {
            Roles = [.. roleCodes],
            Permissions = [.. permissionCodeSet.OrderBy(static code => code)],
            Menus = AuthMenuBuilder.BuildMenuRoutes(userMenus, permissionCodeSet)
        };
    }
}
