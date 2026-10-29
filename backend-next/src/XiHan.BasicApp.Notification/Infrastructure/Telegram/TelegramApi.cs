// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace XiHan.BasicApp.Notification.Infrastructure.Telegram;

/// <summary>
/// 按网络设置给出 HTTP 客户端（代理与超时）。
/// </summary>
/// <remarks>
/// 测试换成假的。
/// </remarks>
public interface ITelegramHttp
{
    /// <summary>
    /// 这套网络设置用的客户端（同一设置复用一个）。
    /// </summary>
    /// <param name="network">网络设置。</param>
    /// <returns>客户端。</returns>
    HttpClient ClientFor(TelegramNetwork network);
}

/// <summary>
/// <see cref="ITelegramHttp"/> 的缺省实现。
/// </summary>
/// <remarks>
/// 每套「代理 + 超时」一个长命客户端（不随请求释放，4.x 每个机器人实例一个）。
/// </remarks>
public sealed class TelegramHttp : ITelegramHttp, IDisposable
{
    private readonly ConcurrentDictionary<(string? Proxy, TimeSpan Timeout), HttpClient> _clients = new();

    /// <inheritdoc />
    public HttpClient ClientFor(TelegramNetwork network)
    {
        ArgumentNullException.ThrowIfNull(network);
        var proxy = string.IsNullOrWhiteSpace(network.ProxyUrl) ? null : network.ProxyUrl.Trim();
        return _clients.GetOrAdd((proxy, network.EffectiveTimeout), static key =>
        {
            var handler = new SocketsHttpHandler { PooledConnectionLifetime = TimeSpan.FromMinutes(5) };
            if (key.Proxy is not null && Uri.TryCreate(key.Proxy, UriKind.Absolute, out var proxyUri))
            {
                var web = new WebProxy(new Uri(proxyUri.GetLeftPart(UriPartial.Authority)));
                if (!string.IsNullOrEmpty(proxyUri.UserInfo))
                {
                    var parts = proxyUri.UserInfo.Split(':', 2);
                    web.Credentials = new NetworkCredential(Uri.UnescapeDataString(parts[0]), parts.Length > 1 ? Uri.UnescapeDataString(parts[1]) : string.Empty);
                }

                handler.Proxy = web;
                handler.UseProxy = true;
            }

            return new HttpClient(handler) { Timeout = key.Timeout };
        });
    }

    /// <inheritdoc />
    public void Dispose()
    {
        foreach (var client in _clients.Values)
        {
            client.Dispose();
        }

        _clients.Clear();
    }
}

/// <summary>
/// 一次 Bot API 调用的结果。
/// </summary>
/// <param name="Ok">是否成功。</param>
/// <param name="Result">成功时的 <c>result</c>（已复制，可在调用之后读）。</param>
/// <param name="ErrorCode">失败时 Bot API 的 <c>error_code</c>（网络错误为空）。</param>
/// <param name="Description">失败说明。</param>
/// <param name="RetryAfter">429 时的 <c>retry_after</c> 秒数。</param>
public sealed record TelegramApiResult(bool Ok, JsonElement Result, int? ErrorCode, string? Description, int? RetryAfter)
{
    /// <summary>
    /// 是否值得重试。
    /// </summary>
    /// <remarks>
    /// 429、5xx 与网络错误（4.x 同）。
    /// </remarks>
    public bool Transient => !Ok && (ErrorCode is null or 429 or >= 500);

    /// <summary>
    /// 成功结果里的 <c>message_id</c>。
    /// </summary>
    public long? MessageId => Ok && Result.ValueKind == JsonValueKind.Object && Result.TryGetProperty("message_id", out var id) && id.TryGetInt64(out var value) ? value : null;
}

/// <summary>
/// Bot API 的底层调用。
/// </summary>
/// <remarks>
/// <c>POST {根地址}/bot{令牌}/{方法}</c>，载荷是 JSON；读 <c>ok</c> / <c>result</c> / <c>error_code</c> / <c>description</c> / <c>parameters.retry_after</c>。
/// 不重试、不审计（由 <see cref="TelegramNotifier"/> 负责）；异常说明里抹掉令牌。
/// </remarks>
/// <param name="http">HTTP 客户端。</param>
public sealed class TelegramApi(ITelegramHttp http)
{
    /// <summary>
    /// 调一个方法。
    /// </summary>
    /// <param name="network">网络设置。</param>
    /// <param name="token">机器人令牌。</param>
    /// <param name="method">方法名。</param>
    /// <param name="payload">载荷。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>结果。</returns>
    public async Task<TelegramApiResult> CallAsync(TelegramNetwork network, string token, string method, JsonObject payload, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(network);
        ArgumentException.ThrowIfNullOrEmpty(token);
        ArgumentNullException.ThrowIfNull(payload);
        var uri = new Uri($"{network.EffectiveBaseUrl}/bot{token}/{method}");
        try
        {
            using var response = await http.ClientFor(network).PostAsJsonAsync(uri, payload, NotificationJsonContext.Default.JsonObject, cancellationToken).ConfigureAwait(false);
            var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            JsonDocument json;
            try
            {
                json = JsonDocument.Parse(body);
            }
            catch (JsonException)
            {
                return new TelegramApiResult(false, default, (int)response.StatusCode, Scrub(body, token), null);
            }

            using (json)
            {
                var root = json.RootElement;
                if (root.TryGetProperty("ok", out var ok) && ok.ValueKind == JsonValueKind.True)
                {
                    return new TelegramApiResult(true, root.TryGetProperty("result", out var result) ? result.Clone() : default, null, null, null);
                }

                var code = root.TryGetProperty("error_code", out var c) && c.TryGetInt32(out var value) ? value : (int)response.StatusCode;
                var description = root.TryGetProperty("description", out var d) ? d.GetString() : body;
                int? retryAfter = root.TryGetProperty("parameters", out var parameters) && parameters.TryGetProperty("retry_after", out var after) && after.TryGetInt32(out var seconds) ? seconds : null;
                return new TelegramApiResult(false, default, code, Scrub(description, token), retryAfter);
            }
        }
#pragma warning disable CA1031 // 网络错误作为可重试的失败交给调用方
        catch (Exception exception) when (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
#pragma warning restore CA1031
        {
            return new TelegramApiResult(false, default, null, exception.GetType().Name + ": " + Scrub(exception.Message, token), null);
        }
    }

    private static string? Scrub(string? text, string token) => text?.Replace(token, "***", StringComparison.Ordinal);
}
