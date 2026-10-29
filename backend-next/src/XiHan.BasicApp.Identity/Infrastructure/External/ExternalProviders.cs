// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.WebUtilities;

namespace XiHan.BasicApp.Identity.Infrastructure.External;

/// <summary>
/// 第三方给出的身份。
/// </summary>
/// <param name="ProviderKey">第三方的用户标识。</param>
/// <param name="DisplayName">显示名。</param>
/// <param name="Email">邮箱。</param>
/// <param name="AvatarUrl">头像。</param>
public sealed record ExternalIdentity(string ProviderKey, string? DisplayName, string? Email, string? AvatarUrl);

/// <summary>
/// 一家第三方登录的协议。
/// </summary>
/// <remarks>
/// 照 4.x 的处理器逐家重写，不经 ASP.NET Core 的认证处理器。拼授权地址、用授权码换令牌、取用户资料。
/// </remarks>
public interface IExternalLoginProvider
{
    /// <summary>
    /// 类型（<see cref="ExternalProviderTypes"/>）。
    /// </summary>
    string Type { get; }

    /// <summary>
    /// 是否用 PKCE。
    /// </summary>
    /// <remarks>
    /// 只有 Google，4.x 同。
    /// </remarks>
    bool UsesPkce => false;

    /// <summary>
    /// 授权地址。
    /// </summary>
    string AuthorizeUrl(ExternalProviderConfig config, string redirectUri, string state, string? codeChallenge);

    /// <summary>
    /// 用授权码换身份。
    /// </summary>
    /// <exception cref="ExternalLoginException">失败时抛出。</exception>
    Task<ExternalIdentity> ExchangeAsync(ExternalProviderConfig config, HttpClient http, string code, string redirectUri, string? codeVerifier, CancellationToken cancellationToken);
}

/// <summary>
/// 第三方返回了错误（说明进日志，不给用户看）。
/// </summary>
public sealed class ExternalLoginException(string message) : Exception(message);

/// <summary>
/// 各家共用的 HTTP 小工具。
/// </summary>
internal static class ProviderHttp
{
    public static string Url(string endpoint, params (string Key, string? Value)[] query)
        => QueryHelpers.AddQueryString(endpoint, query.Where(static q => q.Value is not null).Select(static q => new KeyValuePair<string, string?>(q.Key, q.Value)));

    public static async Task<JsonObject> GetAsync(HttpClient http, string url, CancellationToken cancellationToken, string? bearer = null, (string Name, string Value)? header = null)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        if (bearer is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
        }

        if (header is { } extra)
        {
            request.Headers.TryAddWithoutValidation(extra.Name, extra.Value);
        }

        return await SendAsync(http, request, cancellationToken).ConfigureAwait(false);
    }

    public static async Task<JsonObject> PostFormAsync(HttpClient http, string url, IEnumerable<KeyValuePair<string, string>> form, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = new FormUrlEncodedContent(form) };
        return await SendAsync(http, request, cancellationToken).ConfigureAwait(false);
    }

    public static async Task<JsonObject> PostJsonAsync(HttpClient http, string url, JsonObject body, CancellationToken cancellationToken, string? bearer = null)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json") };
        if (bearer is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
        }

        return await SendAsync(http, request, cancellationToken).ConfigureAwait(false);
    }

    public static async Task<JsonNode?> GetNodeAsync(HttpClient http, string url, string bearer, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
        using var response = await http.SendAsync(request, cancellationToken).ConfigureAwait(false);
        return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<JsonNode>(cancellationToken).ConfigureAwait(false) : null;
    }

    private static async Task<JsonObject> SendAsync(HttpClient http, HttpRequestMessage request, CancellationToken cancellationToken)
    {
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        using var response = await http.SendAsync(request, cancellationToken).ConfigureAwait(false);
        var text = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            throw new ExternalLoginException($"{request.RequestUri?.GetLeftPart(UriPartial.Path)} returned {(int)response.StatusCode}: {Clip(text)}");
        }

        try
        {
            return JsonNode.Parse(text) as JsonObject ?? throw new ExternalLoginException($"{request.RequestUri?.GetLeftPart(UriPartial.Path)} did not return a JSON object.");
        }
        catch (JsonException)
        {
            throw new ExternalLoginException($"{request.RequestUri?.GetLeftPart(UriPartial.Path)} did not return JSON: {Clip(text)}");
        }
    }

    public static string? Text(JsonNode? node, string name)
        => node?[name] is JsonValue value ? value.TryGetValue<string>(out var s) ? Blank(s) : Blank(value.ToJsonString().Trim('"')) : null;

    public static string Require(JsonNode? node, string name, string what)
        => Text(node, name) ?? throw new ExternalLoginException($"{what}: missing {name}.");

    public static string Scope(ExternalProviderConfig config, IReadOnlyList<string> defaults, string separator, bool replace = false)
        => string.Join(separator, (replace && config.Scopes.Count > 0 ? config.Scopes : defaults.Concat(config.Scopes)).Distinct(StringComparer.Ordinal));

    public static string? Blank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;

    private static string Clip(string text) => text.Length <= 300 ? text : text[..300];
}

