// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using XiHan.BasicApp.Notification.Domain.Entities;

namespace XiHan.BasicApp.Notification.Infrastructure;

/// <summary>
/// 阿里云短信（Dysmsapi 2017-05-25 的 <c>SendSms</c>，RPC 风格签名 HMAC-SHA1，不引 SDK）：模板参数整体作为 JSON 对象传给服务商。
/// </summary>
/// <param name="http">HTTP 客户端工厂。</param>
/// <param name="clock">时钟。</param>
public sealed class AliyunSmsTransport(IHttpClientFactory http, TimeProvider clock) : ISmsTransport
{
    private const string Endpoint = "https://dysmsapi.aliyuncs.com/";

    /// <inheritdoc />
    public SmsProvider Provider => SmsProvider.Aliyun;

    /// <inheritdoc />
    public async Task<TransportResult> SendAsync(SmsAccount account, SmsEnvelope envelope, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(account);
        ArgumentNullException.ThrowIfNull(envelope);
        var templateParam = new JsonObject();
        foreach (var (name, value) in envelope.Parameters)
        {
            templateParam[name] = value;
        }

        var query = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["AccessKeyId"] = account.AccessKeyId,
            ["Action"] = "SendSms",
            ["Format"] = "JSON",
            ["PhoneNumbers"] = envelope.Phone,
            ["RegionId"] = string.IsNullOrWhiteSpace(account.Region) ? "cn-hangzhou" : account.Region,
            ["SignName"] = account.SignName,
            ["SignatureMethod"] = "HMAC-SHA1",
            ["SignatureNonce"] = Guid.NewGuid().ToString("N"),
            ["SignatureVersion"] = "1.0",
            ["TemplateCode"] = envelope.ProviderTemplateId,
            ["TemplateParam"] = templateParam.ToJsonString(),
            ["Timestamp"] = clock.GetUtcNow().ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture),
            ["Version"] = "2017-05-25",
        };
        var canonical = string.Join('&', query.Select(static p => $"{Encode(p.Key)}={Encode(p.Value)}"));
        var toSign = "GET&%2F&" + Encode(canonical);
#pragma warning disable CA5350 // 阿里云 RPC 签名规定用 HMAC-SHA1
        var signature = Convert.ToBase64String(HMACSHA1.HashData(Encoding.UTF8.GetBytes(account.AccessKeySecret + "&"), Encoding.UTF8.GetBytes(toSign)));
#pragma warning restore CA5350
        var uri = new Uri($"{Endpoint}?Signature={Encode(signature)}&{canonical}");
        try
        {
            using var client = http.CreateClient(nameof(AliyunSmsTransport));
            using var response = await client.GetAsync(uri, cancellationToken).ConfigureAwait(false);
            var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            using var json = JsonDocument.Parse(body);
            var code = json.RootElement.TryGetProperty("Code", out var c) ? c.GetString() : null;
            return string.Equals(code, "OK", StringComparison.OrdinalIgnoreCase)
                ? TransportResult.Sent(json.RootElement.TryGetProperty("BizId", out var biz) ? biz.GetString() : null)
                : TransportResult.Failed($"{code}: {(json.RootElement.TryGetProperty("Message", out var m) ? m.GetString() : body)}");
        }
#pragma warning disable CA1031 // 网络错误与服务商拒绝记进发送记录
        catch (Exception exception) when (exception is not OperationCanceledException)
#pragma warning restore CA1031
        {
            return TransportResult.Failed(exception.Message);
        }
    }

    /// <summary>
    /// 阿里云要求的百分号编码：RFC 3986，空格编码成 <c>%20</c>、星号 <c>%2A</c>，波浪号不编码。
    /// </summary>
    private static string Encode(string value) => Uri.EscapeDataString(value).Replace("+", "%20", StringComparison.Ordinal).Replace("*", "%2A", StringComparison.Ordinal).Replace("%7E", "~", StringComparison.Ordinal);
}

