#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ProfileAppService.Security
// Guid:e85e27b0-2332-46c9-9e1e-c1bc7dd255dd
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/04 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.Mappers;
using XiHan.BasicApp.Saas.Application.Services;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.Framework.Uow.Attributes;

namespace XiHan.BasicApp.Saas.Application.AppServices;

/// <summary>
/// 个人中心应用服务（密码与双因素安全关注点）。
/// </summary>
public sealed partial class ProfileAppService
{
    /// <inheritdoc />
    [UnitOfWork(true)]
    public async Task ChangePasswordAsync(ProfileChangePasswordDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var currentUserId = GetCurrentUserIdOrThrow();
        var result = await _profileDomainService.ChangePasswordAsync(
            ProfileApplicationMapper.ToChangePasswordCommand(input, currentUserId),
            cancellationToken);

        // 认证审计：密码修改落登录日志
        await PublishSecurityAuditAsync(LoginResult.PasswordChanged, "用户修改密码");

        await _notificationDispatchService.DispatchToUserAsync(
            result.User.BasicId,
            "密码已修改",
            "您的账号密码已更新，如非本人操作，请立即联系管理员。",
            NotificationType.Warning,
            "profile.password.changed",
            result.User.BasicId,
            cancellationToken: cancellationToken);
    }

    /// <inheritdoc />
    [UnitOfWork(true)]
    public async Task Disable2FAAsync(ProfileTwoFactorVerifyDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var userId = GetCurrentUserIdOrThrow();
        var context = await _profileQueryService.GetSecurityContextAsync(userId, cancellationToken);
        var method = ToTwoFactorMethod(input.Method);
        if (!context.Security.TwoFactorMethod.HasFlag(method))
        {
            return;
        }

        await _profileVerificationService.EnsureTwoFactorCodeValidAsync(context, method, input.Code, cancellationToken);
        await _profileDomainService.DisableTwoFactorAsync(ProfileApplicationMapper.ToTwoFactorCommand(userId, method), cancellationToken);

        // 认证审计：解绑 MFA 落登录日志
        await PublishSecurityAuditAsync(LoginResult.MfaUnbound, $"解绑两步验证（{method}）");
    }

    /// <inheritdoc />
    [UnitOfWork(true)]
    public async Task Enable2FAAsync(ProfileTwoFactorVerifyDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var userId = GetCurrentUserIdOrThrow();
        var context = await _profileQueryService.GetSecurityContextAsync(userId, cancellationToken);
        var method = ToTwoFactorMethod(input.Method);
        await _profileVerificationService.EnsureTwoFactorCodeValidAsync(context, method, input.Code, cancellationToken);
        await _profileDomainService.EnableTwoFactorAsync(ProfileApplicationMapper.ToTwoFactorCommand(userId, method), cancellationToken);

        // 认证审计：绑定 MFA 落登录日志
        await PublishSecurityAuditAsync(LoginResult.MfaBound, $"绑定两步验证（{method}）");
    }

    /// <inheritdoc />
    public async Task<ProfileVerificationCodeResultDto> Send2FASetupCodeAsync(ProfileTwoFactorMethodDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var context = await _profileQueryService.GetSecurityContextAsync(GetCurrentUserIdOrThrow(), cancellationToken);
        var method = ToTwoFactorMethod(input.Method);
        return method switch
        {
            TwoFactorMethod.Email => await _profileVerificationService.SendCodeAsync(context.User, ProfileVerificationPurpose.TwoFactorEmail, context.User.Email, "邮箱两步验证", cancellationToken),
            TwoFactorMethod.Phone => await _profileVerificationService.SendCodeAsync(context.User, ProfileVerificationPurpose.TwoFactorPhone, context.User.Phone, "手机两步验证", cancellationToken),
            _ => throw new InvalidOperationException("该双因素方式不需要发送验证码。")
        };
    }

    /// <inheritdoc />
    [UnitOfWork(true)]
    public async Task<ProfileTwoFactorSetupDto> Setup2FAAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var currentUserId = GetCurrentUserIdOrThrow();
        var result = await _profileDomainService.SetupTwoFactorAsync(
            ProfileApplicationMapper.ToTwoFactorSetupCommand(currentUserId, "XiHan BasicApp"),
            cancellationToken);

        return ProfileApplicationMapper.ToTwoFactorSetupDto(result);
    }
}
