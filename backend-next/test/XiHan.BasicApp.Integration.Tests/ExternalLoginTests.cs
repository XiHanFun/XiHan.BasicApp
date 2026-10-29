// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.Identity.Infrastructure.External;
using XiHan.BasicApp.Integration.Tests.Infrastructure;

namespace XiHan.BasicApp.Integration.Tests;

/// <summary>
/// 验证第三方登录与账号绑定（假的 GitHub）。
/// </summary>
/// <remarks>
/// 登录页列出配置的提供商；发起即带 state 跳去授权页并种 Cookie；回调 state 与 Cookie 都对上才换身份、发一次性票据（用户拒绝、Cookie 不对都带错误回前端）；
/// 没绑定的不能登录（拍板，不自动建号）；绑定要本人先领意图才能发起、票据只能由本人确认，票据分登录与绑定、只能用一次；绑定按注册地分开；绑定后能登录、资料随登录刷新；解绑后又不能登录。
/// </remarks>
public sealed class ExternalLoginTests : IDisposable
{
    private readonly SqliteDatabase _database = new();
    private readonly FakeGitHub _github = new();

    public void Dispose() => _database.Dispose();

    private Task<(BasicAppTestServer Server, string Platform, string Tenant)> StartAsync()
        => SettingsTestHost.StartAsync(_database.ConnectionString, configure: builder =>
        {
            builder.Configuration["BasicApp:Auth:OAuth:Providers:0:Name"] = "github";
            builder.Configuration["BasicApp:Auth:OAuth:Providers:0:DisplayName"] = "GitHub";
            builder.Configuration["BasicApp:Auth:OAuth:Providers:0:ClientId"] = "gh-client";
            builder.Configuration["BasicApp:Auth:OAuth:Providers:0:ClientSecret"] = "gh-secret";
            builder.Configuration["BasicApp:Auth:OAuth:Providers:1:Name"] = "placeholder";
            builder.Configuration["BasicApp:Auth:OAuth:Providers:1:Provider"] = "gitee";
            builder.Services.AddHttpClient(ExternalLoginFlow.HttpClientName).ConfigurePrimaryHttpMessageHandler(() => _github);
        });

    /// <summary>
    /// 走一遍发起与回调，返回前端回调页拿到的查询参数。
    /// </summary>
    private static async Task<Dictionary<string, string>> RoundTripAsync(BasicAppTestServer server, string query, bool keepCookie = true, string code = "c-1")
    {
        var (challenge, _) = await server.SendAsync(new HttpRequestMessage(HttpMethod.Get, "/api/oauth/github/challenge" + query));
        Assert.Equal(HttpStatusCode.Redirect, challenge.StatusCode);
        var authorize = challenge.Headers.Location!;
        Assert.Equal("https://github.com/login/oauth/authorize", authorize.GetLeftPart(UriPartial.Path));
        var parameters = QueryHelpers.ParseQuery(authorize.Query);
        Assert.Equal(("gh-client", "http://localhost/api/oauth/github/callback"), (parameters["client_id"].ToString(), parameters["redirect_uri"].ToString()));
        var cookie = challenge.Headers.GetValues("Set-Cookie").Single(static c => c.StartsWith(ExternalLoginFlow.CookieName + "=", StringComparison.Ordinal)).Split(';')[0];
        Assert.Contains("httponly", challenge.Headers.GetValues("Set-Cookie").Single(), StringComparison.OrdinalIgnoreCase);

        var callback = new HttpRequestMessage(HttpMethod.Get, $"/api/oauth/github/callback?code={code}&state={Uri.EscapeDataString(parameters["state"].ToString())}");
        if (keepCookie)
        {
            callback.Headers.Add("Cookie", cookie);
        }

        var (back, _) = await server.SendAsync(callback);
        Assert.Equal(HttpStatusCode.Redirect, back.StatusCode);
        var target = back.Headers.Location!.OriginalString;
        Assert.StartsWith("/#/auth/oauth-callback?", target, StringComparison.Ordinal);
        return QueryHelpers.ParseQuery(target[target.IndexOf('?', StringComparison.Ordinal)..]).ToDictionary(static p => p.Key, static p => p.Value.ToString());
    }