/// <summary>
/// 腾讯云短信（SMS 2021-01-11 的 <c>SendSms</c>，TC3-HMAC-SHA256 签名，不引 SDK）：模板参数按顺序传（腾讯云只认顺序）；手机号不带国家码时按 +86 补上。
/// </summary>
/// <param name="http">HTTP 客户端工厂。</param>
/// <param name="clock">时钟。</param>
public sealed class TencentSmsTransport(IHttpClientFactory http, TimeProvider clock) : ISmsTransport
{
    private const string Host = "sms.tencentcloudapi.com";
    private const string Service = "sms";
    private const string ContentType = "application/json; charset=utf-8";

    /// <inheritdoc />
    public SmsProvider Provider => SmsProvider.Tencent;

    /// <inheritdoc />
    public async Task<TransportResult> SendAsync(SmsAccount account, SmsEnvelope envelope, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(account);
        ArgumentNullException.ThrowIfNull(envelope);
        var phone = envelope.Phone.StartsWith('+') ? envelope.Phone : "+86" + envelope.Phone;
        var payload = new JsonObject
        {
            ["PhoneNumberSet"] = new JsonArray(phone),
            ["SmsSdkAppId"] = account.SdkAppId ?? string.Empty,
            ["SignName"] = account.SignName,
            ["TemplateId"] = envelope.ProviderTemplateId,
            ["TemplateParamSet"] = new JsonArray([.. envelope.Parameters.Select(static p => (JsonNode?)JsonValue.Create(p.Value ?? string.Empty))]),
        }.ToJsonString();

        var now = clock.GetUtcNow();
        var timestamp = now.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture);
        var date = now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var canonicalRequest = $"POST\n/\n\ncontent-type:{ContentType}\nhost:{Host}\n\ncontent-type;host\n{Hex(SHA256.HashData(Encoding.UTF8.GetBytes(payload)))}";
        var scope = $"{date}/{Service}/tc3_request";
        var toSign = $"TC3-HMAC-SHA256\n{timestamp}\n{scope}\n{Hex(SHA256.HashData(Encoding.UTF8.GetBytes(canonicalRequest)))}";
        var secretDate = HMACSHA256.HashData(Encoding.UTF8.GetBytes("TC3" + account.AccessKeySecret), Encoding.UTF8.GetBytes(date));
        var secretService = HMACSHA256.HashData(secretDate, Encoding.UTF8.GetBytes(Service));
        var secretSigning = HMACSHA256.HashData(secretService, Encoding.UTF8.GetBytes("tc3_request"));
        var signature = Hex(HMACSHA256.HashData(secretSigning, Encoding.UTF8.GetBytes(toSign)));

        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri($"https://{Host}/"))
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json"),
        };
        request.Content.Headers.ContentType = System.Net.Http.Headers.MediaTypeHeaderValue.Parse(ContentType);
        request.Headers.TryAddWithoutValidation("Authorization", $"TC3-HMAC-SHA256 Credential={account.AccessKeyId}/{scope}, SignedHeaders=content-type;host, Signature={signature}");
        request.Headers.Add("X-TC-Action", "SendSms");
        request.Headers.Add("X-TC-Timestamp", timestamp);
        request.Headers.Add("X-TC-Version", "2021-01-11");
        request.Headers.Add("X-TC-Region", string.IsNullOrWhiteSpace(account.Region) ? "ap-guangzhou" : account.Region);
        try
        {
            using var client = http.CreateClient(nameof(TencentSmsTransport));
            using var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
            var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            using var json = JsonDocument.Parse(body);
            var root = json.RootElement.GetProperty("Response");
            if (root.TryGetProperty("Error", out var error))
            {
                return TransportResult.Failed($"{error.GetProperty("Code").GetString()}: {error.GetProperty("Message").GetString()}");
            }

            var status = root.GetProperty("SendStatusSet")[0];
            return string.Equals(status.GetProperty("Code").GetString(), "Ok", StringComparison.OrdinalIgnoreCase)
                ? TransportResult.Sent(status.GetProperty("SerialNo").GetString())
                : TransportResult.Failed($"{status.GetProperty("Code").GetString()}: {status.GetProperty("Message").GetString()}");
        }
#pragma warning disable CA1031 // 网络错误与服务商拒绝记进发送记录
        catch (Exception exception) when (exception is not OperationCanceledException)
#pragma warning restore CA1031
        {
            return TransportResult.Failed(exception.Message);
        }
    }

    private static string Hex(byte[] bytes) => Convert.ToHexStringLower(bytes);
}
