#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:UserDataScopeAppService
// Guid:1b2c3d4e-5f6a-4b7c-8d9e-0f1a2b3c4d5e
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/01 00:00:00
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
/// 用户数据范围命令应用服务
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "用户数据范围")]
public sealed class UserDataScopeAppService
    : SaasApplicationService, IUserDataScopeAppService
{
    private readonly IUserDomainService _userDomainService;

    /// <summary>
    /// 构造函数
    /// </summary>
    public UserDataScopeAppService(IUserDomainService userDomainService)
    {
        _userDomainService = userDomainService;
    }

    #region 用户数据范围

    /// <summary>
    /// 授予用户数据范围
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.UserDataScope.Grant)]
    public async Task<UserDataScopeDetailDto> CreateUserDataScopeAsync(UserDataScopeGrantDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _userDomainService.CreateUserDataScopeAsync(UserDataScopeApplicationMapper.ToGrantCommand(input), cancellationToken);
        return UserDataScopeApplicationMapper.ToDetailDto(result.DataScope, result.Department, result.TenantMember);
    }

    /// <summary>
    /// 撤销用户数据范围
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.UserDataScope.Revoke)]
    public async Task DeleteUserDataScopeAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _userDomainService.DeleteUserDataScopeAsync(id, cancellationToken);
    }

    /// <summary>
    /// 更新用户数据范围
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.UserDataScope.Update)]
    public async Task<UserDataScopeDetailDto> UpdateUserDataScopeAsync(UserDataScopeUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _userDomainService.UpdateUserDataScopeAsync(UserDataScopeApplicationMapper.ToUpdateCommand(input), cancellationToken);
        return UserDataScopeApplicationMapper.ToDetailDto(result.DataScope, result.Department, result.TenantMember);
    }

    /// <summary>
    /// 更新用户数据范围状态
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.UserDataScope.Status)]
    public async Task<UserDataScopeDetailDto> UpdateUserDataScopeStatusAsync(UserDataScopeStatusUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _userDomainService.UpdateUserDataScopeStatusAsync(UserDataScopeApplicationMapper.ToStatusCommand(input), cancellationToken);
        return UserDataScopeApplicationMapper.ToDetailDto(result.DataScope, result.Department, result.TenantMember);
    }

    #endregion
}
