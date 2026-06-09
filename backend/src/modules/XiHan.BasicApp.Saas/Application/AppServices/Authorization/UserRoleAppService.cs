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
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Permissions;
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

    /// <summary>
    /// 构造函数
    /// </summary>
    public UserRoleAppService(IUserDomainService userDomainService, ISaasCacheInvalidator cacheInvalidator)
    {
        _userDomainService = userDomainService;
        _cacheInvalidator = cacheInvalidator;
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

        var result = await _userDomainService.UpdateUserRoleStatusAsync(UserRoleApplicationMapper.ToStatusCommand(input), cancellationToken);
        await _cacheInvalidator.InvalidateAuthorizationAsync(cancellationToken: cancellationToken);
        return UserRoleApplicationMapper.ToDetailDto(result.UserRole, result.Role, result.TenantMember, result.Now);
    }

    #endregion

}
