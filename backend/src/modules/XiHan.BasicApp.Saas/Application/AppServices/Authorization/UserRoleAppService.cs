#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:UserRoleAppService
// Guid:9f0a1b2c-3d4e-4f5a-6b7c-8d9e0f1a2b3c
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/01 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.AspNetCore.Authorization;
using XiHan.BasicApp.Saas.Application.Caching;
using XiHan.BasicApp.Saas.Application.Contracts;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.Mappers;
using XiHan.BasicApp.Saas.Application.Services;
using XiHan.BasicApp.Saas.Domain.DomainServices;
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

    private readonly ISuperAdminProtector _superAdminProtector;

    private readonly IUserRoleRepository _userRoleRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public UserRoleAppService(
        IUserDomainService userDomainService,
        ISaasCacheInvalidator cacheInvalidator,
        ISuperAdminProtector superAdminProtector,
        IUserRoleRepository userRoleRepository)
    {
        _userDomainService = userDomainService;
        _cacheInvalidator = cacheInvalidator;
        _superAdminProtector = superAdminProtector;
        _userRoleRepository = userRoleRepository;
    }

    #region 用户角色

    /// <summary>
    /// 授予用户角色
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.UserRole.Grant)]
    public async Task<UserRoleDetailDto> CreateUserRoleAsync(UserRoleGrantDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        // 超管保护：非超管不得改超管用户的角色，也不得把 super_admin 角色授予他人
        await _superAdminProtector.EnsureCanWriteUserAsync(input.UserId, cancellationToken);
        await _superAdminProtector.EnsureCanAssignRoleAsync(input.RoleId, cancellationToken);

        var result = await _userDomainService.CreateUserRoleAsync(UserRoleApplicationMapper.ToGrantCommand(input), cancellationToken);
        await _cacheInvalidator.InvalidateAuthorizationAsync(cancellationToken: cancellationToken);
        return UserRoleApplicationMapper.ToDetailDto(result.UserRole, result.Role, result.TenantMember, result.Now);
    }

    /// <summary>
    /// 撤销用户角色
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.UserRole.Revoke)]
    public async Task DeleteUserRoleAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        // 超管保护：解析该用户角色记录的 UserId/RoleId，非超管不得撤销超管用户的角色或撤销 super_admin 角色
        await EnsureCanWriteUserRoleAsync(id, cancellationToken);
        await _userDomainService.DeleteUserRoleAsync(id, cancellationToken);
        await _cacheInvalidator.InvalidateAuthorizationAsync(cancellationToken: cancellationToken);
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
    }

    #endregion
}
