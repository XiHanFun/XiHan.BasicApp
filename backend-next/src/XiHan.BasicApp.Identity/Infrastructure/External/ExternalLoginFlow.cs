// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Buffers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Contracts;
using XiHan.BasicApp.Identity.Domain.Services;
using XiHan.Framework.Caching.Abstractions.Caches;
using XiHan.Framework.Caching.Abstractions.Stores;
using XiHan.Framework.Hosting.AspNetCore.Endpoints;

namespace XiHan.BasicApp.Identity.Infrastructure.External;

/// <summary>
/// 一张第三方身份票据（回调后发给前端，五分钟内用一次）：登录用的只能换登录；绑定用的记着发起绑定的人，只能由他本人确认。
/// </summary>
/// <param name="Provider">提供商名称。</param>
/// <param name="ProviderType">提供商类型。</param>
/// <param name="Identity">第三方身份。</param>
/// <param name="Mode">登录或绑定。</param>
/// <param name="Tenant">登录的注册地（登录页选的；平台为 <c>platform</c>）。</param>
/// <param name="UserId">绑定：发起绑定的人（登录为空）。</param>
public sealed record ExternalTicket(string Provider, string ProviderType, ExternalIdentity Identity, string Mode, string Tenant, Guid? UserId);

/// <summary>
/// 第三方登录的流程（普通端点 <c>/api/oauth/{提供商}/challenge</c> 与 <c>/callback</c>，绕过内核管道、只碰缓存）：
/// 发起时生成 256 位随机 state 存进共享缓存，同时种一枚只给回调路径的 HttpOnly Cookie，回调时 state 与 Cookie 都对上才认（state 不能被别的浏览器拿来用）；
/// Google 另用 PKCE。回调换来身份后不发令牌，而是发一张一次性票据带去前端：登录由 <c>auth.external-login</c> 换（与密码登录同样过账号状态、锁定、登录守卫与双因素，4.x 第三方登录全跳过），
/// 绑定先由登录着的人经 <c>profile.begin-external-bind</c> 领一个绑定意图（十分钟、用一次）带去发起，票据记着他，只有他本人调 <c>profile.link-external</c> 才能确认：
/// 4.x 只认票据里记的人、不看是谁在确认，攻击者把自己发起的链接发给受害者即可把受害者的第三方账号绑到自己名下；反过来只认确认的人、不记发起人，攻击者把自己完成的回调地址发给受害者即可把自己的第三方账号绑到受害者名下——两头都要对上。
/// 第三方那边失败（用户拒绝、换码出错）一律带着错误码回到前端（4.x 直接 500）。
/// </summary>
public static partial class ExternalLoginFlow
{
    /// <summary>
    /// 路由前缀。
    /// </summary>
    public const string Prefix = "/api/oauth";

    /// <summary>
    /// 登录。
    /// </summary>
    public const string LoginMode = "login";

    /// <summary>
    /// 绑定。
    /// </summary>
    public const string BindMode = "bind";

    /// <summary>
    /// 绑定浏览器的 Cookie。
    /// </summary>
    public const string CookieName = "xihan.oauth";

    /// <summary>
    /// HttpClient 名称。
    /// </summary>
    public const string HttpClientName = "identity.external";

    private static readonly TimeSpan _stateLifetime = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan _ticketLifetime = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan _intentLifetime = TimeSpan.FromMinutes(10);

    /// <summary>
    /// 映射端点（宿主调用）。
    /// </summary>
    public static IEndpointRouteBuilder MapExternalLogin(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        PipelineBypassMetadata.BypassXiHanPipeline(endpoints.MapGet(Prefix + "/{provider}/challenge", ChallengeAsync).ExcludeFromDescription());
        PipelineBypassMetadata.BypassXiHanPipeline(endpoints.MapGet(Prefix + "/{provider}/callback", CallbackAsync).ExcludeFromDescription());
        return endpoints;
    }

