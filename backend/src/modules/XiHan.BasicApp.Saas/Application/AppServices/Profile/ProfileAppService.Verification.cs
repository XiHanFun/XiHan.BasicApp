#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ProfileAppService.Verification
// Guid:f96f38c1-3443-47da-af2f-d2cd8ee366ee
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/04 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.Mappers;
using XiHan.BasicApp.Saas.Application.Services;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.Framework.Uow.Attributes;

namespace XiHan.BasicApp.Saas.Application.AppServices;

/// <summary>
/// 个人中心应用服务（邮箱与手机号验证关注点）。
/// </summary>
public sealed partial class ProfileAppService
{
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
}
