// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using XiHan.BasicApp.Integration.Tests.Infrastructure;

namespace XiHan.BasicApp.Integration.Tests;

/// <summary>
/// 验证 OAuth 2.0 / OIDC 服务端的授权、令牌与吊销
/// </summary>
/// <remarks>
/// 授权端点把查询串带去同意页；同意页预览校验请求（客户端、回调地址、PKCE、openid）；同意发授权码、记下同意，再来即不必确认；
/// 令牌端点（机密客户端 Basic / 表单认证、公开客户端 PKCE）发不透明的访问令牌与刷新令牌和 RS256 的 id_token（受众 ClientId、nonce、按范围的声明，用 JWKS 验得过）；
/// 授权码重放与作废的刷新令牌再用都整族吊销；撤销与撤销同意立即让令牌失效；第三方的访问令牌后台接口不认；拒绝只跳回登记过的地址；
/// 签名密钥轮换后先公布、到点才接替签名，正在签名的不能停用，停用的不再公布
/// </remarks>
public sealed class OAuthServerTests : IDisposable
{
    private const string Callback = "https://rp.example.com/cb";
    private readonly TestDatabase _database = new();

    public void Dispose() => _database.Dispose();

    private static async Task<(HttpStatusCode Status, JsonElement Body)> SendAsync(BasicAppTestServer server, HttpMethod method, string path, string? json, string? token)
    {
        var (response, body) = await server.SendAsync(BasicAppTestServer.Request(method, path, json, token));
        return (response.StatusCode, body);
    }