    /// <summary>
    /// 取出并作废一张票据；不存在、过期或用途不符为空。
    /// </summary>
    public static async Task<ExternalTicket?> ConsumeTicketAsync(ISharedCache cache, string? ticket, string mode, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(cache);
        if (string.IsNullOrWhiteSpace(ticket))
        {
            return null;
        }

        var key = new CacheKey("identity:external:ticket:" + Hash(ticket));
        var stored = await cache.GetAsync<string>(key, cancellationToken).ConfigureAwait(false);
        if (!stored.TryGetValue(out var raw))
        {
            return null;
        }

        await cache.RemoveAsync(key, cancellationToken).ConfigureAwait(false);
        var parsed = DecodeTicket(raw);
        return parsed is not null && parsed.Mode == mode ? parsed : null;
    }

    /// <summary>
    /// 领一个绑定意图（已登录的人发起绑定之前），返回发起绑定的地址。
    /// </summary>
    public static async Task<string> BeginBindAsync(ISharedCache cache, ExternalProviderConfig config, Guid userId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(cache);
        ArgumentNullException.ThrowIfNull(config);
        var intent = RefreshTokens.Generate();
        await cache.SetAsync(new CacheKey("identity:external:intent:" + Hash(intent)), config.Name + "|" + userId.ToString("N"), CacheEntryOptions.Expires(_intentLifetime), cancellationToken).ConfigureAwait(false);
        return QueryHelpers.AddQueryString($"{Prefix}/{config.Name}/challenge", new Dictionary<string, string?> { ["mode"] = BindMode, ["intent"] = intent });
    }