/// <summary>
/// 标准协议几家提供商的公共部分。
/// </summary>
/// <remarks>
/// 授权地址用 client_id / scope / response_type / redirect_uri / state 这一组标准参数、换码用标准表单。
/// </remarks>
internal abstract class StandardProvider : IExternalLoginProvider
{
    public abstract string Type { get; }

    public virtual bool UsesPkce => false;

    protected abstract string AuthorizeEndpoint(ExternalProviderConfig config);

    protected abstract string TokenEndpoint(ExternalProviderConfig config);

    protected abstract string DefaultScope(ExternalProviderConfig config);

    public virtual string AuthorizeUrl(ExternalProviderConfig config, string redirectUri, string state, string? codeChallenge)
        => ProviderHttp.Url(config.AuthorizationEndpoint ?? AuthorizeEndpoint(config),
            ("client_id", config.ClientId), ("scope", DefaultScope(config)), ("response_type", "code"), ("redirect_uri", redirectUri),
            ("code_challenge", codeChallenge), ("code_challenge_method", codeChallenge is null ? null : "S256"), ("state", state));

    public async Task<ExternalIdentity> ExchangeAsync(ExternalProviderConfig config, HttpClient http, string code, string redirectUri, string? codeVerifier, CancellationToken cancellationToken)
    {
        var token = await TokenAsync(config, http, code, redirectUri, codeVerifier, cancellationToken).ConfigureAwait(false);
        return await ProfileAsync(config, http, token, cancellationToken).ConfigureAwait(false);
    }

    protected virtual async Task<string> TokenAsync(ExternalProviderConfig config, HttpClient http, string code, string redirectUri, string? codeVerifier, CancellationToken cancellationToken)
    {
        var form = new List<KeyValuePair<string, string>>
        {
            new("client_id", config.ClientId), new("redirect_uri", redirectUri), new("client_secret", config.ClientSecret), new("code", code), new("grant_type", "authorization_code"),
        };
        if (codeVerifier is not null)
        {
            form.Add(new("code_verifier", codeVerifier));
        }

        var token = await ProviderHttp.PostFormAsync(http, TokenEndpoint(config), form, cancellationToken).ConfigureAwait(false);
        return ProviderHttp.Text(token, "access_token") ?? throw new ExternalLoginException($"{Type} token: {ProviderHttp.Text(token, "error_description") ?? ProviderHttp.Text(token, "error") ?? "no access_token"}");
    }

    protected abstract Task<ExternalIdentity> ProfileAsync(ExternalProviderConfig config, HttpClient http, string accessToken, CancellationToken cancellationToken);
}

/// <summary>
/// Google 登录。
/// </summary>
/// <remarks>
/// PKCE，openid profile email；key sub。
/// </remarks>
internal sealed class GoogleProvider : StandardProvider
{
    public override string Type => "google";

    public override bool UsesPkce => true;

