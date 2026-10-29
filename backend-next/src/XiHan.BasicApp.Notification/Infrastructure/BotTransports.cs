// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Globalization;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using XiHan.BasicApp.Notification.Domain.Entities;

namespace XiHan.BasicApp.Notification.Infrastructure;

/// <summary>
/// 一个发送目标（密钥已解密）。
/// </summary>
/// <param name="Channel">通道。</param>
/// <param name="WebhookUrl">完整的 Webhook 地址（Telegram 为 Bot API 的根地址）。</param>
/// <param name="Secret">签名密钥（Telegram 为机器人令牌）；为空即不签名。</param>
/// <param name="Keyword">安全关键词；为空即不加。</param>
/// <param name="ChatId">目标会话（只有 Telegram 用）。</param>
public sealed record BotEndpoint(BotChannel Channel, string WebhookUrl, string? Secret, string? Keyword, string? ChatId = null);

/// <summary>
/// 机器人的发送通道（每个通道一个实现；测试换成假的）。都发纯文本，安全关键词放在第一行。
/// </summary>
public interface IBotTransport
{
    /// <summary>
    /// 通道。
    /// </summary>
    BotChannel Channel { get; }

    /// <summary>
    /// 发一条纯文本消息。
    /// </summary>
    /// <param name="endpoint">机器人。</param>
    /// <param name="text">正文。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>结果。</returns>
    Task<TransportResult> SendAsync(BotEndpoint endpoint, string text, CancellationToken cancellationToken);
}

/// <summary>
/// 三家群机器人的共同做法：按服务商拼地址与载荷、POST JSON、读返回的错误码。
/// </summary>
/// <param name="http">HTTP 客户端工厂。</param>
public abstract class WebhookBotTransport(IHttpClientFactory http) : IBotTransport
{
    /// <inheritdoc />
    public abstract BotChannel Channel { get; }

    /// <inheritdoc />
    public async Task<TransportResult> SendAsync(BotEndpoint endpoint, string text, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        var content = string.IsNullOrWhiteSpace(endpoint.Keyword) ? text : endpoint.Keyword + "\n" + text;
        var (uri, payload) = Build(endpoint, content);
        try
        {
            using var client = http.CreateClient(GetType().Name);
            using var response = await client.PostAsJsonAsync(uri, payload, NotificationJsonContext.Default.JsonObject, cancellationToken).ConfigureAwait(false);
            var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                return TransportResult.Failed($"HTTP {(int)response.StatusCode}: {body}");
            }

            using var json = JsonDocument.Parse(body);
            return Read(json.RootElement) is { } error ? TransportResult.Failed(error) : TransportResult.Sent(null);
        }
#pragma warning disable CA1031 // 网络错误与服务商拒绝记进发件箱
        catch (Exception exception) when (exception is not OperationCanceledException)
#pragma warning restore CA1031
        {
            return TransportResult.Failed(exception.Message);
        }
    }

    /// <summary>
    /// 拼出请求地址与载荷。
    /// </summary>
    /// <param name="endpoint">机器人。</param>
    /// <param name="content">带关键词的正文。</param>
    /// <returns>地址与载荷。</returns>
    protected abstract (Uri Uri, JsonObject Payload) Build(BotEndpoint endpoint, string content);

    /// <summary>
    /// 读返回体：成功为 <see langword="null"/>，否则是错误说明。
    /// </summary>
    /// <param name="body">返回体。</param>
    /// <returns>错误说明。</returns>
    protected virtual string? Read(JsonElement body)
    {
        var code = body.TryGetProperty("errcode", out var c) && c.ValueKind == JsonValueKind.Number ? c.GetInt32() : 0;
        return code == 0 ? null : $"{code}: {(body.TryGetProperty("errmsg", out var m) ? m.GetString() : body.GetRawText())}";
    }

    /// <summary>
    /// 纯文本载荷（钉钉与企业微信同形）。
    /// </summary>
    /// <param name="content">正文。</param>
    /// <returns>载荷。</returns>
    protected static JsonObject TextPayload(string content) => new() { ["msgtype"] = "text", ["text"] = new JsonObject { ["content"] = content } };
}

/// <summary>
/// 钉钉自定义机器人：有签名密钥时在地址后加 <c>timestamp</c>（毫秒）与 <c>sign</c>（以密钥对「时间戳\n密钥」做 HMAC-SHA256，Base64 后百分号编码）。
/// </summary>
/// <param name="http">HTTP 客户端工厂。</param>
/// <param name="clock">时钟。</param>
public sealed class DingTalkBotTransport(IHttpClientFactory http, TimeProvider clock) : WebhookBotTransport(http)
{
    /// <inheritdoc />
    public override BotChannel Channel => BotChannel.DingTalk;

    /// <inheritdoc />
    protected override (Uri Uri, JsonObject Payload) Build(BotEndpoint endpoint, string content)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        var url = endpoint.WebhookUrl;
        if (!string.IsNullOrEmpty(endpoint.Secret))
        {
            var timestamp = clock.GetUtcNow().ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture);
            var sign = Convert.ToBase64String(HMACSHA256.HashData(Encoding.UTF8.GetBytes(endpoint.Secret), Encoding.UTF8.GetBytes(timestamp + "\n" + endpoint.Secret)));
            url += (url.Contains('?', StringComparison.Ordinal) ? "&" : "?") + $"timestamp={timestamp}&sign={Uri.EscapeDataString(sign)}";
        }

        return (new Uri(url), TextPayload(content));
    }
}

