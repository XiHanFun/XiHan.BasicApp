using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Core.DependencyInjection.ServiceLifetimes;
using XiHan.Framework.Core.Exceptions;

namespace XiHan.BasicApp.Saas.Application.InternalServices.Implementations;

/// <summary>
/// 超级管理员保护规则实现。
/// </summary>
public class SuperAdminGuard : ISuperAdminGuard, IScopedDependency
{
    private const string SuperAdminRoleCode = "super_admin";
    private const string SuperAdminUserName = "superadmin";

    private readonly IRoleRepository _roleRepository;
    private readonly IUserRepository _userRepository;

    public SuperAdminGuard(IRoleRepository roleRepository, IUserRepository userRepository)
    {
        _roleRepository = roleRepository;
        _userRepository = userRepository;
    }

    public async Task EnsureRoleAssignmentAllowedAsync(
        SysUser user,
        IReadOnlyCollection<long> targetRoleIds,
        IReadOnlyCollection<SysRole> targetRoles,
        long? tenantId)
    {
        ArgumentNullException.ThrowIfNull(user);

        var superAdminRole = targetRoles.FirstOrDefault(role =>
                                 string.Equals(role.RoleCode, SuperAdminRoleCode, StringComparison.OrdinalIgnoreCase))
                             ?? await _roleRepository.GetByRoleCodeAsync(SuperAdminRoleCode, null);
        if (superAdminRole is null)
        {
            return;
        }

        var superAdminRoleId = superAdminRole.BasicId;
        var assignAsSuperAdmin = targetRoleIds.Contains(superAdminRoleId);
        var currentUserRoles = await _userRepository.GetUserRolesAsync(user.BasicId, tenantId);
        var currentAsSuperAdmin = currentUserRoles.Any(mapping =>
            mapping.Status == XiHan.BasicApp.Saas.Domain.Enums.YesOrNo.Yes
            && mapping.RoleId == superAdminRoleId);

        if (assignAsSuperAdmin)
        {
            if (!string.Equals(user.UserName, SuperAdminUserName, StringComparison.OrdinalIgnoreCase))
            {
                throw new BusinessException(message: "系统只允许内置 superadmin 账号持有超级管理员角色");
            }

            var holderUserIds = await _userRepository.GetUserIdsByRoleIdAsync(superAdminRoleId);
            if (holderUserIds.Any(userId => userId != user.BasicId))
            {
                throw new BusinessException(message: "系统仅允许一个超级管理员账号");
            }

            return;
        }

        if (currentAsSuperAdmin || string.Equals(user.UserName, SuperAdminUserName, StringComparison.OrdinalIgnoreCase))
        {
            throw new BusinessException(message: "超级管理员账号必须保留超级管理员角色");
        }
    }

    public async Task EnsureAccountMutableAsync(SysUser user, long? tenantId, string operationName)
    {
        ArgumentNullException.ThrowIfNull(user);

        if (!await IsSuperAdminAccountAsync(user, tenantId))
        {
            return;
        }

        throw new BusinessException(message: $"超级管理员账号不允许{operationName}");
    }

    private async Task<bool> IsSuperAdminAccountAsync(SysUser user, long? tenantId)
    {
        if (string.Equals(user.UserName, SuperAdminUserName, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var superAdminRole = await _roleRepository.GetByRoleCodeAsync(SuperAdminRoleCode, null);
        if (superAdminRole is null)
        {
            return false;
        }

        var userRoles = await _userRepository.GetUserRolesAsync(user.BasicId, tenantId);
        return userRoles.Any(mapping =>
            mapping.Status == XiHan.BasicApp.Saas.Domain.Enums.YesOrNo.Yes
            && mapping.RoleId == superAdminRole.BasicId);
    }
}
