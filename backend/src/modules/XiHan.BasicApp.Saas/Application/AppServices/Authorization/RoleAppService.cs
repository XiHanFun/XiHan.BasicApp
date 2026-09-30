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

    private readonly IRoleHierarchyDomainService _roleHierarchyDomainService;

    private readonly ISaasCacheInvalidator _cacheInvalidator;

    private readonly IAuthorizationChangeNotifier _authorizationChangeNotifier;

    private readonly IImpersonationPolicyService _impersonationPolicyService;

    private readonly ISuperAdminProtector _superAdminProtector;

    private readonly IRolePermissionRepository _rolePermissionRepository;

    private readonly IOperationPermissionGuard _operationPermissionGuard;

    private readonly IFieldSecurityService _fieldSecurity;

    /// <summary>
    /// 构造函数
    /// </summary>
    public RoleAppService(
        IRoleDomainService roleDomainService,
        IRoleHierarchyDomainService roleHierarchyDomainService,
        ISaasCacheInvalidator cacheInvalidator,
        IAuthorizationChangeNotifier authorizationChangeNotifier,
        IImpersonationPolicyService impersonationPolicyService,
        ISuperAdminProtector superAdminProtector,
        IRolePermissionRepository rolePermissionRepository,
        IOperationPermissionGuard operationPermissionGuard,
        IFieldSecurityService fieldSecurity)
    {
        _roleDomainService = roleDomainService;
        _roleHierarchyDomainService = roleHierarchyDomainService;
        _cacheInvalidator = cacheInvalidator;
        _authorizationChangeNotifier = authorizationChangeNotifier;
        _impersonationPolicyService = impersonationPolicyService;
        _superAdminProtector = superAdminProtector;
        _rolePermissionRepository = rolePermissionRepository;
        _operationPermissionGuard = operationPermissionGuard;
        _fieldSecurity = fieldSecurity;
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

        // 字段安全：只读字段不能填写
        await _fieldSecurity.EnsureCreatableAsync(typeof(SysRole), input, cancellationToken);

        var result = await _roleDomainService.CreateRoleAsync(RoleApplicationMapper.ToCreateCommand(input), cancellationToken);
        await _cacheInvalidator.InvalidateAuthorizationAsync(cancellationToken: cancellationToken);
        await _cacheInvalidator.InvalidateRoleDefinitionAsync(cancellationToken);
        return RoleApplicationMapper.ToDetailDto(result.Role);
    }

    /// <summary>
    /// 设置角色数据范围：档位与自定义部门一次提交（单事务，仅在最后失效一次缓存）
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.RoleDataScope.Update)]
    public async Task SetRoleDataScopeAsync(RoleDataScopeSetDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        await _superAdminProtector.EnsureCanWriteRoleAsync(input.RoleId, cancellationToken);
        _ = await _roleDomainService.SetRoleDataScopeAsync(
            new RoleDataScopeSetCommand(
                input.RoleId,
                input.DataScope,
                [.. input.Departments.Select(item => new DataScopeDepartmentItem(item.DepartmentId, item.IncludeChildren))]),
            cancellationToken);
        await _cacheInvalidator.InvalidateAuthorizationAsync(cancellationToken: cancellationToken);
    }

    /// <summary>
    /// 批量变更角色的直接上级（一次提交新增与解除，单事务）
    /// </summary>
    /// <remarks>
    /// 入口只要查看继承关系；本次新增上级要新增权限，解除上级要删除权限，各按实际出现的操作校验。
    /// 新增上级等同把上级继承链的权限授给本角色，与分配角色走同一道模仿登录授出校验。
    /// </remarks>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.RoleHierarchy.Read)]
    public async Task BatchUpdateRoleParentsAsync(RoleHierarchyBatchUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var addParentRoleIds = input.AddParentRoleIds.Where(id => id > 0).Distinct().ToList();
        var removeParentRoleIds = input.RemoveParentRoleIds.Where(id => id > 0).Distinct().ToList();
        if (addParentRoleIds.Count > 0)
        {
            await _operationPermissionGuard.EnsureGrantedAsync(SaasPermissionCodes.RoleHierarchy.Create, cancellationToken);
        }

        if (removeParentRoleIds.Count > 0)
        {
            await _operationPermissionGuard.EnsureGrantedAsync(SaasPermissionCodes.RoleHierarchy.Delete, cancellationToken);
        }

        // 超管保护：继承方与本次涉及的每个上级逐个过同一道闸
        await _superAdminProtector.EnsureCanWriteRoleAsync(input.RoleId, cancellationToken);
        foreach (var parentId in addParentRoleIds.Concat(removeParentRoleIds).Distinct())
        {
            await _superAdminProtector.EnsureCanWriteRoleAsync(parentId, cancellationToken);
        }

        await _impersonationPolicyService.EnsureCanGrantRoleIdsAsync(addParentRoleIds, cancellationToken);

        var result = await _roleHierarchyDomainService.UpdateParentsAsync(
            new RoleHierarchyBatchUpdateCommand(input.RoleId, addParentRoleIds, removeParentRoleIds),
            cancellationToken);

        if (result.AddedParentRoleIds.Count == 0 && result.RemovedParentRoleIds.Count == 0)
        {
            return;
        }

        // 继承链变了，持有本角色及其下级的成员权限随之变化，菜单也跟着权限走
        await _cacheInvalidator.InvalidateAuthorizationAsync(cancellationToken: cancellationToken);
        await _cacheInvalidator.InvalidateNavigationAsync(cancellationToken);

        // 逐条记录本次实际发生的继承变更（审计）
        foreach (var parentId in result.RemovedParentRoleIds)
        {
            await _authorizationChangeNotifier.NotifyAsync(
                PermissionChangeType.RoleRemoveParent,
                targetUserId: null,
                targetRoleId: input.RoleId,
                permissionId: null,
                relatedRoleId: parentId,
                cancellationToken: cancellationToken);
        }

        foreach (var parentId in result.AddedParentRoleIds)
        {
            await _authorizationChangeNotifier.NotifyAsync(
                PermissionChangeType.RoleAddParent,
                targetUserId: null,
                targetRoleId: input.RoleId,
                permissionId: null,
                relatedRoleId: parentId,
                cancellationToken: cancellationToken);
        }
    }

    /// <summary>
    /// 批量变更角色权限（一次性提交授予与撤销，单事务，仅在最后失效一次缓存）
    /// </summary>
    /// <remarks>
    /// 入口与「分配权限」按钮同挂授予权限；本次含撤销项时再要撤销权限，只加不减的提交不需要撤销权限。
    /// </remarks>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.RolePermission.Grant)]
    public async Task BatchUpdateRolePermissionsAsync(RolePermissionBatchUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        if (input.RevokeRolePermissionIds.Any(id => id > 0))
        {
            await _operationPermissionGuard.EnsureGrantedAsync(SaasPermissionCodes.RolePermission.Revoke, cancellationToken);
        }

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

        // 字段安全：只读字段不能改，表单交回的脱敏值还原为原值
        await _fieldSecurity.EnsureUpdatableAsync(typeof(SysRole), input.BasicId, input, cancellationToken);

        await _superAdminProtector.EnsureCanWriteRoleAsync(input.BasicId, cancellationToken);
        var result = await _roleDomainService.UpdateRoleAsync(RoleApplicationMapper.ToUpdateCommand(input), cancellationToken);
        await _cacheInvalidator.InvalidateAuthorizationAsync(cancellationToken: cancellationToken);
        await _cacheInvalidator.InvalidateRoleDefinitionAsync(cancellationToken);
        return RoleApplicationMapper.ToDetailDto(result.Role);
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

        // 字段安全：只读字段不能改，表单交回的脱敏值还原为原值
        await _fieldSecurity.EnsureUpdatableAsync(typeof(SysRole), input.BasicId, input, cancellationToken);

        await _superAdminProtector.EnsureCanWriteRoleAsync(input.BasicId, cancellationToken);
        var result = await _roleDomainService.UpdateRoleStatusAsync(RoleApplicationMapper.ToStatusCommand(input), cancellationToken);
        await _cacheInvalidator.InvalidateAuthorizationAsync(cancellationToken: cancellationToken);
        await _cacheInvalidator.InvalidateRoleDefinitionAsync(cancellationToken);
        return RoleApplicationMapper.ToDetailDto(result.Role);
    }
}
