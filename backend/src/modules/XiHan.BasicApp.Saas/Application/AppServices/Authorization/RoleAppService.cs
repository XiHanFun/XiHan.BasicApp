#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:RoleAppService
// Guid:bc22b1f0-ff83-43ee-864e-2beb29117713
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/30 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.AspNetCore.Authorization;
using XiHan.BasicApp.Saas.Application.Contracts;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.Mappers;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Permissions;
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

    /// <summary>
    /// 构造函数
    /// </summary>
    public RoleAppService(IRoleDomainService roleDomainService)
    {
        _roleDomainService = roleDomainService;
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

        var result = await _roleDomainService.CreateRoleDataScopeAsync(RoleDataScopeApplicationMapper.ToGrantCommand(input), cancellationToken);
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

        var result = await _roleDomainService.CreateRoleHierarchyAsync(RoleHierarchyApplicationMapper.ToCreateCommand(input), cancellationToken);
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

        var result = await _roleDomainService.CreateRolePermissionAsync(RolePermissionApplicationMapper.ToGrantCommand(input), cancellationToken);
        return RolePermissionApplicationMapper.ToDetailDto(result.RolePermission, result.Permission);
    }

    /// <summary>
    /// 删除角色
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Role.Delete)]
    public async Task DeleteRoleAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _roleDomainService.DeleteRoleAsync(id, cancellationToken);
    }

    /// <summary>
    /// 撤销角色数据范围
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.RoleDataScope.Revoke)]
    public async Task DeleteRoleDataScopeAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _roleDomainService.DeleteRoleDataScopeAsync(id, cancellationToken);
    }

    /// <summary>
    /// 删除角色直接继承关系
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.RoleHierarchy.Delete)]
    public async Task DeleteRoleHierarchyAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _roleDomainService.DeleteRoleHierarchyAsync(id, cancellationToken);
    }

    /// <summary>
    /// 撤销角色权限
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.RolePermission.Revoke)]
    public async Task DeleteRolePermissionAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _roleDomainService.DeleteRolePermissionAsync(id, cancellationToken);
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

        var result = await _roleDomainService.UpdateRoleAsync(RoleApplicationMapper.ToUpdateCommand(input), cancellationToken);
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

        var result = await _roleDomainService.UpdateRoleDataScopeAsync(RoleDataScopeApplicationMapper.ToUpdateCommand(input), cancellationToken);
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

        var result = await _roleDomainService.UpdateRoleDataScopeStatusAsync(RoleDataScopeApplicationMapper.ToStatusCommand(input), cancellationToken);
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

        var result = await _roleDomainService.UpdateRolePermissionAsync(RolePermissionApplicationMapper.ToUpdateCommand(input), cancellationToken);
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

        var result = await _roleDomainService.UpdateRolePermissionStatusAsync(RolePermissionApplicationMapper.ToStatusCommand(input), cancellationToken);
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

        var result = await _roleDomainService.UpdateRoleStatusAsync(RoleApplicationMapper.ToStatusCommand(input), cancellationToken);
        return RoleApplicationMapper.ToDetailDto(result.Role);
    }

}
