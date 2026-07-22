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

    private readonly ISuperAdminProtector _superAdminProtector;

    private readonly IRolePermissionRepository _rolePermissionRepository;

    private readonly IRoleDataScopeRepository _roleDataScopeRepository;

    private readonly IRoleHierarchyRepository _roleHierarchyRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public RoleAppService(
        IRoleDomainService roleDomainService,
        ISaasCacheInvalidator cacheInvalidator,
        IAuthorizationChangeNotifier authorizationChangeNotifier,
        ISuperAdminProtector superAdminProtector,
        IRolePermissionRepository rolePermissionRepository,
        IRoleDataScopeRepository roleDataScopeRepository,
        IRoleHierarchyRepository roleHierarchyRepository)
    {
        _roleDomainService = roleDomainService;
        _cacheInvalidator = cacheInvalidator;
        _authorizationChangeNotifier = authorizationChangeNotifier;
        _superAdminProtector = superAdminProtector;
        _rolePermissionRepository = rolePermissionRepository;
        _roleDataScopeRepository = roleDataScopeRepository;
        _roleHierarchyRepository = roleHierarchyRepository;
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
    /// 授予角色数据范围
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.RoleDataScope.Grant)]
    public async Task<RoleDataScopeDetailDto> CreateRoleDataScopeAsync(RoleDataScopeGrantDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        await _superAdminProtector.EnsureCanWriteRoleAsync(input.RoleId, cancellationToken);
        var result = await _roleDomainService.CreateRoleDataScopeAsync(RoleDataScopeApplicationMapper.ToGrantCommand(input), cancellationToken);
        await _cacheInvalidator.InvalidateAuthorizationAsync(cancellationToken: cancellationToken);
        return RoleDataScopeApplicationMapper.ToDetailDto(result.DataScope, result.Department);
    }

    /// <summary>
    /// 创建角色直接继承关系
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.RoleHierarchy.Create)]
    public async Task<RoleHierarchyDetailDto> CreateRoleHierarchyAsync(RoleHierarchyCreateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        await _superAdminProtector.EnsureCanWriteRoleAsync(input.AncestorId, cancellationToken);
        await _superAdminProtector.EnsureCanWriteRoleAsync(input.DescendantId, cancellationToken);
        var result = await _roleDomainService.CreateRoleHierarchyAsync(RoleHierarchyApplicationMapper.ToCreateCommand(input), cancellationToken);
        await _cacheInvalidator.InvalidateAuthorizationAsync(cancellationToken: cancellationToken);
        return RoleHierarchyApplicationMapper.ToDetailDto(result.Hierarchy, result.Ancestor, result.Descendant);
    }

    /// <summary>
    /// 授予角色权限
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.RolePermission.Grant)]
    public async Task<RolePermissionDetailDto> CreateRolePermissionAsync(RolePermissionGrantDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        await _superAdminProtector.EnsureCanWriteRoleAsync(input.RoleId, cancellationToken);
        var result = await _roleDomainService.CreateRolePermissionAsync(RolePermissionApplicationMapper.ToGrantCommand(input), cancellationToken);
        await _cacheInvalidator.InvalidateAuthorizationAsync(cancellationToken: cancellationToken);
        await _authorizationChangeNotifier.NotifyAsync(
            result.RolePermission.PermissionAction == PermissionAction.Deny ? PermissionChangeType.RoleDenyPermission : PermissionChangeType.RoleGrantPermission,
            targetUserId: null,
            targetRoleId: result.RolePermission.RoleId,
            permissionId: result.RolePermission.PermissionId,
            reason: result.RolePermission.GrantReason,
            cancellationToken: cancellationToken);
        return RolePermissionApplicationMapper.ToDetailDto(result.RolePermission, result.Permission);
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
    /// 撤销角色数据范围
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.RoleDataScope.Revoke)]
    public async Task DeleteRoleDataScopeAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var dataScope = await _roleDataScopeRepository.GetByIdAsync(id, cancellationToken);
        if (dataScope is not null)
        {
            await _superAdminProtector.EnsureCanWriteRoleAsync(dataScope.RoleId, cancellationToken);
        }
        await _roleDomainService.DeleteRoleDataScopeAsync(id, cancellationToken);
        await _cacheInvalidator.InvalidateAuthorizationAsync(cancellationToken: cancellationToken);
    }

    /// <summary>
    /// 删除角色直接继承关系
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.RoleHierarchy.Delete)]
    public async Task DeleteRoleHierarchyAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var hierarchy = await _roleHierarchyRepository.GetByIdAsync(id, cancellationToken);
        if (hierarchy is not null)
        {
            await _superAdminProtector.EnsureCanWriteRoleAsync(hierarchy.AncestorId, cancellationToken);
            await _superAdminProtector.EnsureCanWriteRoleAsync(hierarchy.DescendantId, cancellationToken);
        }
        await _roleDomainService.DeleteRoleHierarchyAsync(id, cancellationToken);
        await _cacheInvalidator.InvalidateAuthorizationAsync(cancellationToken: cancellationToken);
    }

    /// <summary>
    /// 撤销角色权限
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.RolePermission.Revoke)]
    public async Task DeleteRolePermissionAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var rolePermission = await _rolePermissionRepository.GetByIdAsync(id, cancellationToken);
        if (rolePermission is not null)
        {
            await _superAdminProtector.EnsureCanWriteRoleAsync(rolePermission.RoleId, cancellationToken);
        }
        await _roleDomainService.DeleteRolePermissionAsync(id, cancellationToken);
        await _cacheInvalidator.InvalidateAuthorizationAsync(cancellationToken: cancellationToken);
        if (rolePermission is not null)
        {
            await _authorizationChangeNotifier.NotifyAsync(
                PermissionChangeType.RoleRevokePermission,
                targetUserId: null,
                targetRoleId: rolePermission.RoleId,
                permissionId: rolePermission.PermissionId,
                cancellationToken: cancellationToken);
        }
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
