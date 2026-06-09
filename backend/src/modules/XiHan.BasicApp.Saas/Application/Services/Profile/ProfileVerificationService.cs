#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ProfileVerificationService
// Guid:42aefccd-2eaa-4be5-a790-798efac48d6f
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/18 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using System.Collections.Concurrent;
using System.Security.Cryptography;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.QueryServices;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.Framework.Authentication.Otp;

namespace XiHan.BasicApp.Saas.Application.Services;

/// <summary>
/// 当前用户验证码应用服务实现
/// </summary>
public sealed class ProfileVerificationService
    : IProfileVerificationService
{
    private const string VerificationCodeBusinessType = "profile.verification-code";
    private const int VerificationCodeSeconds = 600;
    private static readonly ConcurrentDictionary<string, VerificationCodeState> VerificationCodes = new();

    private readonly IUserNotificationDispatchService _notificationDispatchService;

    private readonly IOtpService _otpService;

    /// <summary>
    /// 构造函数
    /// </summary>
    public ProfileVerificationService(
        IOtpService otpService,
        IUserNotificationDispatchService notificationDispatchService)
    {
        _otpService = otpService;
        _notificationDispatchService = notificationDispatchService;
    }

    /// <inheritdoc />
    public string ConsumeCode(long userId, ProfileVerificationPurpose purpose, string? code)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        var key = BuildVerificationKey(userId, purpose);
        if (!VerificationCodes.TryRemove(key, out var state) ||
            state.ExpiresAt <= DateTimeOffset.UtcNow ||
            !string.Equals(state.Code, code.Trim(), StringComparison.Ordinal))
        {
            throw new InvalidOperationException("验证码无效或已过期。");
        }

        return state.PendingValue;
    }

    /// <inheritdoc />
    public void EnsureTwoFactorCodeValid(ProfileUserSecurityContext context, TwoFactorMethod method, string? code)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        if (method == TwoFactorMethod.Totp)
        {
            if (string.IsNullOrWhiteSpace(context.Security.TwoFactorSecret))
            {
                throw new InvalidOperationException("请先初始化 TOTP 双因素认证。");
            }

            if (!_otpService.VerifyTotpCode(context.Security.TwoFactorSecret, code.Trim()))
            {
                throw new InvalidOperationException("TOTP 验证码无效。");
            }

            return;
        }

        var purpose = method == TwoFactorMethod.Email
            ? ProfileVerificationPurpose.TwoFactorEmail
            : ProfileVerificationPurpose.TwoFactorPhone;
        _ = ConsumeCode(context.User.BasicId, purpose, code);
    }

    /// <inheritdoc />
    public async Task<ProfileVerificationCodeResultDto> SendCodeAsync(
        SysUser user,
        ProfileVerificationPurpose purpose,
        string? target,
        string title,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(target))
        {
            throw new InvalidOperationException("当前账号缺少可接收验证码的联系方式。");
        }

        var code = GenerateVerificationCode();
        VerificationCodes[BuildVerificationKey(user.BasicId, purpose)] = new VerificationCodeState(
            code,
            target.Trim(),
            DateTimeOffset.UtcNow.AddSeconds(VerificationCodeSeconds));

        await _notificationDispatchService.DispatchToUserAsync(
            user.BasicId,
            $"{title}验证码",
            $"验证码：{code}，10 分钟内有效。",
            NotificationType.User,
            VerificationCodeBusinessType,
            user.BasicId,
            link: "/workbench/profile",
            icon: "lucide:shield-check",
            cancellationToken: cancellationToken);

        return new ProfileVerificationCodeResultDto { ExpiresInSeconds = VerificationCodeSeconds };
    }

    private static string BuildVerificationKey(long userId, ProfileVerificationPurpose purpose)
    {
        return $"{userId}:{purpose}";
    }

    private static string GenerateVerificationCode()
    {
        return RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
    }

    private sealed record VerificationCodeState(string Code, string PendingValue, DateTimeOffset ExpiresAt);
}