    protected override string AuthorizeEndpoint(ExternalProviderConfig config) => "https://accounts.google.com/o/oauth2/v2/auth";

    protected override string TokenEndpoint(ExternalProviderConfig config) => "https://oauth2.googleapis.com/token";

    protected override string DefaultScope(ExternalProviderConfig config) => ProviderHttp.Scope(config, ["openid", "profile", "email"], " ");

    protected override async Task<ExternalIdentity> ProfileAsync(ExternalProviderConfig config, HttpClient http, string accessToken, CancellationToken cancellationToken)
    {
        var user = await ProviderHttp.GetAsync(http, "https://www.googleapis.com/oauth2/v3/userinfo", cancellationToken, bearer: accessToken).ConfigureAwait(false);
        return new ExternalIdentity(ProviderHttp.Require(user, "sub", "google userinfo"), ProviderHttp.Text(user, "name"), ProviderHttp.Text(user, "email"), ProviderHttp.Text(user, "picture"));
    }
}

/// <summary>
/// GitHub 登录。
/// </summary>
/// <remarks>
/// key id、名称取 login；邮箱为空且范围含 user:email 时取主邮箱。
/// </remarks>
internal sealed class GitHubProvider : StandardProvider
{
    public override string Type => "github";

    protected override string AuthorizeEndpoint(ExternalProviderConfig config) => "https://github.com/login/oauth/authorize";

    protected override string TokenEndpoint(ExternalProviderConfig config) => "https://github.com/login/oauth/access_token";

    protected override string DefaultScope(ExternalProviderConfig config) => ProviderHttp.Scope(config, [], " ");

    protected override async Task<ExternalIdentity> ProfileAsync(ExternalProviderConfig config, HttpClient http, string accessToken, CancellationToken cancellationToken)
    {
        var user = await ProviderHttp.GetAsync(http, "https://api.github.com/user", cancellationToken, bearer: accessToken).ConfigureAwait(false);
        var email = ProviderHttp.Text(user, "email");
        if (email is null && config.Scopes.Contains("user:email", StringComparer.Ordinal)
            && await ProviderHttp.GetNodeAsync(http, "https://api.github.com/user/emails", accessToken, cancellationToken).ConfigureAwait(false) is JsonArray emails)
        {
            email = emails.OfType<JsonObject>().FirstOrDefault(static e => e["primary"]?.GetValue<bool>() == true) is { } primary ? ProviderHttp.Text(primary, "email") : null;
        }

        return new ExternalIdentity(ProviderHttp.Require(user, "id", "github user"), ProviderHttp.Text(user, "login"), email, ProviderHttp.Text(user, "avatar_url"));
    }
}

/// <summary>
/// Gitee 登录。
/// </summary>
/// <remarks>
/// user_info emails；key id、名称取 login；邮箱为空时取邮箱列表里第一个。
/// </remarks>
internal sealed class GiteeProvider : StandardProvider
{
    public override string Type => "gitee";

    protected override string AuthorizeEndpoint(ExternalProviderConfig config) => "https://gitee.com/oauth/authorize";

    protected override string TokenEndpoint(ExternalProviderConfig config) => "https://gitee.com/oauth/token";

    protected override string DefaultScope(ExternalProviderConfig config) => ProviderHttp.Scope(config, ["user_info", "emails"], " ");

    protected override async Task<ExternalIdentity> ProfileAsync(ExternalProviderConfig config, HttpClient http, string accessToken, CancellationToken cancellationToken)
    {
        var user = await ProviderHttp.GetAsync(http, "https://gitee.com/api/v5/user", cancellationToken, bearer: accessToken).ConfigureAwait(false);
        var email = ProviderHttp.Text(user, "email");
        if (email is null && DefaultScope(config).Split(' ').Contains("emails", StringComparer.Ordinal)
            && await ProviderHttp.GetNodeAsync(http, "https://gitee.com/api/v5/emails", accessToken, cancellationToken).ConfigureAwait(false) is JsonArray emails)
        {
            email = emails.Select(static e => ProviderHttp.Text(e, "email")).FirstOrDefault(static e => e is not null);
        }

        return new ExternalIdentity(ProviderHttp.Require(user, "id", "gitee user"), ProviderHttp.Text(user, "login"), email, ProviderHttp.Text(user, "avatar_url"));
    }
}

