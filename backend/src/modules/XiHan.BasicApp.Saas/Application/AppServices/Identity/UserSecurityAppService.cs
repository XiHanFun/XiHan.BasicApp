// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.AspNetCore.Authorization;
using XiHan.BasicApp.Saas.Application.Contracts;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.Mappers;
using XiHan.BasicApp.Saas.Application.Services;
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

    private readonly ISuperAdminProtector _superAdminProtector;

    /// <summary>
    /// 构造函数
    /// </summary>
    public UserSecurityAppService(IUserDomainService userDomainService, ISuperAdminProtector superAdminProtector)
    {
        _userDomainService = userDomainService;
        _superAdminProtector = superAdminProtector;
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

        // 超管保护：非超管不得重置超管用户密码
        await _superAdminProtector.EnsureCanWriteUserAsync(input.UserId, cancellationToken);

        var result = await _userDomainService.ResetUserPasswordAsync(UserSecurityApplicationMapper.ToPasswordResetCommand(input), cancellationToken);
        return UserSecurityApplicationMapper.ToDetailDto(result.Security, result.User, result.Now);
    }

    /// <summary>
    /// 重置用户双因素认证（清除 OTP 绑定）
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.UserSecurity.ResetTwoFactor)]
    public async Task<UserSecurityDetailDto> ResetUserTwoFactorAsync(UserTwoFactorResetDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        // 超管保护：非超管不得重置超管用户双因素认证
        await _superAdminProtector.EnsureCanWriteUserAsync(input.UserId, cancellationToken);

        var result = await _userDomainService.ResetUserTwoFactorAsync(UserSecurityApplicationMapper.ToTwoFactorResetCommand(input), cancellationToken);
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

        // 超管保护：非超管不得锁定/解锁超管用户
        await _superAdminProtector.EnsureCanWriteUserAsync(input.UserId, cancellationToken);

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

        // 超管保护：非超管不得修改超管用户登录策略
        await _superAdminProtector.EnsureCanWriteUserAsync(input.UserId, cancellationToken);

        var result = await _userDomainService.UpdateUserLoginPolicyAsync(UserSecurityApplicationMapper.ToLoginPolicyCommand(input), cancellationToken);
        return UserSecurityApplicationMapper.ToDetailDto(result.Security, result.User, result.Now);
    }

    #endregion
}
