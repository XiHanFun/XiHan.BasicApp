#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:TencentCloudSmsGatewayClient
// Guid:f5a1d738-2c96-4b04-8e5a-6d0b9f3c7e12
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/02 15:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using TencentCloud.Common;
using TencentCloud.Common.Profile;
using TencentCloud.Sms.V20210111;
using TencentCloud.Sms.V20210111.Models;
using XiHan.BasicApp.Saas.Application.Services;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Infrastructure.Messaging;

/// <summary>
/// 腾讯云短信网关客户端（V20210111 新版接口）
/// </summary>
/// <remarks>
/// 模板参数为位置数组，须在模板映射中提供 paramOrder 指定参数顺序；
/// 手机号自动补 +86 前缀（已带 + 的按 E.164 原样）；逐号回执 SendStatusSet，任一失败即整体失败。
/// </remarks>
public sealed class TencentCloudSmsGatewayClient : SmsGatewayClientBase
{
    private const string Endpoint = "sms.tencentcloudapi.com";

    private readonly SmsClient _client;
    private readonly string _sdkAppId;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="secretId">SecretId</param>
    /// <param name="secretKey">SecretKey（已解密明文）</param>
    /// <param name="sdkAppId">短信应用ID（SmsSdkAppId）</param>
    /// <param name="region">地域（如 ap-guangzhou）</param>
    /// <param name="signName">短信签名</param>
    /// <param name="templateMapJson">模板映射 JSON</param>
    public TencentCloudSmsGatewayClient(string secretId, string secretKey, string sdkAppId, string region, string signName, string? templateMapJson)
        : base(signName, templateMapJson)
    {
        _sdkAppId = sdkAppId;
        _client = new SmsClient(
            new Credential { SecretId = secretId, SecretKey = secretKey },
            region,
            new ClientProfile { HttpProfile = new HttpProfile { Endpoint = Endpoint } });
    }

    /// <inheritdoc />
    public override SmsProviderType Provider => SmsProviderType.TencentCloud;

    /// <inheritdoc />
    public override async Task<SmsGatewaySendResult> SendAsync(SmsGatewayRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        var mapping = ResolveMapping(request.TemplateCode);
        var templateParams = ParseParams(request.TemplateParamsJson);
        var paramSet = BuildParamSet(mapping, templateParams);

        var sendRequest = new SendSmsRequest
        {
            PhoneNumberSet = request.PhoneNumbers.Select(NormalizePhone).ToArray(),
            SmsSdkAppId = _sdkAppId,
            SignName = SignName,
            TemplateId = mapping.TemplateCode,
            TemplateParamSet = paramSet
        };

        var response = await _client.SendSms(sendRequest);
        var statuses = response?.SendStatusSet ?? [];
        if (statuses.Length == 0)
        {
            return new SmsGatewaySendResult(false, null, "腾讯云短信发送失败：无回执。");
        }

        // 逐号回执：任一失败即整体失败（发件箱按行重试）
        var failures = statuses
            .Where(status => !string.Equals(status.Code, "Ok", StringComparison.OrdinalIgnoreCase))
            .Select(status => $"{status.PhoneNumber}:{status.Code}-{status.Message}")
            .ToArray();
        if (failures.Length > 0)
        {
            return new SmsGatewaySendResult(false, null, $"腾讯云短信发送失败：{string.Join("；", failures)}");
        }

        var serialNos = string.Join(',', statuses.Select(status => status.SerialNo));
        return new SmsGatewaySendResult(true, serialNos, null);
    }

    private static string[] BuildParamSet(SmsTemplateMapping mapping, Dictionary<string, string> templateParams)
    {
        if (templateParams.Count == 0)
        {
            return [];
        }

        if (mapping.ParamOrder is not { Length: > 0 })
        {
            throw new InvalidOperationException($"腾讯云模板参数为位置数组，模板映射 [{mapping.TemplateCode}] 必须提供 paramOrder 指定参数顺序。");
        }

        return [.. mapping.ParamOrder.Select(key =>
            templateParams.TryGetValue(key, out var value)
                ? value
                : throw new InvalidOperationException($"短信模板参数缺少 paramOrder 声明的键 [{key}]。"))];
    }

    private static string NormalizePhone(string phone)
    {
        var trimmed = phone.Trim();
        return trimmed.StartsWith('+') ? trimmed : $"+86{trimmed}";
    }
}