/// <summary>
/// 飞书登录。
/// </summary>
/// <remarks>
/// 扫码走 passport、账号走开放平台 v1 / v2，两套端点成套替换；key union_id ?? open_id。
/// </remarks>
internal sealed class LarkProvider : StandardProvider
{
    public override string Type => "lark";

    protected override string AuthorizeEndpoint(ExternalProviderConfig config)
        => config.Mode == ExternalLoginMode.Account ? "https://accounts.feishu.cn/open-apis/authen/v1/authorize" : "https://passport.feishu.cn/suite/passport/oauth/authorize";

    protected override string TokenEndpoint(ExternalProviderConfig config)
        => config.Mode == ExternalLoginMode.Account ? "https://open.feishu.cn/open-apis/authen/v2/oauth/token" : "https://passport.feishu.cn/suite/passport/oauth/token";

    protected override string DefaultScope(ExternalProviderConfig config) => ProviderHttp.Scope(config, [], " ");

    protected override async Task<string> TokenAsync(ExternalProviderConfig config, HttpClient http, string code, string redirectUri, string? codeVerifier, CancellationToken cancellationToken)
    {
        if (config.Mode != ExternalLoginMode.Account)
        {
            return await base.TokenAsync(config, http, code, redirectUri, codeVerifier, cancellationToken).ConfigureAwait(false);
        }

        var token = await ProviderHttp.PostJsonAsync(http, TokenEndpoint(config), new JsonObject
        {
            ["grant_type"] = "authorization_code", ["client_id"] = config.ClientId, ["client_secret"] = config.ClientSecret, ["code"] = code, ["redirect_uri"] = redirectUri,
        }, cancellationToken).ConfigureAwait(false);
        Check(token);
        return ProviderHttp.Require(token, "access_token", "lark token");
    }

    protected override async Task<ExternalIdentity> ProfileAsync(ExternalProviderConfig config, HttpClient http, string accessToken, CancellationToken cancellationToken)
    {
        var url = config.Mode == ExternalLoginMode.Account ? "https://open.feishu.cn/open-apis/authen/v1/user_info" : "https://passport.feishu.cn/suite/passport/oauth/userinfo";
        var response = await ProviderHttp.GetAsync(http, url, cancellationToken, bearer: accessToken).ConfigureAwait(false);
        Check(response);
        JsonNode user = response["data"] as JsonObject ?? response;
        var key = ProviderHttp.Text(user, "union_id") ?? ProviderHttp.Require(user, "open_id", "lark user");
        return new ExternalIdentity(key, ProviderHttp.Text(user, "name"), ProviderHttp.Text(user, "email"), ProviderHttp.Text(user, "avatar_url"));
    }

    private static void Check(JsonObject response)
    {
        if (response["code"] is JsonValue code && code.ToJsonString() != "0")
        {
            throw new ExternalLoginException("lark: " + (ProviderHttp.Text(response, "msg") ?? code.ToJsonString()));
        }

        if (ProviderHttp.Text(response, "error") is { } error)
        {
            throw new ExternalLoginException("lark: " + (ProviderHttp.Text(response, "error_description") ?? error));
        }
    }
}

/// <summary>
/// QQ 登录。
/// </summary>
/// <remarks>
/// 换码 GET + fmt=json，再取 openid 与资料；key openid、nickname、figureurl_qq_2，没有邮箱。
/// </remarks>
internal sealed class QqProvider : IExternalLoginProvider
{
    public string Type => "qq";

    public string AuthorizeUrl(ExternalProviderConfig config, string redirectUri, string state, string? codeChallenge)
        => ProviderHttp.Url(config.AuthorizationEndpoint ?? "https://graph.qq.com/oauth2.0/authorize",
            ("client_id", config.ClientId), ("scope", ProviderHttp.Scope(config, ["get_user_info"], ",")), ("response_type", "code"), ("redirect_uri", redirectUri), ("state", state));

