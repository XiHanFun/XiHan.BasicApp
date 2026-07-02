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

using System.Text.Json;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.QueryServices;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Messaging;
using XiHan.Framework.Authentication.OneTimeCode;
using XiHan.Framework.Authentication.Otp;
using XiHan.Framework.Bot.Email.Abstractions;
using XiHan.Framework.Bot.Email.Options;
using XiHan.Framework.Core.Exceptions;

namespace XiHan.BasicApp.Saas.Application.Services;

/// <summary>
/// 当前用户验证码应用服务实现
/// </summary>
/// <remarks>
/// 验证码签发与消费委托框架 <see cref="IOneTimeCodeService"/>（分布式缓存后端，加密安全 RNG，消费即销毁）：
/// 改绑场景的待生效联系方式以验证码负载（payload）随码暂存，消费成功后取回。
/// 发码与消费均经 <see cref="IVerificationThrottleService"/> 防刷（发送间隔/日配额/错误计数封禁）。
/// </remarks>
public sealed class ProfileVerificationService
    : IProfileVerificationService
{
    private const string VerificationCodeBusinessType = "profile.verification-code";
    private const string SmsLoginCodeBusinessType = "auth.sms-login";
    private const string Purpose = "profile:verification";
    private const int VerificationCodeSeconds = 600;

    /// <summary>
    /// 作废在途码占位串（框架消费即销毁：以必不匹配的占位码走一次消费即可清除在途码）
    /// </summary>
    private const string InFlightCodeVoidStamp = "!void!";

    private readonly IOneTimeCodeService _oneTimeCodeService;

    private readonly IMessageDeliveryService _messageDeliveryService;

    private readonly IOtpService _otpService;

    private readonly IEmailConfigStore _emailConfigStore;

    private readonly IVerificationThrottleService _verificationThrottleService;

    /// <summary>
    /// 构造函数
    /// </summary>
    public ProfileVerificationService(
        IOneTimeCodeService oneTimeCodeService,
        IOtpService otpService,
        IMessageDeliveryService messageDeliveryService,
        IEmailConfigStore emailConfigStore,
        IVerificationThrottleService verificationThrottleService)
    {
        _oneTimeCodeService = oneTimeCodeService;
        _otpService = otpService;
        _messageDeliveryService = messageDeliveryService;
        _emailConfigStore = emailConfigStore;
        _verificationThrottleService = verificationThrottleService;
    }

    /// <inheritdoc />
    public async Task<string> ConsumeCodeAsync(long userId, ProfileVerificationPurpose purpose, string? code, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        // 封禁期内拒绝校验
        await _verificationThrottleService.EnsureVerifyAllowedAsync(userId, purpose, cancellationToken);

        var result = await _oneTimeCodeService.TryConsumeAsync(Purpose, BuildTarget(userId, purpose), code, cancellationToken);
        if (!result.Succeeded || string.IsNullOrWhiteSpace(result.Payload))
        {
            var bannedNow = await _verificationThrottleService.OnVerifyFailedAsync(userId, purpose, cancellationToken);
            if (bannedNow)
            {
                // 触发封禁：作废在途码（防御性；消费即销毁语义下多数场景已随本次尝试销毁）
                _ = await _oneTimeCodeService.TryConsumeAsync(Purpose, BuildTarget(userId, purpose), InFlightCodeVoidStamp, cancellationToken);
                throw new UserFriendlyException("验证码错误次数过多，已临时锁定，请稍后再试。");
            }

            throw new InvalidOperationException("验证码无效或已过期。");
        }

        // 成功消费清零失败计数
        await _verificationThrottleService.OnVerifySucceededAsync(userId, purpose, cancellationToken);

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

        var trimmedTarget = target.Trim();

        // 防刷：封禁 → 发送间隔（60s）→ 日配额（target 10 次 / IP 50 次）依序检查，通过后记账
        await _verificationThrottleService.EnsureSendAllowedAsync(user.BasicId, purpose, trimmedTarget, cancellationToken);

        // 目标联系方式作为负载随码暂存，消费成功后取回（改绑场景即待生效的新邮箱/新手机号）
        var issued = await _oneTimeCodeService.IssueAsync(
            Purpose,
            BuildTarget(user.BasicId, purpose),
            payload: trimmedTarget,
            new OneTimeCodeOptions { CodeLength = 6, ExpiresInSeconds = VerificationCodeSeconds },
            cancellationToken);

        var emailConfig = await _emailConfigStore.GetAsync(cancellationToken);
        var brand = ResolveBrand(emailConfig);
        var minutes = Math.Max(1, VerificationCodeSeconds / 60);
        // 纯文本兜底内容（模板缺失/损坏时使用）
        var content = $"验证码：{issued.Code}，{minutes} 分钟内有效。";
        var templateParams = JsonSerializer.Serialize(new Dictionary<string, string>
        {
            ["code"] = issued.Code,
            ["minutes"] = minutes.ToString(),
            ["brand"] = brand,
            ["title"] = title,
        });

        // 按用途选择真实投递渠道（手机类 → 短信，邮箱类 → 邮件），模板优先、纯文本兜底；由发件箱异步发送，不再实时推送到前端
        if (IsPhonePurpose(purpose))
        {
            await _messageDeliveryService.CreateSmsAsync(
                new SmsCreateCommand(
                    SenderId: null,
                    ReceiverId: user.BasicId,
                    SmsType: SmsType.VerificationCode,
                    ToPhone: trimmedTarget,
                    Content: content,
                    TemplateCode: SaasMessageTemplateCodes.Auth.SmsVerificationCode,
                    TemplateParams: templateParams,
                    Provider: null,
                    ScheduledTime: null,
                    MaxRetryCount: 3,
                    BusinessType: VerificationCodeBusinessType,
                    BusinessId: user.BasicId,
                    Remark: null),
                cancellationToken);
        }
        else
        {
            await _messageDeliveryService.CreateEmailAsync(
                new EmailCreateCommand(
                    SendUserId: null,
                    ReceiveUserId: user.BasicId,
                    EmailType: EmailType.Verification,
                    FromEmail: emailConfig?.From.FromMail ?? string.Empty,
                    FromName: emailConfig?.From.FromName,
                    ToEmail: trimmedTarget,
                    CcEmail: null,
                    BccEmail: null,
                    Subject: $"{title}验证码",
                    Content: content,
                    // 模板（EmailVerificationCode）为 HTML；模板缺失时回退上方纯文本内容（HTML 邮件里纯文本可正常显示）
                    IsHtml: true,
                    Attachments: null,
                    TemplateCode: SaasMessageTemplateCodes.Auth.EmailVerificationCode,
                    TemplateParams: templateParams,
                    ScheduledTime: null,
                    MaxRetryCount: 3,
                    BusinessType: VerificationCodeBusinessType,
                    BusinessId: user.BasicId,
                    Remark: null),
                cancellationToken);
        }

        return new ProfileVerificationCodeResultDto { ExpiresInSeconds = VerificationCodeSeconds };
    }

    /// <inheritdoc />
    public async Task<ProfileVerificationCodeResultDto> SendLoginTwoFactorSmsAsync(
        SysUser user,
        string phone,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentException.ThrowIfNullOrWhiteSpace(phone);
        cancellationToken.ThrowIfCancellationRequested();

        var trimmedPhone = phone.Trim();

        // 防刷：与安全操作发码共用同一套限流（封禁 → 间隔 → 日配额）
        await _verificationThrottleService.EnsureSendAllowedAsync(user.BasicId, ProfileVerificationPurpose.TwoFactorPhone, trimmedPhone, cancellationToken);

        // 登录 2FA 与安全操作 2FA 共享 TwoFactorPhone 用途（同一在途码，重发覆盖旧码）；
        // 负载暂存手机号以满足消费侧非空校验，与改绑场景语义保持一致
        var issued = await _oneTimeCodeService.IssueAsync(
            Purpose,
            BuildTarget(user.BasicId, ProfileVerificationPurpose.TwoFactorPhone),
            payload: trimmedPhone,
            new OneTimeCodeOptions { CodeLength = 6, ExpiresInSeconds = VerificationCodeSeconds },
            cancellationToken);

        var emailConfig = await _emailConfigStore.GetAsync(cancellationToken);
        var brand = ResolveBrand(emailConfig);
        var minutes = Math.Max(1, VerificationCodeSeconds / 60);
        // 纯文本兜底内容（模板缺失/损坏时使用）
        var content = $"【{brand}】您的登录验证码为 {issued.Code}，{minutes} 分钟内有效，请勿泄露。";
        var templateParams = JsonSerializer.Serialize(new Dictionary<string, string>
        {
            ["code"] = issued.Code,
            ["minutes"] = minutes.ToString(),
            ["brand"] = brand,
        });

        // 使用登录验证码短信模板（SmsLoginCode），模板优先、纯文本兜底；由发件箱异步发送
        await _messageDeliveryService.CreateSmsAsync(
            new SmsCreateCommand(
                SenderId: null,
                ReceiverId: user.BasicId,
                SmsType: SmsType.VerificationCode,
                ToPhone: trimmedPhone,
                Content: content,
                TemplateCode: SaasMessageTemplateCodes.Auth.SmsLoginCode,
                TemplateParams: templateParams,
                Provider: null,
                ScheduledTime: null,
                MaxRetryCount: 3,
                BusinessType: SmsLoginCodeBusinessType,
                BusinessId: user.BasicId,
                Remark: null),
            cancellationToken);

        return new ProfileVerificationCodeResultDto { ExpiresInSeconds = VerificationCodeSeconds };
    }

    private static string BuildTarget(long userId, ProfileVerificationPurpose purpose)
    {
        return $"{userId}:{purpose}";
    }

    /// <summary>
    /// 解析品牌名（发件人显示名 FromName，未配置回退默认品牌）
    /// </summary>
    private static string ResolveBrand(EmailOptions? emailConfig)
    {
        var fromName = emailConfig?.From.FromName;
        return string.IsNullOrWhiteSpace(fromName) ? "XiHan BasicApp" : fromName;
    }

    /// <summary>
    /// 是否为手机类用途（决定短信渠道；其余按邮箱渠道）
    /// </summary>
    private static bool IsPhonePurpose(ProfileVerificationPurpose purpose)
    {
        return purpose is ProfileVerificationPurpose.VerifyPhone
            or ProfileVerificationPurpose.ChangePhone
            or ProfileVerificationPurpose.TwoFactorPhone;
    }
}
