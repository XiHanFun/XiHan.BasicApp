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
/// 用户直授权限命令应用服务
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "用户直授权限")]
public sealed class UserPermissionAppService
    : SaasApplicationService, IUserPermissionAppService
{
    private readonly IUserDomainService _userDomainService;

    private readonly ISaasCacheInvalidator _cacheInvalidator;

    private readonly IAuthorizationChangeNotifier _authorizationChangeNotifier;

    private readonly IImpersonationPolicyService _impersonationPolicyService;

    private readonly IUserPermissionRepository _userPermissionRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public UserPermissionAppService(
        IUserDomainService userDomainService,
        ISaasCacheInvalidator cacheInvalidator,
        IAuthorizationChangeNotifier authorizationChangeNotifier,
        IImpersonationPolicyService impersonationPolicyService,
        IUserPermissionRepository userPermissionRepository)
    {
        _userDomainService = userDomainService;
        _cacheInvalidator = cacheInvalidator;
        _authorizationChangeNotifier = authorizationChangeNotifier;
        _impersonationPolicyService = impersonationPolicyService;
        _userPermissionRepository = userPermissionRepository;
    }

    #region 用户直授权限

    /// <summary>
    /// 授予用户直授权限
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.UserPermission.Grant)]
    public async Task<UserPermissionDetailDto> CreateUserPermissionAsync(UserPermissionGrantDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        await _impersonationPolicyService.EnsureCanGrantPermissionIdsAsync([input.PermissionId], cancellationToken);
        var result = await _userDomainService.CreateUserPermissionAsync(UserPermissionApplicationMapper.ToGrantCommand(input), cancellationToken);
        await _cacheInvalidator.InvalidateAuthorizationAsync(cancellationToken: cancellationToken);
        await _authorizationChangeNotifier.NotifyAsync(
            result.UserPermission.PermissionAction == PermissionAction.Deny ? PermissionChangeType.UserDenyPermission : PermissionChangeType.UserGrantPermission,
            targetUserId: result.UserPermission.UserId,
            targetRoleId: null,
            permissionId: result.UserPermission.PermissionId,
            reason: result.UserPermission.GrantReason,
            cancellationToken: cancellationToken);
        return UserPermissionApplicationMapper.ToDetailDto(result.UserPermission, result.Permission, result.TenantMember, result.Now);
    }

    /// <summary>
    /// 批量变更用户直授权限（一次性提交授予与撤销，单事务，仅在最后失效一次缓存）
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.UserPermission.Grant)]
    [PermissionAuthorize(SaasPermissionCodes.UserPermission.Revoke)]
    public async Task BatchUpdateUserPermissionsAsync(UserPermissionBatchUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        await _impersonationPolicyService.EnsureCanGrantPermissionIdsAsync(
            [.. input.Grants.Select(static grant => grant.PermissionId)],
            cancellationToken);
        var result = await _userDomainService.BatchUpdateUserPermissionsAsync(
            new UserPermissionBatchUpdateCommand(
                input.UserId,
                [.. input.Grants.Select(grant => new UserPermissionBatchGrantItem(grant.PermissionId, grant.PermissionAction))],
                input.RevokeUserPermissionIds),
            cancellationToken);
        await _cacheInvalidator.InvalidateAuthorizationAsync(cancellationToken: cancellationToken);

        // 逐条记录本次实际发生的直授变更（审计）
        foreach (var (changeType, permissionIds) in new[]
        {
            (PermissionChangeType.UserRevokePermission, result.RevokedPermissionIds),
            (PermissionChangeType.UserDenyPermission, result.DeniedPermissionIds),
            (PermissionChangeType.UserGrantPermission, result.GrantedPermissionIds)
        })
        {
            foreach (var permissionId in permissionIds)
            {
                await _authorizationChangeNotifier.NotifyAsync(
                    changeType,
                    targetUserId: input.UserId,
                    targetRoleId: null,
                    permissionId: permissionId,
                    cancellationToken: cancellationToken);
            }
        }
    }

    /// <summary>
    /// 撤销用户直授权限
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.UserPermission.Revoke)]
    public async Task DeleteUserPermissionAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var userPermission = await _userPermissionRepository.GetByIdAsync(id, cancellationToken);
        await _userDomainService.DeleteUserPermissionAsync(id, cancellationToken);
        await _cacheInvalidator.InvalidateAuthorizationAsync(cancellationToken: cancellationToken);
        if (userPermission is not null)
        {
            await _authorizationChangeNotifier.NotifyAsync(
                PermissionChangeType.UserRevokePermission,
                targetUserId: userPermission.UserId,
                targetRoleId: null,
                permissionId: userPermission.PermissionId,
                cancellationToken: cancellationToken);
        }
    }

    /// <summary>
    /// 更新用户直授权限
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.UserPermission.Update)]
    public async Task<UserPermissionDetailDto> UpdateUserPermissionAsync(UserPermissionUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _userDomainService.UpdateUserPermissionAsync(UserPermissionApplicationMapper.ToUpdateCommand(input), cancellationToken);
        await _cacheInvalidator.InvalidateAuthorizationAsync(cancellationToken: cancellationToken);
        // 更新可能翻转授予↔拒绝：按更新后有效状态与动作留痕
        await _authorizationChangeNotifier.NotifyAsync(
            result.UserPermission.Status != ValidityStatus.Valid
                ? PermissionChangeType.UserRevokePermission
                : result.UserPermission.PermissionAction == PermissionAction.Deny
                    ? PermissionChangeType.UserDenyPermission
                    : PermissionChangeType.UserGrantPermission,
            targetUserId: result.UserPermission.UserId,
            targetRoleId: null,
            permissionId: result.UserPermission.PermissionId,
            cancellationToken: cancellationToken);
        return UserPermissionApplicationMapper.ToDetailDto(result.UserPermission, result.Permission, result.TenantMember, result.Now);
    }

    /// <summary>
    /// 更新用户直授权限状态
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.UserPermission.Status)]
    public async Task<UserPermissionDetailDto> UpdateUserPermissionStatusAsync(UserPermissionStatusUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _userDomainService.UpdateUserPermissionStatusAsync(UserPermissionApplicationMapper.ToStatusCommand(input), cancellationToken);
        await _cacheInvalidator.InvalidateAuthorizationAsync(cancellationToken: cancellationToken);
        // 状态切换即授予/收回：Valid→按动作授予/拒绝，Invalid→撤销
        await _authorizationChangeNotifier.NotifyAsync(
            result.UserPermission.Status != ValidityStatus.Valid
                ? PermissionChangeType.UserRevokePermission
                : result.UserPermission.PermissionAction == PermissionAction.Deny
                    ? PermissionChangeType.UserDenyPermission
                    : PermissionChangeType.UserGrantPermission,
            targetUserId: result.UserPermission.UserId,
            targetRoleId: null,
            permissionId: result.UserPermission.PermissionId,
            cancellationToken: cancellationToken);
        return UserPermissionApplicationMapper.ToDetailDto(result.UserPermission, result.Permission, result.TenantMember, result.Now);
    }

    #endregion
}