    public async Task<ExternalIdentity> ExchangeAsync(ExternalProviderConfig config, HttpClient http, string code, string redirectUri, string? codeVerifier, CancellationToken cancellationToken)
    {
        var token = await ProviderHttp.GetAsync(http, ProviderHttp.Url("https://graph.qq.com/oauth2.0/token",
            ("client_id", config.ClientId), ("client_secret", config.ClientSecret), ("redirect_uri", redirectUri), ("code", code), ("grant_type", "authorization_code"), ("fmt", "json")), cancellationToken).ConfigureAwait(false);
        if (ProviderHttp.Text(token, "error") is { } error && error != "0")
        {
            throw new ExternalLoginException("qq token: " + (ProviderHttp.Text(token, "error_description") ?? error));
        }

        var accessToken = ProviderHttp.Require(token, "access_token", "qq token");
        var me = await ProviderHttp.GetAsync(http, ProviderHttp.Url("https://graph.qq.com/oauth2.0/me", ("access_token", accessToken), ("fmt", "json")), cancellationToken).ConfigureAwait(false);
        if (ProviderHttp.Text(me, "error") is { } meError && meError != "0")
        {
            throw new ExternalLoginException("qq me: " + (ProviderHttp.Text(me, "error_description") ?? meError));
        }

        var openId = ProviderHttp.Require(me, "openid", "qq me");
        var user = await ProviderHttp.GetAsync(http, ProviderHttp.Url("https://graph.qq.com/user/get_user_info", ("oauth_consumer_key", config.ClientId), ("access_token", accessToken), ("openid", openId)), cancellationToken).ConfigureAwait(false);
        if (ProviderHttp.Text(user, "ret") is { } ret && ret != "0")
        {
            throw new ExternalLoginException("qq user: " + (ProviderHttp.Text(user, "msg") ?? ret));
        }

        return new ExternalIdentity(openId, ProviderHttp.Text(user, "nickname"), null, ProviderHttp.Text(user, "figureurl_qq_2"));
    }
}

/// <summary>
/// 微信登录。
/// </summary>
/// <remarks>
/// 扫码 qrconnect / snsapi_login，账号走公众号网页授权 / snsapi_userinfo；参数按序、末尾 <c>#wechat_redirect</c>；state 是短随机串，不需要 4.x 的短 state 绕法；key unionid ?? openid。
/// </remarks>
internal sealed class WeChatProvider : IExternalLoginProvider
{
    public string Type => "wechat";

    public string AuthorizeUrl(ExternalProviderConfig config, string redirectUri, string state, string? codeChallenge)
    {
        var account = config.Mode == ExternalLoginMode.Account;
        var endpoint = config.AuthorizationEndpoint ?? (account ? "https://open.weixin.qq.com/connect/oauth2/authorize" : "https://open.weixin.qq.com/connect/qrconnect");
        var scope = ProviderHttp.Scope(config, [account ? "snsapi_userinfo" : "snsapi_login"], ",", replace: true);
        return ProviderHttp.Url(endpoint, ("appid", config.ClientId), ("redirect_uri", redirectUri), ("response_type", "code"), ("scope", scope), ("state", state)) + "#wechat_redirect";
    }

