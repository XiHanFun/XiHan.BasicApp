// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Application.QueryServices;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Permissions;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Authorization.Permissions;
using XiHan.Framework.Core.Exceptions;
using XiHan.Framework.MultiTenancy.Abstractions;
using XiHan.Framework.Security.Users;

namespace XiHan.BasicApp.Saas.Application.Services;

/// <summary>
/// 模仿登录准入判定服务实现。
/// </summary>
/// <remarks>
/// 全部判定 fail-closed：任一条件不满足即抛 <see cref="UserFriendlyException"/>，不做静默降级。
/// 超管判定读实时授权快照而非令牌里的角色声明，角色被撤销后即时生效。
/// </remarks>
public sealed class ImpersonationPolicyService : IImpersonationPolicyService
{
    /// <summary>
    /// 超级管理员角色编码（与种子/授权快照约定一致）。
    /// </summary>
    private const string SuperAdminRoleCode = "super_admin";

    private readonly IAuthContextQueryService _authContextQueryService;

    private readonly IAuthorizationSnapshotQueryService _authorizationSnapshotQueryService;

    private readonly ICurrentTenant _currentTenant;

    private readonly ICurrentUser _currentUser;

    private readonly IPermissionChecker _permissionChecker;

    private readonly ISuperAdminProtector _superAdminProtector;

    private readonly IPermissionRepository _permissionRepository;

    private readonly ITenantUserRepository _tenantUserRepository;

    private readonly IUserRepository _userRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public ImpersonationPolicyService(
        IAuthContextQueryService authContextQueryService,
        IAuthorizationSnapshotQueryService authorizationSnapshotQueryService,
        ICurrentTenant currentTenant,
        ICurrentUser currentUser,
        IPermissionChecker permissionChecker,
        IPermissionRepository permissionRepository,
        ISuperAdminProtector superAdminProtector,
        ITenantUserRepository tenantUserRepository,
        IUserRepository userRepository)
    {
        _authContextQueryService = authContextQueryService;
        _authorizationSnapshotQueryService = authorizationSnapshotQueryService;
        _currentTenant = currentTenant;
        _currentUser = currentUser;
        _permissionChecker = permissionChecker;
        _permissionRepository = permissionRepository;
        _superAdminProtector = superAdminProtector;
        _tenantUserRepository = tenantUserRepository;
        _userRepository = userRepository;
    }

    /// <summary>
    /// 判定发起人能否模仿目标用户，通过则返回准入方案，否则抛出禁止异常。
    /// </summary>
    /// <param name="operatorUserId">发起人用户标识</param>
    /// <param name="operatorTenantId">发起人当前所处租户；空表示平台运维态</param>
    /// <param name="operatorIsImpersonating">发起人当前是否已处于模仿态</param>
    /// <param name="targetUserId">目标用户标识</param>
    /// <param name="requestedTenantId">请求指定的目标租户；空表示由服务解析</param>
    /// <param name="now">当前时间</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>准入方案</returns>
    public async Task<ImpersonationPlan> AuthorizeStartAsync(
        long operatorUserId,
        long? operatorTenantId,
        bool operatorIsImpersonating,
        long targetUserId,
        long? requestedTenantId,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(operatorUserId, 0);
        cancellationToken.ThrowIfCancellationRequested();

        if (operatorIsImpersonating)
        {
            throw new UserFriendlyException("当前已处于模仿状态，不能再次发起模仿。");
        }

        if (targetUserId <= 0)
        {
            throw new UserFriendlyException("目标用户不存在。");
        }

        if (targetUserId == operatorUserId)
        {
            throw new UserFriendlyException("不能模仿自己。");
        }

        var target = await _userRepository.GetByIdIgnoreTenantAsync(targetUserId, cancellationToken)
            ?? throw new UserFriendlyException("目标用户不存在。");

        if (target.Status != EnableStatus.Enabled)
        {
            throw new UserFriendlyException("目标用户已被禁用，无法模仿。");
        }

        // 「是不是超管」必须是全局事实：租户上下文下的读过滤器会挡掉租户戳不同的授权行
        using (_currentTenant.Change(null))
        {
            if (await _superAdminProtector.IsProtectedUserAsync(target.BasicId, cancellationToken))
            {
                throw new UserFriendlyException("不能模仿超级管理员。");
            }
        }

        var operatorSnapshot = await _authorizationSnapshotQueryService.BuildAsync(operatorUserId, now, cancellationToken);
        var operatorIsSuperAdmin = operatorSnapshot.Roles.Contains(SuperAdminRoleCode, StringComparer.OrdinalIgnoreCase);

        var targetTenantId = await ResolveTargetTenantIdAsync(
            operatorTenantId,
            target,
            requestedTenantId,
            now,
            cancellationToken);

        var isCrossTenant = targetTenantId != operatorTenantId;
        if (isCrossTenant &&
            !await _permissionChecker.IsGrantedAsync(
                operatorUserId.ToString(),
                SaasPermissionCodes.Impersonation.CrossTenant,
                cancellationToken))
        {
            throw new UserFriendlyException("当前账号无权跨租户模仿。");
        }

        string? targetTenantName = null;
        if (targetTenantId.HasValue)
        {
            var tenant = await _authContextQueryService.GetLoginTenantOrThrowAsync(targetTenantId, now, cancellationToken)
                ?? throw new UserFriendlyException("目标租户不存在或不可用。");
            targetTenantName = tenant.TenantName;

            var targetMembership = await _tenantUserRepository.GetMembershipAsync(targetTenantId.Value, target.BasicId, cancellationToken);
            EnsureMembershipUsable(targetMembership, now, "目标用户不是该租户的有效成员。");

            if (!operatorIsSuperAdmin)
            {
                var operatorMembership = await _tenantUserRepository.GetMembershipAsync(targetTenantId.Value, operatorUserId, cancellationToken);
                EnsureMembershipUsable(operatorMembership, now, "当前账号不是该租户的有效成员。");

                if (!ImpersonationDefaults.AdministrativeMemberTypes.Contains(operatorMembership!.MemberType))
                {
                    throw new UserFriendlyException("只有租户所有者或管理员可以发起模仿。");
                }

                if (ImpersonationDefaults.AdministrativeMemberTypes.Contains(targetMembership!.MemberType))
                {
                    throw new UserFriendlyException("不能模仿同级或更高权限的成员。");
                }
            }
        }
        else if (!operatorIsSuperAdmin)
        {
            // 平台运维态下没有成员关系可作判据，非超管一律拒绝
            throw new UserFriendlyException("只有超级管理员可以在平台运维态发起模仿。");
        }

        return new ImpersonationPlan(target, targetTenantId, targetTenantName, isCrossTenant, operatorIsSuperAdmin);
    }