    private static async Task<(HttpResponseMessage Response, JsonElement Body)> FormAsync(BasicAppTestServer server, string path, Dictionary<string, string> form, string? basic = null, string? bearer = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, path) { Content = new FormUrlEncodedContent(form) };
        if (basic is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes(basic)));
        }

        if (bearer is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
        }

        return await server.SendAsync(request);
    }

    private static async Task<HttpStatusCode> UserInfoStatusAsync(BasicAppTestServer server, string accessToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/oauth2/userinfo");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return (await server.SendAsync(request)).Response.StatusCode;
    }

    private static string Query(string clientId, string scope = "openid profile", string? challenge = null, string state = "xyz", string nonce = "n-1")
    {
        var query = new Dictionary<string, string?> { ["responseType"] = "code", ["clientId"] = clientId, ["redirectUri"] = Callback, ["scope"] = scope, ["state"] = state, ["nonce"] = nonce };
        if (challenge is not null)
        {
            query["codeChallenge"] = challenge;
            query["codeChallengeMethod"] = "S256";
        }

        return QueryHelpers.AddQueryString(string.Empty, query);
    }

    private static string Json(string clientId, string scope = "openid profile", string? challenge = null, string state = "xyz", string nonce = "n-1")
        => JsonSerializer.Serialize(new { responseType = "code", clientId, redirectUri = Callback, scope, state, nonce, codeChallenge = challenge, codeChallengeMethod = challenge is null ? null : "S256" });

    private static async Task<string> CodeAsync(BasicAppTestServer server, string token, string clientId, string scope = "openid profile", string? challenge = null)
    {
        var (status, approved) = await SendAsync(server, HttpMethod.Post, "/api/oauth-consents:approve", Json(clientId, scope, challenge), token);
        Assert.Equal(HttpStatusCode.OK, status);
        var redirect = new Uri(approved.GetProperty("redirectUri").GetString()!);
        Assert.Equal(Callback, redirect.GetLeftPart(UriPartial.Path));
        var query = QueryHelpers.ParseQuery(redirect.Query);
        Assert.Equal("xyz", query["state"].ToString());
        return query["code"].ToString();
    }

    private static JsonElement Payload(string jwt) => Segment(jwt, 1);

    private static JsonElement Segment(string jwt, int index)
    {
        var part = jwt.Split('.')[index].Replace('-', '+').Replace('_', '/');
        part = part.PadRight(part.Length + ((4 - (part.Length % 4)) % 4), '=');
        return JsonDocument.Parse(Convert.FromBase64String(part)).RootElement.Clone();
    }

    [Fact]
    public async Task AuthorizationCodeFlow_IssuesIdentityTokens_AndRevokesThemOnAbuse()
    {
        var (server, platform, tenant) = await SettingsTestHost.StartAsync(_database.ConnectionString);
        await using var running = server;
        var app = new { appName = "RP", clientId = "rp-app", clientType = "Confidential", appType = "Web", grantTypes = new[] { "authorization_code", "refresh_token" }, redirectUris = new[] { Callback }, scopes = new[] { "openid", "profile", "email" }, accessTokenLifetime = 3600, refreshTokenLifetime = 86400, authorizationCodeLifetime = 300, skipConsent = false, isEnabled = true };
        var (_, created) = await SendAsync(server, HttpMethod.Post, "/api/oauth-apps", JsonSerializer.Serialize(app), platform);
        var secret = created.GetProperty("clientSecret").GetString()!;
        var (_, spa) = await SendAsync(server, HttpMethod.Post, "/api/oauth-apps", JsonSerializer.Serialize(app with { }).Replace("\"rp-app\"", "\"spa-app\"", StringComparison.Ordinal).Replace("\"Confidential\"", "\"Public\"", StringComparison.Ordinal), platform);
        Assert.Equal(JsonValueKind.Null, spa.GetProperty("clientSecret").ValueKind);

        // 授权端点把查询串原样带去同意页
        var authorize = await server.SendAsync(new HttpRequestMessage(HttpMethod.Get, "/api/oauth2/authorize?response_type=code&client_id=rp-app&state=s"));
        Assert.Equal(HttpStatusCode.Redirect, authorize.Response.StatusCode);
        Assert.Equal("/#/oauth/authorize?response_type=code&client_id=rp-app&state=s", authorize.Response.Headers.Location!.OriginalString);

        // 同意页预览：校验客户端、回调地址、PKCE 与 openid；第一次要确认
        var (_, preview) = await SendAsync(server, HttpMethod.Get, "/api/oauth-consents:resolve" + Query("rp-app"), null, tenant);
        Assert.Equal((true, "RP", false), (preview.GetProperty("valid").GetBoolean(), preview.GetProperty("appName").GetString(), preview.GetProperty("skipConsent").GetBoolean()));
        Assert.Equal("unauthorized_client", (await SendAsync(server, HttpMethod.Get, "/api/oauth-consents:resolve" + Query("nobody"), null, tenant)).Body.GetProperty("error").GetString());
        Assert.Equal("invalid_request", (await SendAsync(server, HttpMethod.Get, "/api/oauth-consents:resolve" + Query("spa-app"), null, tenant)).Body.GetProperty("error").GetString());
        Assert.Equal("invalid_scope", (await SendAsync(server, HttpMethod.Get, "/api/oauth-consents:resolve" + Query("rp-app", scope: "profile"), null, tenant)).Body.GetProperty("error").GetString());
        var (_, badRedirect) = await SendAsync(server, HttpMethod.Post, "/api/oauth-consents:deny", Json("rp-app").Replace(Callback, "https://evil.example.com/cb", StringComparison.Ordinal), tenant);
        Assert.Equal("OPENPLATFORM_VALIDATION_014", badRedirect.GetProperty("code").GetString());
        var (_, denied) = await SendAsync(server, HttpMethod.Post, "/api/oauth-consents:deny", Json("rp-app"), tenant);
        Assert.Equal(Callback + "?error=access_denied&error_description=The%20user%20denied%20the%20request.&state=xyz", denied.GetProperty("redirectUri").GetString());

        // 兑换：表单认证；id_token 受众是 ClientId、带 nonce、只按 profile 给声明，用 JWKS 验得过
        var code = await CodeAsync(server, tenant, "rp-app");
        Assert.Equal("invalid_client", (await FormAsync(server, "/api/oauth2/token", new() { ["grant_type"] = "authorization_code", ["code"] = code, ["redirect_uri"] = Callback, ["client_id"] = "rp-app", ["client_secret"] = "wrong" })).Body.GetProperty("error").GetString());
        var (tokenResponse, tokens) = await FormAsync(server, "/api/oauth2/token", new() { ["grant_type"] = "authorization_code", ["code"] = code, ["redirect_uri"] = Callback, ["client_id"] = "rp-app", ["client_secret"] = secret });
        Assert.Equal(HttpStatusCode.OK, tokenResponse.StatusCode);
        Assert.Equal("no-store", tokenResponse.Headers.CacheControl!.ToString());
        Assert.Equal(("Bearer", 3600, "openid profile"), (tokens.GetProperty("token_type").GetString(), tokens.GetProperty("expires_in").GetInt32(), tokens.GetProperty("scope").GetString()));
        var access = tokens.GetProperty("access_token").GetString()!;
        var refresh = tokens.GetProperty("refresh_token").GetString()!;
        var idToken = tokens.GetProperty("id_token").GetString()!;
        var claims = Payload(idToken);
        Assert.Equal(("rp-app", "n-1", "admin"), (claims.GetProperty("aud").GetString(), claims.GetProperty("nonce").GetString(), claims.GetProperty("preferred_username").GetString()));
        Assert.False(claims.TryGetProperty("email", out _));
        var (_, discovery) = await SendAsync(server, HttpMethod.Get, "/api/oauth2/.well-known/openid-configuration", null, null);
        var issuer = discovery.GetProperty("issuer").GetString()!;
        Assert.Equal((issuer, issuer + "/token"), (claims.GetProperty("iss").GetString(), discovery.GetProperty("token_endpoint").GetString()));
        var (_, jwks) = await SendAsync(server, HttpMethod.Get, "/api/oauth2/jwks", null, null);
        var jwk = jwks.GetProperty("keys")[0];
        using var rsa = RSA.Create();
        rsa.ImportParameters(new RSAParameters { Modulus = Base64UrlEncoder.DecodeBytes(jwk.GetProperty("n").GetString()), Exponent = Base64UrlEncoder.DecodeBytes(jwk.GetProperty("e").GetString()) });
        var validated = await new JsonWebTokenHandler().ValidateTokenAsync(idToken, new TokenValidationParameters { ValidIssuer = issuer, ValidAudience = "rp-app", IssuerSigningKey = new RsaSecurityKey(rsa) { KeyId = jwk.GetProperty("kid").GetString() } });
        Assert.True(validated.IsValid, validated.Exception?.Message);

        // userinfo 认得出访问令牌；后台接口不认它（只用于身份）
        var (_, me) = await FormAsync(server, "/api/oauth2/userinfo", [], bearer: access);
        Assert.Equal((claims.GetProperty("sub").GetString(), "admin"), (me.GetProperty("sub").GetString(), me.GetProperty("preferred_username").GetString()));
        Assert.Equal(HttpStatusCode.Unauthorized, (await SendAsync(server, HttpMethod.Get, "/api/auth:me", null, access)).Status);

        // 授权码重放：整族吊销
        Assert.Equal("invalid_grant", (await FormAsync(server, "/api/oauth2/token", new() { ["grant_type"] = "authorization_code", ["code"] = code, ["redirect_uri"] = Callback }, basic: "rp-app:" + secret)).Body.GetProperty("error").GetString());
        Assert.Equal(HttpStatusCode.Unauthorized, await UserInfoStatusAsync(server, access));

        // 同意过即不必确认；刷新令牌轮换，作废的再用即整族吊销
        Assert.True((await SendAsync(server, HttpMethod.Get, "/api/oauth-consents:resolve" + Query("rp-app"), null, tenant)).Body.GetProperty("skipConsent").GetBoolean());
        var (_, second) = await FormAsync(server, "/api/oauth2/token", new() { ["grant_type"] = "authorization_code", ["code"] = await CodeAsync(server, tenant, "rp-app"), ["redirect_uri"] = Callback }, basic: "rp-app:" + secret);
        var (_, rotated) = await FormAsync(server, "/api/oauth2/token", new() { ["grant_type"] = "refresh_token", ["refresh_token"] = second.GetProperty("refresh_token").GetString()! }, basic: "rp-app:" + secret);
        var rotatedAccess = rotated.GetProperty("access_token").GetString()!;
        Assert.False(rotated.TryGetProperty("id_token", out _));
        Assert.Equal(HttpStatusCode.OK, await UserInfoStatusAsync(server, rotatedAccess));
        Assert.Equal("invalid_grant", (await FormAsync(server, "/api/oauth2/token", new() { ["grant_type"] = "refresh_token", ["refresh_token"] = second.GetProperty("refresh_token").GetString()! }, basic: "rp-app:" + secret)).Body.GetProperty("error").GetString());
        Assert.Equal(HttpStatusCode.Unauthorized, await UserInfoStatusAsync(server, rotatedAccess));
        Assert.Equal("invalid_grant", (await FormAsync(server, "/api/oauth2/token", new() { ["grant_type"] = "refresh_token", ["refresh_token"] = refresh }, basic: "rp-app:" + secret)).Body.GetProperty("error").GetString());

        // 公开客户端：没有 verifier 不给，verifier 对了才给；撤销刷新令牌连同整族
        var verifier = Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(32));
        var challenge = Base64UrlEncoder.Encode(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)));
        var spaCode = await CodeAsync(server, tenant, "spa-app", challenge: challenge);
        Assert.Equal("invalid_grant", (await FormAsync(server, "/api/oauth2/token", new() { ["grant_type"] = "authorization_code", ["code"] = await CodeAsync(server, tenant, "spa-app", challenge: challenge), ["redirect_uri"] = Callback, ["client_id"] = "spa-app" })).Body.GetProperty("error").GetString());
        var (_, spaTokens) = await FormAsync(server, "/api/oauth2/token", new() { ["grant_type"] = "authorization_code", ["code"] = spaCode, ["redirect_uri"] = Callback, ["client_id"] = "spa-app", ["code_verifier"] = verifier });
        var spaAccess = spaTokens.GetProperty("access_token").GetString()!;
        Assert.Equal(HttpStatusCode.OK, await UserInfoStatusAsync(server, spaAccess));
        Assert.Equal(HttpStatusCode.OK, (await FormAsync(server, "/api/oauth2/revoke", new() { ["token"] = spaTokens.GetProperty("refresh_token").GetString()!, ["client_id"] = "spa-app" })).Response.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, await UserInfoStatusAsync(server, spaAccess));

        // 授权过的应用：列得出、撤销即令牌失效，再撤销即不存在
        var (_, third) = await FormAsync(server, "/api/oauth2/token", new() { ["grant_type"] = "authorization_code", ["code"] = await CodeAsync(server, tenant, "rp-app"), ["redirect_uri"] = Callback }, basic: "rp-app:" + secret);
        var (_, consents) = await SendAsync(server, HttpMethod.Get, "/api/oauth-consents", null, tenant);
        Assert.Equal(["rp-app", "spa-app"], consents.GetProperty("items").EnumerateArray().Select(static c => c.GetProperty("clientId").GetString()!).Order(StringComparer.Ordinal));
        Assert.True((await SendAsync(server, HttpMethod.Post, "/api/oauth-consents:revoke", """{"clientId":"rp-app"}""", tenant)).Body.GetProperty("revoked").GetBoolean());
        Assert.Equal(HttpStatusCode.Unauthorized, await UserInfoStatusAsync(server, third.GetProperty("access_token").GetString()!));
        Assert.Equal("OPENPLATFORM_NOTFOUND_002", (await SendAsync(server, HttpMethod.Post, "/api/oauth-consents:revoke", """{"clientId":"rp-app"}""", tenant)).Body.GetProperty("code").GetString());
        Assert.False((await SendAsync(server, HttpMethod.Get, "/api/oauth-consents:resolve" + Query("rp-app"), null, tenant)).Body.GetProperty("skipConsent").GetBoolean());
    }

    [Fact]
    public async Task SigningKeys_TakeOverAfterTheLead_AndOnlyRetireWhenNoLongerSigning()
    {
        var clock = new ManualClock(DateTimeOffset.UtcNow);
        var (server, platform, tenant) = await SettingsTestHost.StartAsync(_database.ConnectionString, configure: builder => builder.Services.AddSingleton<TimeProvider>(clock));
        await using var running = server;
        var app = new { appName = "RP", clientId = "rp-app", clientType = "Confidential", appType = "Web", grantTypes = new[] { "authorization_code" }, redirectUris = new[] { Callback }, scopes = new[] { "openid" }, accessTokenLifetime = 3600, refreshTokenLifetime = 86400, authorizationCodeLifetime = 300, skipConsent = true, isEnabled = true };
        var secret = (await SendAsync(server, HttpMethod.Post, "/api/oauth-apps", JsonSerializer.Serialize(app), platform)).Body.GetProperty("clientSecret").GetString()!;

        async Task<string> SignedByAsync()
        {
            var (_, tokens) = await FormAsync(server, "/api/oauth2/token", new() { ["grant_type"] = "authorization_code", ["code"] = await CodeAsync(server, tenant, "rp-app", scope: "openid"), ["redirect_uri"] = Callback }, basic: "rp-app:" + secret);
            return Segment(tokens.GetProperty("id_token").GetString()!, 0).GetProperty("kid").GetString()!;
        }

        async Task<string[]> PublishedAsync()
            => [.. (await SendAsync(server, HttpMethod.Get, "/api/oauth2/jwks", null, null)).Body.GetProperty("keys").EnumerateArray().Select(static k => k.GetProperty("kid").GetString()!)];

        Task<(HttpStatusCode Status, JsonElement Body)> KeysAsync(string path = "")
            => path.Length == 0 ? SendAsync(server, HttpMethod.Get, "/api/oauth-signing-keys", null, platform) : SendAsync(server, HttpMethod.Post, "/api/oauth-signing-keys" + path, "{}", platform);

        static Dictionary<string, string> Statuses(JsonElement list)
            => list.GetProperty("items").EnumerateArray().ToDictionary(static k => k.GetProperty("keyId").GetString()!, static k => k.GetProperty("status").GetString()!);

        static JsonElement Key(JsonElement list, string kid)
            => list.GetProperty("items").EnumerateArray().Single(k => k.GetProperty("keyId").GetString() == kid);

        // 第一次用到自动生成、即刻签名；正在签名的不能停用；租户一侧看不到
        var first = await SignedByAsync();
        var (_, listed) = await KeysAsync();
        Assert.Equal(new Dictionary<string, string> { [first] = "Signing" }, Statuses(listed));
        Assert.Equal(900, listed.GetProperty("rotationLeadSeconds").GetInt32());
        var (inUse, inUseBody) = await KeysAsync($"/{Key(listed, first).GetProperty("id").GetString()}:retire");
        Assert.Equal((HttpStatusCode.Conflict, "OPENPLATFORM_CONFLICT_005"), (inUse, inUseBody.GetProperty("code").GetString()));
        Assert.Equal(HttpStatusCode.Forbidden, (await SendAsync(server, HttpMethod.Get, "/api/oauth-signing-keys", null, tenant)).Status);

        // 轮换：新密钥即刻公布，到点才接替签名；待接替时不能再轮换
        var (rotatedStatus, rotated) = await KeysAsync(":rotate");
        Assert.Equal(HttpStatusCode.OK, rotatedStatus);
        var second = Statuses(rotated).Single(static p => p.Value == "Pending").Key;
        Assert.Equal(clock.GetUtcNow().AddMinutes(15), Key(rotated, second).GetProperty("signsFrom").GetDateTimeOffset());
        Assert.Equal([first, second], await PublishedAsync());
        Assert.Equal(first, await SignedByAsync());
        Assert.Equal("OPENPLATFORM_CONFLICT_004", (await KeysAsync(":rotate")).Body.GetProperty("code").GetString());

        clock.Set(clock.GetUtcNow().AddMinutes(15));
        Assert.Equal(second, await SignedByAsync());
        (_, listed) = await KeysAsync();
        Assert.Equal(new Dictionary<string, string> { [first] = "Retained", [second] = "Signing" }, Statuses(listed));

        // 停用被接替的那把：不再公布；再停用原样返回；不存在即 404
        var firstId = Key(listed, first).GetProperty("id").GetString();
        var (retiredStatus, retired) = await KeysAsync($"/{firstId}:retire");
        Assert.Equal(HttpStatusCode.OK, retiredStatus);
        Assert.Equal(("Retired", clock.GetUtcNow()), (Key(retired, first).GetProperty("status").GetString(), Key(retired, first).GetProperty("retiredAt").GetDateTimeOffset()));
        Assert.Equal([second], await PublishedAsync());
        Assert.Equal(HttpStatusCode.OK, (await KeysAsync($"/{firstId}:retire")).Status);
        var (missing, missingBody) = await KeysAsync($"/{Guid.NewGuid()}:retire");
        Assert.Equal((HttpStatusCode.NotFound, "OPENPLATFORM_NOTFOUND_003"), (missing, missingBody.GetProperty("code").GetString()));

        // 停用待接替的即取消这次轮换：到点仍是原来那把签名
        var (_, again) = await KeysAsync(":rotate");
        var third = Statuses(again).Single(static p => p.Value == "Pending").Key;
        Assert.Equal(HttpStatusCode.OK, (await KeysAsync($"/{Key(again, third).GetProperty("id").GetString()}:retire")).Status);
        clock.Set(clock.GetUtcNow().AddMinutes(15));
        Assert.Equal(second, await SignedByAsync());
        Assert.Equal([second], await PublishedAsync());
    }
}