    private static async Task<IResult> ChallengeAsync(HttpContext http, string provider, ExternalLoginOptions options, ExternalLoginProviders providers, ISharedCache cache)
    {
        var config = options.Find(provider);
        if (config is null || providers.Find(config.Type) is not { } handler)
        {
            return Results.Redirect(FrontendUrl(options, ("error", "invalid_provider"), ("provider", provider)));
        }

        var mode = string.Equals(http.Request.Query["mode"], BindMode, StringComparison.Ordinal) ? BindMode : LoginMode;
        var tenant = http.Request.Query["tenant"].ToString().Trim() is { Length: > 0 and <= 64 } requested ? requested : TenantIds.Platform;
        Guid? userId = null;
        if (mode == BindMode)
        {
            // 绑定须带着已登录的人领的意图（用一次）
            var intentKey = new CacheKey("identity:external:intent:" + Hash(http.Request.Query["intent"].ToString()));
            var intent = await cache.GetAsync<string>(intentKey, http.RequestAborted).ConfigureAwait(false);
            var parts = intent.TryGetValue(out var rawIntent) ? rawIntent.Split('|') : [];
            if (parts.Length != 2 || parts[0] != config.Name || !Guid.TryParseExact(parts[1], "N", out var owner))
            {
                return Results.Redirect(FrontendUrl(options, ("error", "invalid_state"), ("provider", config.Name), ("mode", BindMode)));
            }

            await cache.RemoveAsync(intentKey, http.RequestAborted).ConfigureAwait(false);
            userId = owner;
        }

        var state = RefreshTokens.Generate();
        var browser = RefreshTokens.Generate();
        var verifier = handler.UsesPkce ? RefreshTokens.Generate() : null;
        await cache.SetAsync(new CacheKey("identity:external:state:" + Hash(state)), EncodeState(config.Name, mode, tenant, Hash(browser), verifier, userId), CacheEntryOptions.Expires(_stateLifetime), http.RequestAborted).ConfigureAwait(false);
        http.Response.Cookies.Append(CookieName, browser, new CookieOptions
        {
            HttpOnly = true,
            Secure = http.Request.IsHttps,
            SameSite = SameSiteMode.Lax,
            Path = Prefix,
            MaxAge = _stateLifetime,
        });
        var challenge = verifier is null ? null : Base64Url(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)));
        return Results.Redirect(handler.AuthorizeUrl(config, CallbackUrl(http, options, config), state, challenge));
    }

    private static async Task<IResult> CallbackAsync(HttpContext http, string provider, ExternalLoginOptions options, ExternalLoginProviders providers, ISharedCache cache, IHttpClientFactory clients, ILoggerFactory loggers)
    {
        var config = options.Find(provider);
        if (config is null || providers.Find(config.Type) is not { } handler)
        {
            return Results.Redirect(FrontendUrl(options, ("error", "invalid_provider"), ("provider", provider)));
        }

        var stateValue = http.Request.Query["state"].ToString();
        var stateKey = new CacheKey("identity:external:state:" + Hash(stateValue));
        var stored = stateValue.Length == 0 ? default : await cache.GetAsync<string>(stateKey, http.RequestAborted).ConfigureAwait(false);
        var state = stored.TryGetValue(out var raw) ? DecodeState(raw) : null;
        var browser = http.Request.Cookies[CookieName];
        http.Response.Cookies.Delete(CookieName, new CookieOptions { Path = Prefix });
        if (state is null || state.Provider != config.Name || browser is null || !FixedEquals(state.BrowserHash, Hash(browser)))
        {
            return Results.Redirect(FrontendUrl(options, ("error", "invalid_state"), ("provider", config.Name)));
        }

        await cache.RemoveAsync(stateKey, http.RequestAborted).ConfigureAwait(false);
        if (http.Request.Query["error"].ToString().Length > 0 || http.Request.Query["code"].ToString() is not { Length: > 0 } code)
        {
            return Results.Redirect(FrontendUrl(options, ("error", "access_denied"), ("provider", config.Name), ("mode", state.Mode)));
        }

        ExternalIdentity identity;
        try
        {
            identity = await handler.ExchangeAsync(config, clients.CreateClient(HttpClientName), code, CallbackUrl(http, options, config), state.Verifier, http.RequestAborted).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is ExternalLoginException or HttpRequestException or TaskCanceledException or JsonException)
        {
            LogExchangeFailed(loggers.CreateLogger(typeof(ExternalLoginFlow)), exception, config.Name);
            return Results.Redirect(FrontendUrl(options, ("error", "external_auth_failed"), ("provider", config.Name), ("mode", state.Mode)));
        }

        var ticket = RefreshTokens.Generate();
        var issued = new ExternalTicket(config.Name, config.Type, identity, state.Mode, state.Tenant, state.UserId);
        await cache.SetAsync(new CacheKey("identity:external:ticket:" + Hash(ticket)), EncodeTicket(issued), CacheEntryOptions.Expires(_ticketLifetime), http.RequestAborted).ConfigureAwait(false);
        return Results.Redirect(FrontendUrl(options, ("ticket", ticket), ("mode", state.Mode), ("provider", config.Name)));
    }

    /// <summary>
    /// 回调地址（在第三方后台登记的就是它）：配置了 <c>CallbackBaseUrl</c> 用它，否则取请求的来源。
    /// </summary>
    private static string CallbackUrl(HttpContext http, ExternalLoginOptions options, ExternalProviderConfig config)
        => $"{options.CallbackBaseUrl ?? $"{http.Request.Scheme}://{http.Request.Host}{http.Request.PathBase}"}{Prefix}/{config.Name}/callback";

    private static string FrontendUrl(ExternalLoginOptions options, params (string Key, string? Value)[] parameters)
    {
        var query = QueryHelpers.AddQueryString(string.Empty, parameters.Where(static p => !string.IsNullOrEmpty(p.Value)).Select(static p => new KeyValuePair<string, string?>(p.Key, p.Value)));
        var page = options.FrontendCallbackUrl;
        return page.Contains('?', StringComparison.Ordinal) ? page + "&" + query[1..] : page + query;
    }

    private sealed record StateEntry(string Provider, string Mode, string Tenant, string BrowserHash, string? Verifier, Guid? UserId);

    private static string EncodeState(string provider, string mode, string tenant, string browserHash, string? verifier, Guid? userId)
        => Json(writer =>
        {
            writer.WriteString("p", provider);
            writer.WriteString("m", mode);
            writer.WriteString("t", tenant);
            writer.WriteString("b", browserHash);
            writer.WriteString("v", verifier);
            writer.WriteString("u", userId?.ToString("N"));
        });

    private static StateEntry? DecodeState(string raw)
    {
        try
        {
            using var document = JsonDocument.Parse(raw);
            var root = document.RootElement;
            return new StateEntry(root.GetProperty("p").GetString()!, root.GetProperty("m").GetString()!, root.GetProperty("t").GetString()!, root.GetProperty("b").GetString()!, root.GetProperty("v").GetString(), UserOf(root));
        }
        catch (Exception exception) when (exception is JsonException or KeyNotFoundException or InvalidOperationException)
        {
            return null;
        }
    }

    private static string EncodeTicket(ExternalTicket ticket)
        => Json(writer =>
        {
            writer.WriteString("p", ticket.Provider);
            writer.WriteString("y", ticket.ProviderType);
            writer.WriteString("k", ticket.Identity.ProviderKey);
            writer.WriteString("n", ticket.Identity.DisplayName);
            writer.WriteString("e", ticket.Identity.Email);
            writer.WriteString("a", ticket.Identity.AvatarUrl);
            writer.WriteString("m", ticket.Mode);
            writer.WriteString("t", ticket.Tenant);
            writer.WriteString("u", ticket.UserId?.ToString("N"));
        });

    private static ExternalTicket? DecodeTicket(string raw)
    {
        try
        {
            using var document = JsonDocument.Parse(raw);
            var root = document.RootElement;
            var identity = new ExternalIdentity(root.GetProperty("k").GetString()!, root.GetProperty("n").GetString(), root.GetProperty("e").GetString(), root.GetProperty("a").GetString());
            return new ExternalTicket(root.GetProperty("p").GetString()!, root.GetProperty("y").GetString()!, identity, root.GetProperty("m").GetString()!, root.GetProperty("t").GetString()!, UserOf(root));
        }
        catch (Exception exception) when (exception is JsonException or KeyNotFoundException or InvalidOperationException)
        {
            return null;
        }
    }

    private static Guid? UserOf(JsonElement root) => Guid.TryParseExact(root.GetProperty("u").GetString(), "N", out var userId) ? userId : null;

    private static string Json(Action<Utf8JsonWriter> body)
    {
        var buffer = new ArrayBufferWriter<byte>();
        using (var writer = new Utf8JsonWriter(buffer))
        {
            writer.WriteStartObject();
            body(writer);
            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    private static string Hash(string value) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    private static bool FixedEquals(string left, string right) => CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(left), Encoding.ASCII.GetBytes(right));

    private static string Base64Url(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    [LoggerMessage(Level = LogLevel.Warning, Message = "External login with {Provider} failed while exchanging the code.")]
    private static partial void LogExchangeFailed(ILogger logger, Exception exception, string provider);
}

/// <summary>
/// 登记的提供商协议（按类型）。
/// </summary>
/// <param name="providers">全部协议。</param>
public sealed class ExternalLoginProviders(IEnumerable<IExternalLoginProvider> providers)
{
    private readonly Dictionary<string, IExternalLoginProvider> _byType = providers.GroupBy(static p => p.Type, StringComparer.Ordinal).ToDictionary(static g => g.Key, static g => g.First(), StringComparer.Ordinal);

    /// <summary>
    /// 按类型取。
    /// </summary>
    public IExternalLoginProvider? Find(string type) => _byType.GetValueOrDefault(type);
}
