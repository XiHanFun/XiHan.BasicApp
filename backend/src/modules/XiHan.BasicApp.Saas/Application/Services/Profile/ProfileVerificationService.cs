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

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.QueryServices;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.Framework.Authentication.OneTimeCode;
using XiHan.Framework.Authentication.Otp;

namespace XiHan.BasicApp.Saas.Application.Services;

/// <summary>
/// 当前用户验证码应用服务实现
/// </summary>
/// <remarks>
/// 验证码签发与消费委托框架 <see cref="IOneTimeCodeService"/>（分布式缓存后端，加密安全 RNG，消费即销毁）：
/// 改绑场景的待生效联系方式以验证码负载（payload）随码暂存，消费成功后取回。
/// </remarks>
public sealed class ProfileVerificationService
    : IProfileVerificationService
{
    private const string VerificationCodeBusinessType = "profile.verification-code";
    private const string Purpose = "profile:verification";
    private const int VerificationCodeSeconds = 600;

    private readonly IOneTimeCodeService _oneTimeCodeService;

    private readonly IUserNotificationDispatchService _notificationDispatchService;

    private readonly IOtpService _otpService;

    /// <summary>
    /// 构造函数
    /// </summary>
    public ProfileVerificationService(
        IOneTimeCodeService oneTimeCodeService,
        IOtpService otpService,
        IUserNotificationDispatchService notificationDispatchService)
    {
        _oneTimeCodeService = oneTimeCodeService;
        _otpService = otpService;
        _notificationDispatchService = notificationDispatchService;
    }

    /// <inheritdoc />
    public async Task<string> ConsumeCodeAsync(long userId, ProfileVerificationPurpose purpose, string? code, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        var result = await _oneTimeCodeService.TryConsumeAsync(Purpose, BuildTarget(userId, purpose), code, cancellationToken);
        if (!result.Succeeded || string.IsNullOrWhiteSpace(result.Payload))
        {
            throw new InvalidOperationException("验证码无效或已过期。");
        }

        return result.Payload;
    }

    /// <inheritdoc />
    public async Task EnsureTwoFactorCodeValidAsync(ProfileUserSecurityContext context, TwoFactorMethod method, string? code, CancellationToken cancellationToken = default)
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
        _ = await ConsumeCodeAsync(context.User.BasicId, purpose, code, cancellationToken);
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

        // 目标联系方式作为负载随码暂存，消费成功后取回（改绑场景即待生效的新邮箱/新手机号）
        var issued = await _oneTimeCodeService.IssueAsync(
            Purpose,
            BuildTarget(user.BasicId, purpose),
            payload: target.Trim(),
            new OneTimeCodeOptions { CodeLength = 6, ExpiresInSeconds = VerificationCodeSeconds },
            cancellationToken);

        await _notificationDispatchService.DispatchToUserAsync(
            user.BasicId,
            $"{title}验证码",
            $"验证码：{issued.Code}，10 分钟内有效。",
            NotificationType.User,
            VerificationCodeBusinessType,
            user.BasicId,
            link: "/workbench/profile",
            icon: "lucide:shield-check",
            cancellationToken: cancellationToken);

        return new ProfileVerificationCodeResultDto { ExpiresInSeconds = VerificationCodeSeconds };
    }

    private static string BuildTarget(long userId, ProfileVerificationPurpose purpose)
    {
        return $"{userId}:{purpose}";
    }
}