    public async Task<ExternalIdentity> ExchangeAsync(ExternalProviderConfig config, HttpClient http, string code, string redirectUri, string? codeVerifier, CancellationToken cancellationToken)
    {
        var token = await ProviderHttp.GetAsync(http, ProviderHttp.Url("https://api.weixin.qq.com/sns/oauth2/access_token", ("appid", config.ClientId), ("secret", config.ClientSecret), ("code", code), ("grant_type", "authorization_code")), cancellationToken).ConfigureAwait(false);
        CheckErrcode(token, "wechat token");
        var accessToken = ProviderHttp.Require(token, "access_token", "wechat token");
        var openId = ProviderHttp.Require(token, "openid", "wechat token");
        var unionId = ProviderHttp.Text(token, "unionid");
        var scope = ProviderHttp.Text(token, "scope");
        if (scope is not null && !scope.Contains("snsapi_userinfo", StringComparison.Ordinal) && !scope.Contains("snsapi_login", StringComparison.Ordinal))
        {
            return new ExternalIdentity(unionId ?? openId, null, null, null);
        }

        var user = await ProviderHttp.GetAsync(http, ProviderHttp.Url("https://api.weixin.qq.com/sns/userinfo", ("access_token", accessToken), ("openid", openId), ("lang", "zh_CN")), cancellationToken).ConfigureAwait(false);
        CheckErrcode(user, "wechat userinfo");
        var key = ProviderHttp.Text(user, "unionid") ?? unionId ?? ProviderHttp.Text(user, "openid") ?? openId;
        return new ExternalIdentity(key, ProviderHttp.Text(user, "nickname"), null, ProviderHttp.Text(user, "headimgurl"));
    }

    internal static void CheckErrcode(JsonObject response, string what)
    {
        if (ProviderHttp.Text(response, "errcode") is { } errcode && errcode != "0")
        {
            throw new ExternalLoginException($"{what}: {ProviderHttp.Text(response, "errmsg") ?? errcode}");
        }
    }
}

/// <summary>
/// 企业微信登录。
/// </summary>
/// <remarks>
/// 扫码 sso/login、账号走网页授权 snsapi_privateinfo；令牌是应用级的 gettoken、与授权码无关，授权码交给 getuserinfo；有 user_ticket 再取详情，开了 LoadMemberProfile 再补成员资料；key userid ?? openid。
/// </remarks>
internal sealed class WeComProvider : IExternalLoginProvider
{
    public string Type => "wecom";

    public string AuthorizeUrl(ExternalProviderConfig config, string redirectUri, string state, string? codeChallenge)
    {
        if (config.Mode == ExternalLoginMode.Account)
        {
            var scope = ProviderHttp.Scope(config, ["snsapi_privateinfo"], ",", replace: true);
            return ProviderHttp.Url(config.AuthorizationEndpoint ?? "https://open.weixin.qq.com/connect/oauth2/authorize",
                ("appid", config.ClientId), ("redirect_uri", redirectUri), ("response_type", "code"), ("scope", scope), ("agentid", config.AgentId), ("state", state)) + "#wechat_redirect";
        }

        return ProviderHttp.Url(config.AuthorizationEndpoint ?? "https://login.work.weixin.qq.com/wwlogin/sso/login",
            ("login_type", "CorpApp"), ("appid", config.ClientId), ("agentid", config.AgentId), ("redirect_uri", redirectUri), ("state", state));
    }

    public async Task<ExternalIdentity> ExchangeAsync(ExternalProviderConfig config, HttpClient http, string code, string redirectUri, string? codeVerifier, CancellationToken cancellationToken)
    {
        var token = await ProviderHttp.GetAsync(http, ProviderHttp.Url("https://qyapi.weixin.qq.com/cgi-bin/gettoken", ("corpid", config.ClientId), ("corpsecret", config.ClientSecret)), cancellationToken).ConfigureAwait(false);
        WeChatProvider.CheckErrcode(token, "wecom gettoken");
        var accessToken = ProviderHttp.Require(token, "access_token", "wecom gettoken");
        var info = await ProviderHttp.GetAsync(http, ProviderHttp.Url("https://qyapi.weixin.qq.com/cgi-bin/auth/getuserinfo", ("access_token", accessToken), ("code", code)), cancellationToken).ConfigureAwait(false);
        WeChatProvider.CheckErrcode(info, "wecom getuserinfo");
        var merged = Scalars(info);
        if (ProviderHttp.Text(info, "user_ticket") is { } ticket)
        {
            var detail = await ProviderHttp.PostJsonAsync(http, ProviderHttp.Url("https://qyapi.weixin.qq.com/cgi-bin/auth/getuserdetail", ("access_token", accessToken)), new JsonObject { ["user_ticket"] = ticket }, cancellationToken).ConfigureAwait(false);
            WeChatProvider.CheckErrcode(detail, "wecom getuserdetail");
            foreach (var (key, value) in Scalars(detail))
            {
                merged[key] = value;
            }
        }

        if (config.LoadMemberProfile && merged.TryGetValue("userid", out var userId))
        {
            var member = await ProviderHttp.GetAsync(http, ProviderHttp.Url("https://qyapi.weixin.qq.com/cgi-bin/user/get", ("access_token", accessToken), ("userid", userId)), cancellationToken).ConfigureAwait(false);
            if (ProviderHttp.Text(member, "errcode") is null or "0")
            {
                foreach (var (key, value) in Scalars(member))
                {
                    merged.TryAdd(key, value);
                }
            }
        }

        var providerKey = merged.GetValueOrDefault("userid") ?? merged.GetValueOrDefault("openid") ?? throw new ExternalLoginException("wecom: missing userid.");
        return new ExternalIdentity(providerKey, merged.GetValueOrDefault("name"), merged.GetValueOrDefault("email"), merged.GetValueOrDefault("avatar"));
    }

