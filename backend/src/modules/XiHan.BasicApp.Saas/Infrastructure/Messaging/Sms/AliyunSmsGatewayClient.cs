#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:AliyunSmsGatewayClient
// Guid:e2c8a514-7b60-4d93-9f27-3a5c1e8d0b74
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/02 15:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using AlibabaCloud.SDK.Dysmsapi20170525.Models;
using System.Text.Json;
using XiHan.BasicApp.Saas.Application.Services;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Infrastructure.Messaging;

/// <summary>
/// 阿里云短信网关客户端
/// </summary>
/// <remarks>
/// 端点固定 dysmsapi.aliyuncs.com；模板参数为命名 JSON（参数名须与阿里云控制台模板变量名一致）；
/// 多手机号逗号拼接单次提交；成功判定 Body.Code == "OK"，回执为 BizId。
/// </remarks>
public sealed class AliyunSmsGatewayClient : SmsGatewayClientBase
{
    private const string Endpoint = "dysmsapi.aliyuncs.com";

    private readonly AlibabaCloud.SDK.Dysmsapi20170525.Client _client;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="accessKeyId">AccessKeyId</param>
    /// <param name="accessKeySecret">AccessKeySecret（已解密明文）</param>
    /// <param name="signName">短信签名</param>
    /// <param name="templateMapJson">模板映射 JSON</param>
    public AliyunSmsGatewayClient(string accessKeyId, string accessKeySecret, string signName, string? templateMapJson)
        : base(signName, templateMapJson)
    {
        _client = new AlibabaCloud.SDK.Dysmsapi20170525.Client(new AlibabaCloud.OpenApiClient.Models.Config
        {
            AccessKeyId = accessKeyId,
            AccessKeySecret = accessKeySecret,
            Endpoint = Endpoint
        });
    }

    /// <inheritdoc />
    public override SmsProviderType Provider => SmsProviderType.Aliyun;

    /// <inheritdoc />
    public override async Task<SmsGatewaySendResult> SendAsync(SmsGatewayRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        var mapping = ResolveMapping(request.TemplateCode);
        var templateParams = ParseParams(request.TemplateParamsJson);

        var sendRequest = new SendSmsRequest
        {
            PhoneNumbers = string.Join(',', request.PhoneNumbers),
            SignName = SignName,
            TemplateCode = mapping.TemplateCode,
            TemplateParam = templateParams.Count > 0 ? JsonSerializer.Serialize(templateParams) : null
        };

        var response = await _client.SendSmsAsync(sendRequest);
        var body = response?.Body;
        return string.Equals(body?.Code, "OK", StringComparison.OrdinalIgnoreCase)
            ? new SmsGatewaySendResult(true, body?.BizId, null)
            : new SmsGatewaySendResult(false, null, $"阿里云短信发送失败：{body?.Code}-{body?.Message}");
    }
}
