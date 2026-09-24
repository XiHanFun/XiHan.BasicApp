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
/// 角色命令应用服务
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "角色")]
public sealed class RoleAppService
    : SaasApplicationService, IRoleAppService
{
    private readonly IRoleDomainService _roleDomainService;

    private readonly ISaasCacheInvalidator _cacheInvalidator;

    private readonly IAuthorizationChangeNotifier _authorizationChangeNotifier;

    private readonly IImpersonationPolicyService _impersonationPolicyService;

    private readonly ISuperAdminProtector _superAdminProtector;

    private readonly IRolePermissionRepository _rolePermissionRepository;

    private readonly IRoleDataScopeRepository _roleDataScopeRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public RoleAppService(
        IRoleDomainService roleDomainService,
        ISaasCacheInvalidator cacheInvalidator,
        IAuthorizationChangeNotifier authorizationChangeNotifier,
        IImpersonationPolicyService impersonationPolicyService,
        ISuperAdminProtector superAdminProtector,
        IRolePermissionRepository rolePermissionRepository,
        IRoleDataScopeRepository roleDataScopeRepository)
    {
        _roleDomainService = roleDomainService;
        _cacheInvalidator = cacheInvalidator;
        _authorizationChangeNotifier = authorizationChangeNotifier;
        _impersonationPolicyService = impersonationPolicyService;
        _superAdminProtector = superAdminProtector;
        _rolePermissionRepository = rolePermissionRepository;
        _roleDataScopeRepository = roleDataScopeRepository;
    }

    /// <summary>
    /// 创建角色
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Role.Create)]
    public async Task<RoleDetailDto> CreateRoleAsync(RoleCreateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _roleDomainService.CreateRoleAsync(RoleApplicationMapper.ToCreateCommand(input), cancellationToken);
        await _cacheInvalidator.InvalidateAuthorizationAsync(cancellationToken: cancellationToken);
        await _cacheInvalidator.InvalidateRoleDefinitionAsync(cancellationToken);
        return RoleApplicationMapper.ToDetailDto(result.Role);
    }

    /// <summary>
    /// 批量变更角色数据范围（一次性提交授予与撤销，单事务，仅在最后失效一次缓存）
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.RoleDataScope.Grant)]
    [PermissionAuthorize(SaasPermissionCodes.RoleDataScope.Revoke)]
    public async Task BatchUpdateRoleDataScopesAsync(RoleDataScopeBatchUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        await _superAdminProtector.EnsureCanWriteRoleAsync(input.RoleId, cancellationToken);
        _ = await _roleDomainService.BatchUpdateRoleDataScopesAsync(
            new RoleDataScopeBatchUpdateCommand(
                input.RoleId,
                [.. input.Grants.Select(grant => new RoleDataScopeBatchGrantItem(grant.DepartmentId, grant.IncludeChildren))],
                input.RevokeRoleDataScopeIds),
            cancellationToken);
        await _cacheInvalidator.InvalidateAuthorizationAsync(cancellationToken: cancellationToken);
    }

    /// <summary>
    /// 批量变更角色的直接父角色（一次性提交新增与移除，单事务，仅在最后失效一次缓存）
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.RoleHierarchy.Create)]
    [PermissionAuthorize(SaasPermissionCodes.RoleHierarchy.Delete)]
    public async Task BatchUpdateRoleParentsAsync(RoleHierarchyBatchUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        // 超管保护：继承方与本次涉及的每个父角色逐个过同一道闸
        await _superAdminProtector.EnsureCanWriteRoleAsync(input.RoleId, cancellationToken);
        foreach (var parentId in input.AddParentRoleIds.Concat(input.RemoveParentRoleIds).Where(id => id > 0).Distinct())
        {
            await _superAdminProtector.EnsureCanWriteRoleAsync(parentId, cancellationToken);
        }

        _ = await _roleDomainService.BatchUpdateRoleParentsAsync(
            new RoleHierarchyBatchUpdateCommand(input.RoleId, input.AddParentRoleIds, input.RemoveParentRoleIds),
            cancellationToken);
        await _cacheInvalidator.InvalidateAuthorizationAsync(cancellationToken: cancellationToken);
    }

    /// <summary>
    /// 批量变更角色权限（一次性提交授予与撤销，单事务，仅在最后失效一次缓存）
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.RolePermission.Grant)]
    [PermissionAuthorize(SaasPermissionCodes.RolePermission.Revoke)]
    public async Task BatchUpdateRolePermissionsAsync(RolePermissionBatchUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        await _superAdminProtector.EnsureCanWriteRoleAsync(input.RoleId, cancellationToken);
        await _impersonationPolicyService.EnsureCanGrantPermissionIdsAsync(input.GrantPermissionIds, cancellationToken);
        var result = await _roleDomainService.BatchUpdateRolePermissionsAsync(
            new RolePermissionBatchUpdateCommand(input.RoleId, input.GrantPermissionIds, input.RevokeRolePermissionIds),
            cancellationToken);
        await _cacheInvalidator.InvalidateAuthorizationAsync(cancellationToken: cancellationToken);

        // 逐条记录本次实际发生的角色权限变更（审计）
        foreach (var permissionId in result.RevokedPermissionIds)
        {
            await _authorizationChangeNotifier.NotifyAsync(
                PermissionChangeType.RoleRevokePermission,
                targetUserId: null,
                targetRoleId: input.RoleId,
                permissionId: permissionId,
                cancellationToken: cancellationToken);
        }

        foreach (var permissionId in result.GrantedPermissionIds)
        {
            await _authorizationChangeNotifier.NotifyAsync(
                PermissionChangeType.RoleGrantPermission,
                targetUserId: null,
                targetRoleId: input.RoleId,
                permissionId: permissionId,
                cancellationToken: cancellationToken);
        }
    }

    /// <summary>
    /// 删除角色
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Role.Delete)]
    public async Task DeleteRoleAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _superAdminProtector.EnsureCanWriteRoleAsync(id, cancellationToken);
        await _roleDomainService.DeleteRoleAsync(id, cancellationToken);
        await _cacheInvalidator.InvalidateAuthorizationAsync(cancellationToken: cancellationToken);
        await _cacheInvalidator.InvalidateRoleDefinitionAsync(cancellationToken);
    }

    /// <summary>
    /// 更新角色
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Role.Update)]
    public async Task<RoleDetailDto> UpdateRoleAsync(RoleUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        await _superAdminProtector.EnsureCanWriteRoleAsync(input.BasicId, cancellationToken);
        var result = await _roleDomainService.UpdateRoleAsync(RoleApplicationMapper.ToUpdateCommand(input), cancellationToken);
        await _cacheInvalidator.InvalidateAuthorizationAsync(cancellationToken: cancellationToken);
        await _cacheInvalidator.InvalidateRoleDefinitionAsync(cancellationToken);
        return RoleApplicationMapper.ToDetailDto(result.Role);
    }

    /// <summary>
    /// 更新角色数据范围
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.RoleDataScope.Update)]
    public async Task<RoleDataScopeDetailDto> UpdateRoleDataScopeAsync(RoleDataScopeUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var dataScope = await _roleDataScopeRepository.GetByIdAsync(input.BasicId, cancellationToken);
        if (dataScope is not null)
        {
            await _superAdminProtector.EnsureCanWriteRoleAsync(dataScope.RoleId, cancellationToken);
        }
        var result = await _roleDomainService.UpdateRoleDataScopeAsync(RoleDataScopeApplicationMapper.ToUpdateCommand(input), cancellationToken);
        await _cacheInvalidator.InvalidateAuthorizationAsync(cancellationToken: cancellationToken);
        return RoleDataScopeApplicationMapper.ToDetailDto(result.DataScope, result.Department);
    }

    /// <summary>
    /// 更新角色数据范围状态
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.RoleDataScope.Status)]
    public async Task<RoleDataScopeDetailDto> UpdateRoleDataScopeStatusAsync(RoleDataScopeStatusUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var dataScope = await _roleDataScopeRepository.GetByIdAsync(input.BasicId, cancellationToken);
        if (dataScope is not null)
        {
            await _superAdminProtector.EnsureCanWriteRoleAsync(dataScope.RoleId, cancellationToken);
        }
        var result = await _roleDomainService.UpdateRoleDataScopeStatusAsync(RoleDataScopeApplicationMapper.ToStatusCommand(input), cancellationToken);
        await _cacheInvalidator.InvalidateAuthorizationAsync(cancellationToken: cancellationToken);
        return RoleDataScopeApplicationMapper.ToDetailDto(result.DataScope, result.Department);
    }

    /// <summary>
    /// 更新角色权限
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.RolePermission.Update)]
    public async Task<RolePermissionDetailDto> UpdateRolePermissionAsync(RolePermissionUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var rolePermission = await _rolePermissionRepository.GetByIdAsync(input.BasicId, cancellationToken);
        if (rolePermission is not null)
        {
            await _superAdminProtector.EnsureCanWriteRoleAsync(rolePermission.RoleId, cancellationToken);
        }
        var result = await _roleDomainService.UpdateRolePermissionAsync(RolePermissionApplicationMapper.ToUpdateCommand(input), cancellationToken);
        await _cacheInvalidator.InvalidateAuthorizationAsync(cancellationToken: cancellationToken);
        // 更新可能翻转授予↔拒绝：按更新后有效状态与动作留痕
        await _authorizationChangeNotifier.NotifyAsync(
            result.RolePermission.Status != ValidityStatus.Valid
                ? PermissionChangeType.RoleRevokePermission
                : result.RolePermission.PermissionAction == PermissionAction.Deny
                    ? PermissionChangeType.RoleDenyPermission
                    : PermissionChangeType.RoleGrantPermission,
            targetUserId: null,
            targetRoleId: result.RolePermission.RoleId,
            permissionId: result.RolePermission.PermissionId,
            cancellationToken: cancellationToken);
        return RolePermissionApplicationMapper.ToDetailDto(result.RolePermission, result.Permission);
    }

    /// <summary>
    /// 更新角色权限状态
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.RolePermission.Status)]
    public async Task<RolePermissionDetailDto> UpdateRolePermissionStatusAsync(RolePermissionStatusUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var rolePermission = await _rolePermissionRepository.GetByIdAsync(input.BasicId, cancellationToken);
        if (rolePermission is not null)
        {
            await _superAdminProtector.EnsureCanWriteRoleAsync(rolePermission.RoleId, cancellationToken);
        }
        var result = await _roleDomainService.UpdateRolePermissionStatusAsync(RolePermissionApplicationMapper.ToStatusCommand(input), cancellationToken);
        await _cacheInvalidator.InvalidateAuthorizationAsync(cancellationToken: cancellationToken);
        // 状态切换即授予/收回：Valid→按动作授予/拒绝，Invalid→撤销
        await _authorizationChangeNotifier.NotifyAsync(
            result.RolePermission.Status != ValidityStatus.Valid
                ? PermissionChangeType.RoleRevokePermission
                : result.RolePermission.PermissionAction == PermissionAction.Deny
                    ? PermissionChangeType.RoleDenyPermission
                    : PermissionChangeType.RoleGrantPermission,
            targetUserId: null,
            targetRoleId: result.RolePermission.RoleId,
            permissionId: result.RolePermission.PermissionId,
            cancellationToken: cancellationToken);
        return RolePermissionApplicationMapper.ToDetailDto(result.RolePermission, result.Permission);
    }

    /// <summary>
    /// 更新角色状态
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Role.Status)]
    public async Task<RoleDetailDto> UpdateRoleStatusAsync(RoleStatusUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        await _superAdminProtector.EnsureCanWriteRoleAsync(input.BasicId, cancellationToken);
        var result = await _roleDomainService.UpdateRoleStatusAsync(RoleApplicationMapper.ToStatusCommand(input), cancellationToken);
        await _cacheInvalidator.InvalidateAuthorizationAsync(cancellationToken: cancellationToken);
        await _cacheInvalidator.InvalidateRoleDefinitionAsync(cancellationToken);
        return RoleApplicationMapper.ToDetailDto(result.Role);
    }
}