    /// <summary>
    /// 领一个绑定意图，返回发起地址的查询串。
    /// </summary>
    private static async Task<string> BindQueryAsync(BasicAppTestServer server, string token)
    {
        var (status, body) = await PostAsync(server, "/api/profile:begin-external-bind", new { provider = "github" }, token);
        Assert.Equal(HttpStatusCode.OK, status);
        var url = body.GetProperty("url").GetString()!;
        Assert.StartsWith("/api/oauth/github/challenge?mode=bind&intent=", url, StringComparison.Ordinal);
        return url[url.IndexOf('?', StringComparison.Ordinal)..];
    }

    private static async Task<(HttpStatusCode Status, JsonElement Body)> PostAsync(BasicAppTestServer server, string path, object body, string? token = null)
    {
        var (response, payload) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, path, JsonSerializer.Serialize(body), token));
        return (response.StatusCode, payload);
    }

    [Fact]
    public async Task ThirdPartyLogin_OnlyForBoundAccounts_AndBindingIsStartedAndConfirmedByTheSameUser()
    {
        var (server, platform, tenant) = await StartAsync();
        await using var running = server;

        // 登录页只列配置里启用且填了 ClientId 的（没填 ClientId 的 placeholder 不列）
        var (_, config) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/api/auth:login-config"));
        Assert.Equal([("github", "github")], config.GetProperty("oAuthProviders").EnumerateArray().Select(static p => (p.GetProperty("name").GetString()!, p.GetProperty("type").GetString()!)));

        // 没绑定：拒绝（不自动建号）；票据只能用一次
        var login = await RoundTripAsync(server, "?mode=login&tenant=contoso");
        Assert.Equal(("login", "github"), (login["mode"], login["provider"]));
        var (unbound, unboundBody) = await PostAsync(server, "/api/auth:external-login", new { ticket = login["ticket"] });
        Assert.Equal((HttpStatusCode.Forbidden, "IDENTITY_DENIED_015"), (unbound, unboundBody.GetProperty("code").GetString()));
        Assert.Equal("IDENTITY_VALIDATION_015", (await PostAsync(server, "/api/auth:external-login", new { ticket = login["ticket"] })).Body.GetProperty("code").GetString());

        // 回调要 state 与发起时的 Cookie 都对上；用户在第三方拒绝也带错误回前端
        Assert.Equal("invalid_state", (await RoundTripAsync(server, "?mode=login&tenant=contoso", keepCookie: false))["error"]);
        var (unknown, _) = await server.SendAsync(new HttpRequestMessage(HttpMethod.Get, "/api/oauth/nobody/challenge"));
        Assert.Contains("error=invalid_provider", unknown.Headers.Location!.OriginalString, StringComparison.Ordinal);
        _github.FailToken = true;
        Assert.Equal("external_auth_failed", (await RoundTripAsync(server, "?mode=login&tenant=contoso"))["error"]);
        _github.FailToken = false;

        // 绑定：没有本人领的意图不能发起，意图只用一次；不认识的提供方即不存在
        var (unsigned, _) = await server.SendAsync(new HttpRequestMessage(HttpMethod.Get, "/api/oauth/github/challenge?mode=bind"));
        Assert.Contains("error=invalid_state", unsigned.Headers.Location!.OriginalString, StringComparison.Ordinal);
        Assert.Equal("IDENTITY_NOTFOUND_005", (await PostAsync(server, "/api/profile:begin-external-bind", new { provider = "nobody" }, tenant)).Body.GetProperty("code").GetString());
        var bindQuery = await BindQueryAsync(server, tenant);
        var bind = await RoundTripAsync(server, bindQuery);
        var (reused, _) = await server.SendAsync(new HttpRequestMessage(HttpMethod.Get, "/api/oauth/github/challenge" + bindQuery));
        Assert.Contains("error=invalid_state", reused.Headers.Location!.OriginalString, StringComparison.Ordinal);

        // 确认要发起的本人：别人完成的回调票据（攻击者把自己的第三方账号塞给受害者）绑不上；登录用的票据也不能拿来绑定
        Assert.Equal("IDENTITY_VALIDATION_015", (await PostAsync(server, "/api/profile:link-external", new { ticket = (await RoundTripAsync(server, await BindQueryAsync(server, tenant)))["ticket"] }, platform)).Body.GetProperty("code").GetString());
        Assert.Equal("IDENTITY_VALIDATION_015", (await PostAsync(server, "/api/profile:link-external", new { ticket = (await RoundTripAsync(server, "?mode=login&tenant=contoso"))["ticket"] }, tenant)).Body.GetProperty("code").GetString());
        var (linked, linkedBody) = await PostAsync(server, "/api/profile:link-external", new { ticket = bind["ticket"] }, tenant);
        Assert.Equal((HttpStatusCode.OK, "github", "octo"), (linked, linkedBody.GetProperty("provider").GetString(), linkedBody.GetProperty("displayName").GetString()));

        // 绑定后能登录（资料随登录刷新）；拿到的令牌是本系统的会话
        _github.Login = "octo-renamed";
        var (signedIn, tokens) = await PostAsync(server, "/api/auth:external-login", new { ticket = (await RoundTripAsync(server, "?mode=login&tenant=contoso"))["ticket"] });
        Assert.Equal(HttpStatusCode.OK, signedIn);
        var access = tokens.GetProperty("token").GetProperty("accessToken").GetString()!;
        var (me, meBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/api/auth:me", token: access));
        Assert.Equal((HttpStatusCode.OK, "admin"), (me.StatusCode, meBody.GetProperty("user").GetProperty("userName").GetString()));
        var (_, listed) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/api/profile:external-logins", token: tenant));
        var item = Assert.Single(listed.GetProperty("items").EnumerateArray());
        Assert.Equal(("octo-renamed", JsonValueKind.String), (item.GetProperty("displayName").GetString(), item.GetProperty("lastLoginAt").ValueKind));

        // 绑定按注册地分开：平台账号可以绑同一个第三方账号，平台登录（不选租户）进的是平台账号
        Assert.Equal(HttpStatusCode.OK, (await PostAsync(server, "/api/profile:link-external", new { ticket = (await RoundTripAsync(server, await BindQueryAsync(server, platform)))["ticket"] }, platform)).Status);
        var (_, platformTokens) = await PostAsync(server, "/api/auth:external-login", new { ticket = (await RoundTripAsync(server, "?mode=login"))["ticket"] });
        var (_, platformMe) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/api/auth:me", token: platformTokens.GetProperty("token").GetProperty("accessToken").GetString()));
        Assert.Equal("superadmin", platformMe.GetProperty("user").GetProperty("userName").GetString());

        // 解绑后又不能登录；没绑过的解绑即不存在
        Assert.Empty((await PostAsync(server, "/api/profile:unlink-external", new { provider = "github" }, tenant)).Body.GetProperty("items").EnumerateArray());
        Assert.Equal("IDENTITY_NOTFOUND_004", (await PostAsync(server, "/api/profile:unlink-external", new { provider = "github" }, tenant)).Body.GetProperty("code").GetString());
        Assert.Equal("IDENTITY_DENIED_015", (await PostAsync(server, "/api/auth:external-login", new { ticket = (await RoundTripAsync(server, "?mode=login&tenant=contoso"))["ticket"] })).Body.GetProperty("code").GetString());
    }

    /// <summary>
    /// 假的 GitHub：换码与取用户。
    /// </summary>
    private sealed class FakeGitHub : HttpMessageHandler
    {
        public bool FailToken { get; set; }

        public string Login { get; set; } = "octo";

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var url = request.RequestUri!.GetLeftPart(UriPartial.Path);
            if (url == "https://github.com/login/oauth/access_token")
            {
                var form = await request.Content!.ReadAsStringAsync(cancellationToken);
                Assert.Contains("client_secret=gh-secret", form, StringComparison.Ordinal);
                return Json(FailToken ? """{"error":"bad_verification_code"}""" : """{"access_token":"gh-token","token_type":"bearer"}""");
            }

            if (url == "https://api.github.com/user")
            {
                Assert.Equal("gh-token", request.Headers.Authorization?.Parameter);
                Assert.NotEmpty(request.Headers.UserAgent);
                return Json($$"""{"id":4242,"login":"{{Login}}","email":"octo@example.com","avatar_url":"https://avatars.example.com/4242"}""");
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        }

        private static HttpResponseMessage Json(string body) => new(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
    }
}
