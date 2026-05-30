#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:UserSecurityAppService
// Guid:7d8e9f0a-1b2c-4d3e-4f5a-6b7c8d9e0f1a
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
/// 用户安全命令应用服务
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "用户安全")]
public sealed class UserSecurityAppService
    : SaasApplicationService, IUserSecurityAppService
{
    private readonly IUserDomainService _userDomainService;

    /// <summary>
    /// 构造函数
    /// </summary>
    public UserSecurityAppService(IUserDomainService userDomainService)
    {
        _userDomainService = userDomainService;
    }

    #region 用户安全

    /// <summary>
    /// 重置用户密码
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.UserSecurity.ResetPassword)]
    public async Task<UserSecurityDetailDto> ResetUserPasswordAsync(UserPasswordResetDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _userDomainService.ResetUserPasswordAsync(UserSecurityApplicationMapper.ToPasswordResetCommand(input), cancellationToken);
        return UserSecurityApplicationMapper.ToDetailDto(result.Security, result.User, result.Now);
    }

    /// <summary>
    /// 更新用户锁定状态
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.UserSecurity.Lock)]
    public async Task<UserSecurityDetailDto> UpdateUserLockAsync(UserLockUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _userDomainService.UpdateUserLockAsync(UserSecurityApplicationMapper.ToLockCommand(input), cancellationToken);
        return UserSecurityApplicationMapper.ToDetailDto(result.Security, result.User, result.Now);
    }

    /// <summary>
    /// 更新用户登录策略
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.UserSecurity.LoginPolicy)]
    public async Task<UserSecurityDetailDto> UpdateUserLoginPolicyAsync(UserLoginPolicyUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _userDomainService.UpdateUserLoginPolicyAsync(UserSecurityApplicationMapper.ToLoginPolicyCommand(input), cancellationToken);
        return UserSecurityApplicationMapper.ToDetailDto(result.Security, result.User, result.Now);
    }

    #endregion

}
