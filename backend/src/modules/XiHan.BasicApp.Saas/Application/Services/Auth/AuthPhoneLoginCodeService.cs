// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text.Json;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Messaging;
using XiHan.Framework.Authentication.OneTimeCode;
using XiHan.Framework.Bot.Email.Abstractions;
using XiHan.Framework.Bot.Sms.Abstractions;

namespace XiHan.BasicApp.Saas.Application.Services;

/// <summary>
/// 手机号验证码登录的验证码下发与校验服务实现
/// </summary>
/// <remarks>
/// 委托框架 <see cref="IOneTimeCodeService"/>（加密安全 RNG，消费即销毁，错一次即作废在途码）。
/// 短信用登录验证码模板（<see cref="SaasMessageTemplateCodes.Auth.SmsLoginCode"/>），模板优先、纯文本兜底，由发件箱异步发送；
/// 发码前先确认短信网关已配置且启用（平台上下文即平台默认网关），否则直接报错。
/// </remarks>
public sealed class AuthPhoneLoginCodeService : IAuthPhoneLoginCodeService
{
    private const string Purpose = "auth:phone-login";
    private const string SmsLoginBusinessType = "auth.sms-login";
    private const string DefaultBrand = "XiHan BasicApp";

    private readonly IOneTimeCodeService _oneTimeCodeService;
    private readonly ISmsConfigStore _smsConfigStore;
    private readonly IEmailConfigStore _emailConfigStore;
    private readonly IMessageDeliveryService _messageDeliveryService;

    /// <summary>
    /// 构造函数
    /// </summary>
    public AuthPhoneLoginCodeService(
        IOneTimeCodeService oneTimeCodeService,
        ISmsConfigStore smsConfigStore,
        IEmailConfigStore emailConfigStore,
        IMessageDeliveryService messageDeliveryService)
    {
        _oneTimeCodeService = oneTimeCodeService;
        _smsConfigStore = smsConfigStore;
        _emailConfigStore = emailConfigStore;
        _messageDeliveryService = messageDeliveryService;
    }

    /// <summary>
    /// 验证码有效期（秒）
    /// </summary>
    public int ExpiresInSeconds => 600;

    /// <summary>
    /// 为手机号生成登录验证码并经短信发送
    /// </summary>
    /// <param name="phone">手机号</param>
    /// <param name="receiveUserId">接收账号（发件箱记录归属）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>生成的验证码</returns>
    public async Task<string> IssueAndSendAsync(string phone, long receiveUserId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(phone);
        var trimmedPhone = phone.Trim();

        var smsConfig = await _smsConfigStore.GetAsync(cancellationToken);
        if (smsConfig is not { IsEnabled: true })
        {
            throw new InvalidOperationException("短信网关未配置或未启用，暂不能用手机号登录。");
        }

        // 云厂商只按服务商模板发送，缺映射时发件箱必然失败：发码前拦下，不让接口先报「已发送」
        if (!HasTemplateMapping(smsConfig.TemplateMap, SaasMessageTemplateCodes.Auth.SmsLoginCode))
        {
            throw new InvalidOperationException(
                $"短信网关的模板映射里没有登录验证码模板 [{SaasMessageTemplateCodes.Auth.SmsLoginCode}]，暂不能用手机号登录；请在短信配置的模板映射里补上对应的服务商模板。");
        }

        var issued = await _oneTimeCodeService.IssueAsync(
            Purpose,
            BuildTarget(trimmedPhone),
            payload: null,
            new OneTimeCodeOptions { CodeLength = 6, ExpiresInSeconds = ExpiresInSeconds },
            cancellationToken);

        var emailConfig = await _emailConfigStore.GetAsync(cancellationToken);
        var fromName = emailConfig?.From.FromName;
        var brand = string.IsNullOrWhiteSpace(fromName) ? DefaultBrand : fromName;
        var minutes = Math.Max(1, ExpiresInSeconds / 60);

        await _messageDeliveryService.CreateSmsAsync(
            new SmsCreateCommand(
                SenderId: null,
                ReceiverId: receiveUserId,
                SmsType: SmsType.VerificationCode,
                ToPhone: trimmedPhone,
                Content: $"【{brand}】您的登录验证码为 {issued.Code}，{minutes} 分钟内有效，请勿泄露。",
                TemplateCode: SaasMessageTemplateCodes.Auth.SmsLoginCode,
                TemplateParams: JsonSerializer.Serialize(new Dictionary<string, string>
                {
                    ["code"] = issued.Code,
                    ["minutes"] = minutes.ToString(),
                    ["brand"] = brand,
                }),
                Provider: null,
                ScheduledTime: null,
                MaxRetryCount: 3,
                BusinessType: SmsLoginBusinessType,
                BusinessId: receiveUserId,
                Remark: null),
            cancellationToken);

        return issued.Code;
    }

    /// <summary>
    /// 校验并消费手机号的登录验证码（一次性，消费即销毁）
    /// </summary>
    /// <param name="phone">手机号</param>
    /// <param name="code">待校验验证码</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>校验是否通过</returns>
    public async Task<bool> TryConsumeAsync(string phone, string? code, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(phone) || string.IsNullOrWhiteSpace(code))
        {
            return false;
        }

        var result = await _oneTimeCodeService.TryConsumeAsync(Purpose, BuildTarget(phone.Trim()), code, cancellationToken);
        return result.Succeeded;
    }

    private static string BuildTarget(string phone)
    {
        return $"platform:{phone}";
    }

    /// <summary>
    /// 模板映射（形如 {"内部模板码":{"templateCode":"服务商模板码"}}）里是否有该内部模板码且服务商模板码非空；键不区分大小写，与网关客户端一致
    /// </summary>
    private static bool HasTemplateMapping(string? templateMapJson, string internalTemplateCode)
    {
        if (string.IsNullOrWhiteSpace(templateMapJson))
        {
            return false;
        }

        using var document = JsonDocument.Parse(templateMapJson);
        if (document.RootElement.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        foreach (var entry in document.RootElement.EnumerateObject())
        {
            if (!string.Equals(entry.Name.Trim(), internalTemplateCode, StringComparison.OrdinalIgnoreCase)
                || entry.Value.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            foreach (var field in entry.Value.EnumerateObject())
            {
                if (string.Equals(field.Name, "templateCode", StringComparison.OrdinalIgnoreCase)
                    && field.Value.ValueKind == JsonValueKind.String
                    && !string.IsNullOrWhiteSpace(field.Value.GetString()))
                {
                    return true;
                }
            }
        }

        return false;
    }
}
