#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:UserPermissionAppService
// Guid:0a1b2c3d-4e5f-4a6b-7c8d-9e0f1a2b3c4d
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
/// 用户直授权限命令应用服务
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "用户直授权限")]
public sealed class UserPermissionAppService
    : SaasApplicationService, IUserPermissionAppService
{
    private readonly IUserDomainService _userDomainService;

    private readonly ISaasCacheInvalidator _cacheInvalidator;

    /// <summary>
    /// 构造函数
    /// </summary>
    public UserPermissionAppService(IUserDomainService userDomainService, ISaasCacheInvalidator cacheInvalidator)
    {
        _userDomainService = userDomainService;
        _cacheInvalidator = cacheInvalidator;
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

        var result = await _userDomainService.CreateUserPermissionAsync(UserPermissionApplicationMapper.ToGrantCommand(input), cancellationToken);
        await _cacheInvalidator.InvalidateAuthorizationAsync(cancellationToken: cancellationToken);
        return UserPermissionApplicationMapper.ToDetailDto(result.UserPermission, result.Permission, result.TenantMember, result.Now);
    }

    /// <summary>
    /// 撤销用户直授权限
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.UserPermission.Revoke)]
    public async Task DeleteUserPermissionAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _userDomainService.DeleteUserPermissionAsync(id, cancellationToken);
        await _cacheInvalidator.InvalidateAuthorizationAsync(cancellationToken: cancellationToken);
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
        return UserPermissionApplicationMapper.ToDetailDto(result.UserPermission, result.Permission, result.TenantMember, result.Now);
    }

    #endregion
}
