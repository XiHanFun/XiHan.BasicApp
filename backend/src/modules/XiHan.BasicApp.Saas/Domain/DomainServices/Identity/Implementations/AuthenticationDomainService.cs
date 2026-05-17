#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:AuthenticationDomainService
// Guid:7e6852f4-379c-461e-99f1-2e88c5b06555
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/17 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Authentication.Users;

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 认证领域服务实现
/// </summary>
public sealed class AuthenticationDomainService
    : IAuthenticationDomainService
{
    private readonly IAuthenticationService _authenticationService;

    private readonly IUserRepository _userRepository;

    private readonly IUserSecurityRepository _userSecurityRepository;

    private readonly ITenantUserRepository _tenantUserRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public AuthenticationDomainService(
        IAuthenticationService authenticationService,
        IUserRepository userRepository,
        IUserSecurityRepository userSecurityRepository,
        ITenantUserRepository tenantUserRepository)
    {
        _authenticationService = authenticationService;
        _userRepository = userRepository;
        _userSecurityRepository = userSecurityRepository;
        _tenantUserRepository = tenantUserRepository;
    }
    /// <inheritdoc />
    public async Task<LoginAuthenticationResult> AuthenticatePasswordLoginAsync(
        string userName,
        string password,
        long? tenantId,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var authResult = await _authenticationService.AuthenticateAsync(userName, password, cancellationToken);
        if (!authResult.Succeeded && !authResult.RequiresTwoFactor)
        {
            return LoginAuthenticationResult.Failed(
                authResult.IsLockedOut ? LoginResult.AccountLocked : LoginResult.InvalidCredentials,
                authResult.ErrorMessage ?? "用户名或密码错误。");
        }

        if (!long.TryParse(authResult.UserId, out var userId))
        {
            throw new InvalidOperationException("用户标识无效。");
        }

        var user = await _userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new InvalidOperationException("认证用户不存在。");
        var security = await _userSecurityRepository.GetByUserIdAsync(userId, cancellationToken);

        if (authResult.RequiresTwoFactor && security is not null && security.TwoFactorMethod != TwoFactorMethod.None)
        {
            return LoginAuthenticationResult.TwoFactorRequired(user, security);
        }

        if (authResult.RequiresTwoFactor)
        {
            return LoginAuthenticationResult.Failed(LoginResult.RequiresTwoFactor, "用户双因素配置不存在。");
        }

        if (user.Status != EnableStatus.Enabled)
        {
            return LoginAuthenticationResult.Failed(LoginResult.AccountDisabled, "用户已被禁用。");
        }

        if (tenantId.HasValue)
        {
            var membership = await _tenantUserRepository.GetMembershipAsync(userId, cancellationToken);
            if (membership is null)
            {
                return LoginAuthenticationResult.Failed(LoginResult.Failed, "用户不是当前租户成员。");
            }

            if (membership.InviteStatus != TenantMemberInviteStatus.Accepted || membership.Status != ValidityStatus.Valid)
            {
                return LoginAuthenticationResult.Failed(LoginResult.Failed, "用户当前租户成员身份无效。");
            }

            if (membership.EffectiveTime.HasValue && membership.EffectiveTime.Value > now)
            {
                return LoginAuthenticationResult.Failed(LoginResult.Failed, "用户当前租户成员身份尚未生效。");
            }

            if (membership.ExpirationTime.HasValue && membership.ExpirationTime.Value <= now)
            {
                return LoginAuthenticationResult.Failed(LoginResult.Failed, "用户当前租户成员身份已过期。");
            }
        }

        if (security is not null && security.PasswordExpiryTime.HasValue && security.PasswordExpiryTime.Value <= now)
        {
            return LoginAuthenticationResult.Failed(LoginResult.Failed, "密码已过期，请联系管理员重置密码。");
        }

        return LoginAuthenticationResult.Success(user, security);
    }
}