    private static Dictionary<string, string> Scalars(JsonObject response)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var (key, value) in response)
        {
            if (key is "errcode" or "errmsg" or "user_ticket" || value is not JsonValue || ProviderHttp.Text(response, key) is not { } text)
            {
                continue;
            }

            result[key] = text;
        }

        return result;
    }
}

/// <summary>
/// 钉钉登录。
/// </summary>
/// <remarks>
/// 扫码 challenge.htm、账号 auth，prompt=consent，可限定 corpId；换码走驼峰 JSON；资料用 <c>x-acs-dingtalk-access-token</c> 头；key unionId ?? openId。
/// </remarks>
internal sealed class DingTalkProvider : IExternalLoginProvider
{
    public string Type => "dingtalk";

    public string AuthorizeUrl(ExternalProviderConfig config, string redirectUri, string state, string? codeChallenge)
        => ProviderHttp.Url(config.AuthorizationEndpoint ?? (config.Mode == ExternalLoginMode.Account ? "https://login.dingtalk.com/oauth2/auth" : "https://login.dingtalk.com/oauth2/challenge.htm"),
            ("client_id", config.ClientId), ("redirect_uri", redirectUri), ("response_type", "code"), ("scope", ProviderHttp.Scope(config, ["openid"], " ")), ("prompt", "consent"), ("corpId", ProviderHttp.Blank(config.CorpId)), ("state", state));

    public async Task<ExternalIdentity> ExchangeAsync(ExternalProviderConfig config, HttpClient http, string code, string redirectUri, string? codeVerifier, CancellationToken cancellationToken)
    {
        var token = await ProviderHttp.PostJsonAsync(http, "https://api.dingtalk.com/v1.0/oauth2/userAccessToken", new JsonObject
        {
            ["clientId"] = config.ClientId, ["clientSecret"] = config.ClientSecret, ["code"] = code, ["grantType"] = "authorization_code",
        }, cancellationToken).ConfigureAwait(false);
        var accessToken = ProviderHttp.Require(token, "accessToken", "dingtalk token");
        var user = await ProviderHttp.GetAsync(http, "https://api.dingtalk.com/v1.0/contact/users/me", cancellationToken, header: ("x-acs-dingtalk-access-token", accessToken)).ConfigureAwait(false);
        if (user.ContainsKey("code"))
        {
            throw new ExternalLoginException("dingtalk user: " + (ProviderHttp.Text(user, "message") ?? ProviderHttp.Text(user, "code")));
        }

        var key = ProviderHttp.Text(user, "unionId") ?? ProviderHttp.Require(user, "openId", "dingtalk user");
        return new ExternalIdentity(key, ProviderHttp.Text(user, "nick"), ProviderHttp.Text(user, "email"), ProviderHttp.Text(user, "avatarUrl"));
    }
}
