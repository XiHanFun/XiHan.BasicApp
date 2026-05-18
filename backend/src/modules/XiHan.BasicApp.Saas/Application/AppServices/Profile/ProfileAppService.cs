#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ProfileAppService
// Guid:d74d16af-1221-45b8-8d0d-b0ab6cc144cc
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/04 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.AspNetCore.Authorization;
using XiHan.BasicApp.Saas.Application.Contracts;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.Mappers;
using XiHan.BasicApp.Saas.Application.QueryServices;
using XiHan.BasicApp.Saas.Application.Services;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Events;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.EventBus.Abstractions.Local;
using XiHan.Framework.Security.Claims;
using XiHan.Framework.Security.Users;
using XiHan.Framework.Uow.Attributes;

namespace XiHan.BasicApp.Saas.Application.AppServices;

/// <summary>
/// 当前用户个人中心应用服务
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "个人中心")]
public sealed class ProfileAppService
    : SaasApplicationService, IProfileAppService
{
    private readonly ICurrentUser _currentUser;

    private readonly ILocalEventBus _localEventBus;

    private readonly IUserNotificationDispatchService _notificationDispatchService;

    private readonly IProfileDomainService _profileDomainService;

    private readonly IProfileQueryService _profileQueryService;

    private readonly IProfileVerificationService _profileVerificationService;

    /// <summary>
    /// 构造函数
    /// </summary>
    public ProfileAppService(
        IProfileDomainService profileDomainService,
        IProfileQueryService profileQueryService,
        IProfileVerificationService profileVerificationService,
        ILocalEventBus localEventBus,
        IUserNotificationDispatchService notificationDispatchService,
        ICurrentUser currentUser)
    {
        _profileDomainService = profileDomainService;
        _profileQueryService = profileQueryService;
        _profileVerificationService = profileVerificationService;
        _localEventBus = localEventBus;
        _notificationDispatchService = notificationDispatchService;
        _currentUser = currentUser;
    }

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
    public async Task ChangeUserNameAsync(ProfileChangeUserNameDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var currentUserId = GetCurrentUserIdOrThrow();
        var result = await _profileDomainService.ChangeUserNameAsync(
            ProfileApplicationMapper.ToChangeUserNameCommand(input, currentUserId),
            cancellationToken);

        await _notificationDispatchService.DispatchToUserAsync(
            result.User.BasicId,
            "用户名已修改",
            $"您的账号用户名已修改为 {result.User.UserName}。",
            NotificationType.User,
            "profile.username.changed",
            result.User.BasicId,
            cancellationToken: cancellationToken);
    }

    /// <inheritdoc />
    [UnitOfWork(true)]
    public async Task ConfirmChangeEmailAsync(ProfileVerificationCodeDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var userId = GetCurrentUserIdOrThrow();
        var pendingEmail = _profileVerificationService.ConsumeCode(userId, ProfileVerificationPurpose.ChangeEmail, input.Code);
        await _profileDomainService.ConfirmContactAsync(
            ProfileApplicationMapper.ToConfirmContactCommand(userId, ProfileContactKind.Email, pendingEmail),
            cancellationToken);
    }

    /// <inheritdoc />
    [UnitOfWork(true)]
    public async Task ConfirmChangePhoneAsync(ProfileVerificationCodeDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var userId = GetCurrentUserIdOrThrow();
        var pendingPhone = _profileVerificationService.ConsumeCode(userId, ProfileVerificationPurpose.ChangePhone, input.Code);
        await _profileDomainService.ConfirmContactAsync(
            ProfileApplicationMapper.ToConfirmContactCommand(userId, ProfileContactKind.Phone, pendingPhone),
            cancellationToken);
    }

    /// <inheritdoc />
    [UnitOfWork(true)]
    public async Task DeactivateAccountAsync(ProfilePasswordConfirmDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var currentUserId = GetCurrentUserIdOrThrow();
        var result = await _profileDomainService.DeactivateAccountAsync(
            ProfileApplicationMapper.ToPasswordConfirmCommand(input, currentUserId, _currentUser.UserId),
            cancellationToken);
        await PublishSessionRevokedEventsAsync(result.DomainEvents, cancellationToken);
    }

    /// <inheritdoc />
    [UnitOfWork(true)]
    public async Task DeleteAccountAsync(ProfilePasswordConfirmDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var currentUserId = GetCurrentUserIdOrThrow();
        var result = await _profileDomainService.DeleteAccountAsync(
            ProfileApplicationMapper.ToPasswordConfirmCommand(input, currentUserId, _currentUser.UserId),
            cancellationToken);
        await PublishSessionRevokedEventsAsync(result.DomainEvents, cancellationToken);
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

        _profileVerificationService.EnsureTwoFactorCodeValid(context, method, input.Code);
        await _profileDomainService.DisableTwoFactorAsync(ProfileApplicationMapper.ToTwoFactorCommand(userId, method), cancellationToken);
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
        _profileVerificationService.EnsureTwoFactorCodeValid(context, method, input.Code);
        await _profileDomainService.EnableTwoFactorAsync(ProfileApplicationMapper.ToTwoFactorCommand(userId, method), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<List<ProfileExternalLoginDto>> GetLinkedAccountsAsync(CancellationToken cancellationToken = default)
    {
        return await _profileQueryService.GetLinkedAccountsAsync(GetCurrentUserIdOrThrow(), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ProfileLoginLogPageDto> GetLoginLogsAsync(int page = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        return await _profileQueryService.GetLoginLogsAsync(GetCurrentUserIdOrThrow(), page, pageSize, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<UserProfileDto> GetProfileAsync(CancellationToken cancellationToken = default)
    {
        return await _profileQueryService.GetProfileAsync(GetCurrentUserIdOrThrow(), _currentUser.TenantId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<List<ProfileSessionDto>> GetSessionsAsync(CancellationToken cancellationToken = default)
    {
        return await _profileQueryService.GetSessionsAsync(GetCurrentUserIdOrThrow(), GetCurrentSessionId(), cancellationToken);
    }

    /// <inheritdoc />
    [UnitOfWork(true)]
    public async Task RevokeOtherSessionsAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var currentUserId = GetCurrentUserIdOrThrow();
        var result = await _profileDomainService.RevokeOtherSessionsAsync(
            ProfileApplicationMapper.ToOtherSessionsRevokeCommand(currentUserId, GetCurrentSessionId(), _currentUser.UserId),
            cancellationToken);
        await PublishSessionRevokedEventsAsync(result.DomainEvents, cancellationToken);
    }

    /// <inheritdoc />
    [UnitOfWork(true)]
    public async Task RevokeSessionAsync(ProfileSessionRevokeDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var currentUserId = GetCurrentUserIdOrThrow();
        var result = await _profileDomainService.RevokeSessionAsync(
            ProfileApplicationMapper.ToSessionRevokeCommand(input, currentUserId, GetCurrentSessionId(), _currentUser.UserId),
            cancellationToken);
        await PublishSessionRevokedEventsAsync(result.DomainEvents, cancellationToken);
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
    public async Task<ProfileVerificationCodeResultDto> SendChangeEmailCodeAsync(ProfileChangeEmailDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var currentUserId = GetCurrentUserIdOrThrow();
        var result = await _profileDomainService.PrepareChangeContactAsync(
            ProfileApplicationMapper.ToChangeContactPrepareCommand(currentUserId, ProfileContactKind.Email, input.NewEmail, input.Password),
            cancellationToken);
        return await _profileVerificationService.SendCodeAsync(result.User, ProfileVerificationPurpose.ChangeEmail, result.Target, "邮箱换绑", cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ProfileVerificationCodeResultDto> SendChangePhoneCodeAsync(ProfileChangePhoneDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var currentUserId = GetCurrentUserIdOrThrow();
        var result = await _profileDomainService.PrepareChangeContactAsync(
            ProfileApplicationMapper.ToChangeContactPrepareCommand(currentUserId, ProfileContactKind.Phone, input.NewPhone, input.Password),
            cancellationToken);
        return await _profileVerificationService.SendCodeAsync(result.User, ProfileVerificationPurpose.ChangePhone, result.Target, "手机换绑", cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ProfileVerificationCodeResultDto> SendEmailVerifyCodeAsync(CancellationToken cancellationToken = default)
    {
        var context = await _profileQueryService.GetSecurityContextAsync(GetCurrentUserIdOrThrow(), cancellationToken);
        if (string.IsNullOrWhiteSpace(context.User.Email))
        {
            throw new InvalidOperationException("当前账号未绑定邮箱。");
        }

        return await _profileVerificationService.SendCodeAsync(context.User, ProfileVerificationPurpose.VerifyEmail, context.User.Email, "邮箱验证", cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ProfileVerificationCodeResultDto> SendPhoneVerifyCodeAsync(CancellationToken cancellationToken = default)
    {
        var context = await _profileQueryService.GetSecurityContextAsync(GetCurrentUserIdOrThrow(), cancellationToken);
        if (string.IsNullOrWhiteSpace(context.User.Phone))
        {
            throw new InvalidOperationException("当前账号未绑定手机号。");
        }

        return await _profileVerificationService.SendCodeAsync(context.User, ProfileVerificationPurpose.VerifyPhone, context.User.Phone, "手机验证", cancellationToken);
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

    /// <inheritdoc />
    [UnitOfWork(true)]
    public async Task UnlinkAccountAsync(ProfileUnlinkAccountDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        await _profileDomainService.UnlinkAccountAsync(
            ProfileApplicationMapper.ToUnlinkAccountCommand(input, GetCurrentUserIdOrThrow()),
            cancellationToken);
    }

    /// <inheritdoc />
    [UnitOfWork(true)]
    public async Task<UserProfileDto> UpdateProfileAsync(ProfileUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var userId = GetCurrentUserIdOrThrow();
        await _profileDomainService.UpdateProfileAsync(
            ProfileApplicationMapper.ToUpdateCommand(input, userId),
            cancellationToken);

        return await _profileQueryService.GetProfileAsync(userId, _currentUser.TenantId, cancellationToken);
    }

    /// <inheritdoc />
    [UnitOfWork(true)]
    public async Task VerifyEmailAsync(ProfileVerificationCodeDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var userId = GetCurrentUserIdOrThrow();
        _ = _profileVerificationService.ConsumeCode(userId, ProfileVerificationPurpose.VerifyEmail, input.Code);
        await _profileDomainService.VerifyContactAsync(
            ProfileApplicationMapper.ToVerifyContactCommand(userId, ProfileContactKind.Email),
            cancellationToken);
    }

    /// <inheritdoc />
    [UnitOfWork(true)]
    public async Task VerifyPhoneAsync(ProfileVerificationCodeDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var userId = GetCurrentUserIdOrThrow();
        _ = _profileVerificationService.ConsumeCode(userId, ProfileVerificationPurpose.VerifyPhone, input.Code);
        await _profileDomainService.VerifyContactAsync(
            ProfileApplicationMapper.ToVerifyContactCommand(userId, ProfileContactKind.Phone),
            cancellationToken);
    }

    private static TwoFactorMethod ToTwoFactorMethod(int method)
    {
        return method switch
        {
            (int)TwoFactorMethod.Totp => TwoFactorMethod.Totp,
            (int)TwoFactorMethod.Email => TwoFactorMethod.Email,
            (int)TwoFactorMethod.Phone => TwoFactorMethod.Phone,
            _ => throw new ArgumentOutOfRangeException(nameof(method), "双因素方式无效。")
        };
    }

    private string? GetCurrentSessionId()
    {
        return _currentUser.FindClaim(XiHanClaimTypes.SessionId)?.Value;
    }

    private long GetCurrentUserIdOrThrow()
    {
        return _currentUser.UserId ?? throw new InvalidOperationException("当前用户未登录。");
    }

    private async Task PublishSessionRevokedEventsAsync(
        IReadOnlyList<UserSessionRevokedDomainEvent> domainEvents,
        CancellationToken cancellationToken)
    {
        foreach (var domainEvent in domainEvents)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await _localEventBus.PublishAsync(domainEvent);
        }
    }

}