    /// <summary>
    /// 判定当前用户能否授出指定权限，不能则抛出禁止异常。
    /// </summary>
    /// <param name="permissionIds">被授出的权限主键集合</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task EnsureCanGrantPermissionIdsAsync(IReadOnlyCollection<long> permissionIds, CancellationToken cancellationToken = default)
    {
        if (permissionIds is not { Count: > 0 })
        {
            return;
        }

        cancellationToken.ThrowIfCancellationRequested();

        if (_superAdminProtector.IsCurrentUserSuperAdmin())
        {
            return;
        }

        var ids = permissionIds.Where(static id => id > 0).Distinct().ToList();
        if (ids.Count == 0)
        {
            return;
        }

        var permissions = await _permissionRepository.GetListAsync(
            permission => ids.Contains(permission.BasicId),
            cancellationToken);
        var codes = permissions
            .Select(static permission => permission.PermissionCode)
            .Where(static code => !string.IsNullOrWhiteSpace(code))
            .Select(static code => code.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (codes.Count == 0)
        {
            return;
        }

        var platformOnly = codes.Where(SaasPlatformPermissions.PlatformOnlyCodes.Contains).ToList();
        if (platformOnly.Count > 0)
        {
            throw new UserFriendlyException($"无权授予平台专属权限：{string.Join("、", platformOnly)}。");
        }

        var impersonationCodes = codes
            .Where(static code => code.StartsWith(
                $"{SaasPermissionCodes.Module}:{SaasPermissionCodes.Impersonation.Group}:",
                StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (impersonationCodes.Count == 0)
        {
            return;
        }

        var operatorUserId = _currentUser.UserId
            ?? throw new UserFriendlyException("当前用户未登录。");
        var tenantId = _currentTenant.Id
            ?? throw new UserFriendlyException("只有超级管理员可以在平台运维态授予模仿登录权限。");

        var membership = await _tenantUserRepository.GetMembershipAsync(tenantId, operatorUserId, cancellationToken);
        if (membership is null || !ImpersonationDefaults.AdministrativeMemberTypes.Contains(membership.MemberType))
        {
            throw new UserFriendlyException("只有超级管理员或租户管理员可以授予模仿登录权限。");
        }
    }

    /// <summary>
    /// 解析模仿会话所处租户：显式指定优先，其次沿用发起人当前上下文，最后回落到目标用户自己的登录落点。
    /// </summary>
    private async Task<long?> ResolveTargetTenantIdAsync(
        long? operatorTenantId,
        SysUser target,
        long? requestedTenantId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (requestedTenantId is > 0)
        {
            return requestedTenantId;
        }

        if (operatorTenantId is > 0)
        {
            return operatorTenantId;
        }

        // 平台运维态发起：按目标用户自己登录时的落点解析，使模仿态与其真实登录体验一致。
        // 解析不出唯一落点时 fail-closed，不能兜底成平台态——那会跳过整条成员有效性校验
        if (target.TenantId == 0)
        {
            return null;
        }

        var memberships = await _tenantUserRepository.GetActiveByUserIdAsync(target.BasicId, now, cancellationToken);
        return memberships.Count switch
        {
            1 => memberships[0].TenantId,
            0 => throw new UserFriendlyException("目标用户不是任何租户的有效成员，无法模仿。"),
            _ => throw new UserFriendlyException("目标用户归属多个租户，请指定要进入的租户。")
        };
    }

    /// <summary>
    /// 校验成员关系可用：已接受邀请、状态有效且落在生效期内。
    /// </summary>
    private static void EnsureMembershipUsable(SysTenantUser? membership, DateTimeOffset now, string message)
    {
        if (membership is null ||
            membership.InviteStatus != TenantMemberInviteStatus.Accepted ||
            membership.Status != ValidityStatus.Valid ||
            (membership.EffectiveTime.HasValue && membership.EffectiveTime.Value > now) ||
            (membership.ExpirationTime.HasValue && membership.ExpirationTime.Value <= now))
        {
            throw new UserFriendlyException(message);
        }
    }
}