/// <summary>
/// 飞书自定义机器人：有签名密钥时载荷里带 <c>timestamp</c>（秒）与 <c>sign</c>（以「时间戳\n密钥」为密钥对空串做 HMAC-SHA256，Base64）；返回 <c>code</c> 为 0 即成功。
/// </summary>
/// <param name="http">HTTP 客户端工厂。</param>
/// <param name="clock">时钟。</param>
public sealed class LarkBotTransport(IHttpClientFactory http, TimeProvider clock) : WebhookBotTransport(http)
{
    /// <inheritdoc />
    public override BotChannel Channel => BotChannel.Lark;

    /// <inheritdoc />
    protected override (Uri Uri, JsonObject Payload) Build(BotEndpoint endpoint, string content)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        var payload = new JsonObject { ["msg_type"] = "text", ["content"] = new JsonObject { ["text"] = content } };
        if (!string.IsNullOrEmpty(endpoint.Secret))
        {
            var timestamp = clock.GetUtcNow().ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture);
            payload["timestamp"] = timestamp;
            payload["sign"] = Convert.ToBase64String(HMACSHA256.HashData(Encoding.UTF8.GetBytes(timestamp + "\n" + endpoint.Secret), Array.Empty<byte>()));
        }

        return (new Uri(endpoint.WebhookUrl), payload);
    }

    /// <inheritdoc />
    protected override string? Read(JsonElement body)
    {
        // 新版返回 code / msg，旧版返回 StatusCode / StatusMessage
        var code = body.TryGetProperty("code", out var c) && c.ValueKind == JsonValueKind.Number ? c.GetInt32()
            : body.TryGetProperty("StatusCode", out var s) && s.ValueKind == JsonValueKind.Number ? s.GetInt32() : 0;
        return code == 0 ? null : $"{code}: {(body.TryGetProperty("msg", out var m) ? m.GetString() : body.GetRawText())}";
    }
}

/// <summary>
/// Telegram 机器人：<c>POST {根地址}/bot{令牌}/sendMessage</c>，发到目标会话；返回 <c>ok</c> 为真即成功，消息 ID 记进发件箱。
/// </summary>
/// <param name="http">HTTP 客户端工厂。</param>
public sealed class TelegramBotTransport(IHttpClientFactory http) : IBotTransport
{
    /// <inheritdoc />
    public BotChannel Channel => BotChannel.Telegram;

    /// <inheritdoc />
    public async Task<TransportResult> SendAsync(BotEndpoint endpoint, string text, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        if (string.IsNullOrEmpty(endpoint.Secret) || string.IsNullOrEmpty(endpoint.ChatId))
        {
            return TransportResult.Failed("The Telegram bot token or chat ID is missing.");
        }

        var uri = new Uri($"{endpoint.WebhookUrl.TrimEnd('/')}/bot{endpoint.Secret}/sendMessage");
        var payload = new JsonObject { ["chat_id"] = endpoint.ChatId, ["text"] = text };
        try
        {
            using var client = http.CreateClient(nameof(TelegramBotTransport));
            using var response = await client.PostAsJsonAsync(uri, payload, NotificationJsonContext.Default.JsonObject, cancellationToken).ConfigureAwait(false);
            var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            using var json = JsonDocument.Parse(body);
            var root = json.RootElement;
            if (root.TryGetProperty("ok", out var ok) && ok.ValueKind == JsonValueKind.True)
            {
                var messageId = root.TryGetProperty("result", out var result) && result.TryGetProperty("message_id", out var id) ? id.GetRawText() : null;
                return TransportResult.Sent(messageId);
            }

            var code = root.TryGetProperty("error_code", out var c) ? c.GetRawText() : ((int)response.StatusCode).ToString(CultureInfo.InvariantCulture);
            return TransportResult.Failed($"{code}: {(root.TryGetProperty("description", out var d) ? d.GetString() : body)}");
        }
#pragma warning disable CA1031 // 网络错误与服务商拒绝记进发件箱（异常消息不含令牌：令牌只在地址路径里）
        catch (Exception exception) when (exception is not OperationCanceledException)
#pragma warning restore CA1031
        {
            return TransportResult.Failed(exception.GetType().Name + ": " + exception.Message.Replace(endpoint.Secret, "***", StringComparison.Ordinal));
        }
    }
}

/// <summary>
/// 企业微信群机器人：地址里已带 <c>key</c>，没有签名。
/// </summary>
/// <param name="http">HTTP 客户端工厂。</param>
public sealed class WeComBotTransport(IHttpClientFactory http) : WebhookBotTransport(http)
{
    /// <inheritdoc />
    public override BotChannel Channel => BotChannel.WeCom;

    /// <inheritdoc />
    protected override (Uri Uri, JsonObject Payload) Build(BotEndpoint endpoint, string content)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        return (new Uri(endpoint.WebhookUrl), TextPayload(content));
    }
}
