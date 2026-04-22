using XiHan.BasicApp.Saas.Application.Caching;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.Helpers;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Core.DependencyInjection.ServiceLifetimes;

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
    private readonly ITenantAccessContextService _tenantAccessContextService;

    public AuthorizationContextService(
        IUserRepository userRepository,
        IRoleResolutionDomainService roleResolutionDomainService,
        IPermissionDomainService permissionDomainService,
        IRbacAuthorizationCacheService authorizationCacheService,
        IMenuRepository menuRepository,
        ITenantAccessContextService tenantAccessContextService)
    {
        _userRepository = userRepository;
        _roleResolutionDomainService = roleResolutionDomainService;
        _permissionDomainService = permissionDomainService;
        _authorizationCacheService = authorizationCacheService;
        _menuRepository = menuRepository;
        _tenantAccessContextService = tenantAccessContextService;
    }

    public Task<CurrentUserSessionContext?> GetCurrentContextAsync(CancellationToken cancellationToken = default)
    {
        return _tenantAccessContextService.GetCurrentContextAsync(cancellationToken);
    }

    public async Task<SysUser?> GetCurrentUserAsync(CancellationToken cancellationToken = default)
    {
        var context = await GetCurrentContextAsync(cancellationToken);
        return context?.User;
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
        var context = await GetCurrentContextAsync(cancellationToken);
        var effectiveTenantId = context?.CurrentTenantId ?? (user.TenantId > 0 ? user.TenantId : null);

        var roleCodes = await GetUserRoleCodesAsync(user.BasicId, effectiveTenantId, cancellationToken);
        var permissionCodes = await GetUserPermissionCodesAsync(user.BasicId, effectiveTenantId, cancellationToken);
        var userMenus = await _menuRepository.GetUserMenusAsync(user.BasicId, effectiveTenantId, cancellationToken);

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
