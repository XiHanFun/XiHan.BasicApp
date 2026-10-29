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
/// <param name="WebhookUrl">完整的 Webhook 地址（Telegram 不用：根地址在平台设置里）。</param>
/// <param name="Secret">签名密钥（Telegram 为机器人令牌）；为空即不签名。</param>
/// <param name="Keyword">安全关键词；为空即不加。</param>
/// <param name="ChatId">目标会话（只有 Telegram 用）。</param>
/// <param name="BotId">Telegram 机器人 ID（出站审计用）。</param>
/// <param name="BotName">Telegram 机器人名称（出站审计用）。</param>
public sealed record BotEndpoint(BotChannel Channel, string WebhookUrl, string? Secret, string? Keyword, string? ChatId = null, Guid? BotId = null, string? BotName = null);

/// <summary>
/// 机器人的发送通道。
/// </summary>
/// <remarks>
/// 每个通道一个实现；测试换成假的。都发纯文本，安全关键词放在第一行。
/// </remarks>
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
/// 三家群机器人的共同做法。
/// </summary>
/// <remarks>
/// 按服务商拼地址与载荷、POST JSON、读返回的错误码。
/// </remarks>
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
/// 钉钉自定义机器人。
/// </summary>
/// <remarks>
/// 有签名密钥时在地址后加 <c>timestamp</c>（毫秒）与 <c>sign</c>（以密钥对「时间戳\n密钥」做 HMAC-SHA256，Base64 后百分号编码）。
/// </remarks>
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
/// 飞书自定义机器人。
/// </summary>
/// <remarks>
/// 有签名密钥时载荷里带 <c>timestamp</c>（秒）与 <c>sign</c>（以「时间戳\n密钥」为密钥对空串做 HMAC-SHA256，Base64）；返回 <c>code</c> 为 0 即成功。
/// </remarks>
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
/// Telegram 广播。
/// </summary>
/// <remarks>
/// 经 <see cref="Telegram.TelegramNotifier"/> 发 <c>sendMessage</c>（与交互用同一套网络设置、重试与出站审计）；返回的消息 ID 记进发件箱。
/// </remarks>
/// <param name="notifier">Telegram 出站。</param>
public sealed class TelegramBotTransport(Telegram.TelegramNotifier notifier) : IBotTransport
{
    /// <inheritdoc />
    public BotChannel Channel => BotChannel.Telegram;

    /// <inheritdoc />
    public async Task<TransportResult> SendAsync(BotEndpoint endpoint, string text, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        if (string.IsNullOrEmpty(endpoint.Secret) || !long.TryParse(endpoint.ChatId, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var chatId))
        {
            return TransportResult.Failed("The Telegram bot token or chat ID is missing.");
        }

        var bot = new Telegram.TelegramBot(endpoint.BotId ?? Guid.Empty, endpoint.BotName ?? "broadcast", endpoint.Secret, [], [], [], false);
        var result = await notifier.SendTextAsync(bot, chatId, text, cancellationToken: cancellationToken).ConfigureAwait(false);
        return result.Ok
            ? TransportResult.Sent(result.MessageId?.ToString(CultureInfo.InvariantCulture))
            : TransportResult.Failed($"{result.ErrorCode?.ToString(CultureInfo.InvariantCulture) ?? "network"}: {result.Description}");
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
