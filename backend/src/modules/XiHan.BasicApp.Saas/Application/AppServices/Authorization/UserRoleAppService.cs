// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.AspNetCore.Authorization;
using XiHan.BasicApp.Saas.Application.Authorization;
using XiHan.BasicApp.Saas.Application.Caching;
using XiHan.BasicApp.Saas.Application.Contracts;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.Mappers;
using XiHan.BasicApp.Saas.Application.Services;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Permissions;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Authorization.AspNetCore;
using XiHan.Framework.Uow.Attributes;

namespace XiHan.BasicApp.Saas.Application.AppServices;

/// <summary>
/// 用户角色命令应用服务
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "用户角色")]
public sealed class UserRoleAppService
    : SaasApplicationService, IUserRoleAppService
{
    private readonly IUserDomainService _userDomainService;

    private readonly ISaasCacheInvalidator _cacheInvalidator;

    private readonly IAuthorizationChangeNotifier _authorizationChangeNotifier;

    private readonly IImpersonationPolicyService _impersonationPolicyService;

    private readonly ISuperAdminProtector _superAdminProtector;

    private readonly IUserRoleRepository _userRoleRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public UserRoleAppService(
        IUserDomainService userDomainService,
        ISaasCacheInvalidator cacheInvalidator,
        IAuthorizationChangeNotifier authorizationChangeNotifier,
        IImpersonationPolicyService impersonationPolicyService,
        ISuperAdminProtector superAdminProtector,
        IUserRoleRepository userRoleRepository)
    {
        _userDomainService = userDomainService;
        _cacheInvalidator = cacheInvalidator;
        _authorizationChangeNotifier = authorizationChangeNotifier;
        _impersonationPolicyService = impersonationPolicyService;
        _superAdminProtector = superAdminProtector;
        _userRoleRepository = userRoleRepository;
    }

    #region 用户角色

    /// <summary>
    /// 批量变更用户角色（一次性提交授予与撤销，单事务，仅在最后失效一次缓存）
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.UserRole.Grant)]
    [PermissionAuthorize(SaasPermissionCodes.UserRole.Revoke)]
    public async Task BatchUpdateUserRolesAsync(UserRoleBatchUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        // 超管保护：非超管不得改超管用户的角色，也不得授予或撤销 super_admin 角色——
        // 撤销项按记录主键解析出角色，与授予项一起逐个过同一道闸
        await _superAdminProtector.EnsureCanWriteUserAsync(input.UserId, cancellationToken);
        var revokeIds = input.RevokeUserRoleIds.Where(id => id > 0).Distinct().ToList();
        var revokingRoleIds = revokeIds.Count == 0
            ? []
            : (await _userRoleRepository.GetListAsync(
                userRole => revokeIds.Contains(userRole.BasicId) && userRole.UserId == input.UserId,
                cancellationToken)).Select(userRole => userRole.RoleId).ToList();
        foreach (var roleId in input.GrantRoleIds.Concat(revokingRoleIds).Distinct())
        {
            await _superAdminProtector.EnsureCanAssignRoleAsync(roleId, cancellationToken);
        }

        // 角色是与直授等价的授权通道：角色里含模仿权限时，走与直授同一道准入
        await _impersonationPolicyService.EnsureCanGrantRoleIdsAsync([.. input.GrantRoleIds], cancellationToken);

        var result = await _userDomainService.BatchUpdateUserRolesAsync(
            new UserRoleBatchUpdateCommand(input.UserId, input.GrantRoleIds, input.RevokeUserRoleIds),
            cancellationToken);
        await _cacheInvalidator.InvalidateAuthorizationAsync(cancellationToken: cancellationToken);

        // 逐条记录本次实际发生的角色变更（审计）
        foreach (var (changeType, roleIds) in new[]
        {
            (PermissionChangeType.UserRemoveRole, result.RevokedRoleIds),
            (PermissionChangeType.UserAssignRole, result.GrantedRoleIds)
        })
        {
            foreach (var roleId in roleIds)
            {
                await _authorizationChangeNotifier.NotifyAsync(
                    changeType,
                    targetUserId: input.UserId,
                    targetRoleId: roleId,
                    permissionId: null,
                    cancellationToken: cancellationToken);
            }
        }
    }

    /// <summary>
    /// 批量变更角色成员（以角色为中心，一次性提交加入与移出，单事务，仅在最后失效一次缓存）
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.UserRole.Grant)]
    [PermissionAuthorize(SaasPermissionCodes.UserRole.Revoke)]
    public async Task BatchUpdateRoleMembersAsync(RoleMemberBatchUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        // 超管保护：非超管不得授予或撤销 super_admin 角色，也不得改超管用户的角色——
        // 移出项按记录主键解析出成员，与加入项一起逐个过同一道闸
        await _superAdminProtector.EnsureCanAssignRoleAsync(input.RoleId, cancellationToken);
        var revokeIds = input.RevokeUserRoleIds.Where(id => id > 0).Distinct().ToList();
        var revokingUserIds = revokeIds.Count == 0
            ? []
            : (await _userRoleRepository.GetListAsync(
                userRole => revokeIds.Contains(userRole.BasicId) && userRole.RoleId == input.RoleId,
                cancellationToken)).Select(userRole => userRole.UserId).ToList();
        foreach (var userId in input.GrantUserIds.Concat(revokingUserIds).Distinct())
        {
            await _superAdminProtector.EnsureCanWriteUserAsync(userId, cancellationToken);
        }

        // 角色是与直授等价的授权通道：角色里含模仿权限时，加人走与直授同一道准入
        if (input.GrantUserIds.Count > 0)
        {
            await _impersonationPolicyService.EnsureCanGrantRoleIdsAsync([input.RoleId], cancellationToken);
        }

        var result = await _userDomainService.BatchUpdateRoleMembersAsync(
            new RoleMemberBatchUpdateCommand(input.RoleId, input.GrantUserIds, input.RevokeUserRoleIds),
            cancellationToken);
        await _cacheInvalidator.InvalidateAuthorizationAsync(cancellationToken: cancellationToken);

        // 逐条记录本次实际发生的角色变更（审计）
        foreach (var (changeType, userIds) in new[]
        {
            (PermissionChangeType.UserRemoveRole, result.RevokedUserIds),
            (PermissionChangeType.UserAssignRole, result.GrantedUserIds)
        })
        {
            foreach (var userId in userIds)
            {
                await _authorizationChangeNotifier.NotifyAsync(
                    changeType,
                    targetUserId: userId,
                    targetRoleId: input.RoleId,
                    permissionId: null,
                    cancellationToken: cancellationToken);
            }
        }
    }

    /// <summary>
    /// 更新用户角色
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.UserRole.Update)]
    public async Task<UserRoleDetailDto> UpdateUserRoleAsync(UserRoleUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        // 超管保护：解析该用户角色记录的 UserId/RoleId，非超管不得修改超管用户的角色或 super_admin 角色绑定
        await EnsureCanWriteUserRoleAsync(input.BasicId, cancellationToken);

        var result = await _userDomainService.UpdateUserRoleAsync(UserRoleApplicationMapper.ToUpdateCommand(input), cancellationToken);
        await _cacheInvalidator.InvalidateAuthorizationAsync(cancellationToken: cancellationToken);
        // 更新后按有效状态留痕：有效=分配角色，失效=移除角色
        await _authorizationChangeNotifier.NotifyAsync(
            result.UserRole.Status != ValidityStatus.Valid
                ? PermissionChangeType.UserRemoveRole
                : PermissionChangeType.UserAssignRole,
            targetUserId: result.UserRole.UserId,
            targetRoleId: result.UserRole.RoleId,
            permissionId: null,
            cancellationToken: cancellationToken);
        return UserRoleApplicationMapper.ToDetailDto(result.UserRole, result.Role, result.TenantMember, result.Now);
    }

    /// <summary>
    /// 更新用户角色状态
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.UserRole.Status)]
    public async Task<UserRoleDetailDto> UpdateUserRoleStatusAsync(UserRoleStatusUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        // 超管保护：解析该用户角色记录的 UserId/RoleId，非超管不得启停超管用户的角色或 super_admin 角色绑定
        await EnsureCanWriteUserRoleAsync(input.BasicId, cancellationToken);

        var result = await _userDomainService.UpdateUserRoleStatusAsync(UserRoleApplicationMapper.ToStatusCommand(input), cancellationToken);
        await _cacheInvalidator.InvalidateAuthorizationAsync(cancellationToken: cancellationToken);
        // 状态切换即分配/移除角色：Valid→分配角色，Invalid→移除角色
        await _authorizationChangeNotifier.NotifyAsync(
            result.UserRole.Status != ValidityStatus.Valid
                ? PermissionChangeType.UserRemoveRole
                : PermissionChangeType.UserAssignRole,
            targetUserId: result.UserRole.UserId,
            targetRoleId: result.UserRole.RoleId,
            permissionId: null,
            cancellationToken: cancellationToken);
        return UserRoleApplicationMapper.ToDetailDto(result.UserRole, result.Role, result.TenantMember, result.Now);
    }

    /// <summary>
    /// 超管保护：按用户角色记录 id 解析其 UserId/RoleId，校验当前用户可写该用户、可授予/撤销该角色。
    /// </summary>
    private async Task EnsureCanWriteUserRoleAsync(long userRoleId, CancellationToken cancellationToken)
    {
        var userRole = await _userRoleRepository.GetByIdAsync(userRoleId, cancellationToken);
        if (userRole is null)
        {
            return;
        }

        await _superAdminProtector.EnsureCanWriteUserAsync(userRole.UserId, cancellationToken);
        await _superAdminProtector.EnsureCanAssignRoleAsync(userRole.RoleId, cancellationToken);
        await _impersonationPolicyService.EnsureCanGrantRoleIdsAsync([userRole.RoleId], cancellationToken);
    }

    #endregion
}
