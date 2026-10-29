// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Net;
using System.Text.Json;
using XiHan.BasicApp.Integration.Tests.Infrastructure;

namespace XiHan.BasicApp.Integration.Tests;

/// <summary>
/// OAuth 应用：平台「应用管理」建机密 / 公开客户端（密钥只返回一次、公开客户端没有密钥也不能重置），ClientId 全局唯一，授权类型、范围、回调地址与主页按规则校验；
/// 个人中心「我的应用」只看得到、改得了本人的，授权类型与范围固定、改资料不动它们；平台列表看得到用户建的；租户管理员没有平台的应用管理权限。
/// </summary>
public sealed class OAuthAppsTests : IDisposable
{
    private static readonly string[] _toolCallback = ["https://tool.example.com/cb"];
    private static readonly string[] _toolCallback2 = ["https://tool.example.com/cb2"];
    private readonly SqliteDatabase _database = new();

    public void Dispose() => _database.Dispose();

    private static async Task<(HttpStatusCode Status, JsonElement Body)> SendAsync(BasicAppTestServer server, HttpMethod method, string path, object? body, string token)
    {
        var json = body switch
        {
            null => null,
            string text => text,
            _ => JsonSerializer.Serialize(body),
        };
        var (response, payload) = await server.SendAsync(BasicAppTestServer.Request(method, path, json, token));
        return (response.StatusCode, payload);
    }

    private static string? Code(JsonElement body) => body.ValueKind == JsonValueKind.Object && body.TryGetProperty("code", out var code) ? code.GetString() : null;

    private static object App(string name, string? clientId = null, string clientType = "Confidential", string[]? grants = null, string[]? scopes = null, string[]? redirects = null, string? homepage = null) => new
    {
        appName = name,
        appDescription = (string?)null,
        clientId,
        clientType,
        appType = "Web",
        grantTypes = grants ?? ["authorization_code", "refresh_token"],
        redirectUris = redirects ?? ["https://app.example.com/callback"],
        scopes = scopes ?? ["openid", "profile"],
        accessTokenLifetime = 3600,
        refreshTokenLifetime = 2592000,
        authorizationCodeLifetime = 300,
        logo = (string?)null,
        homepage,
        skipConsent = false,
        isEnabled = true,
        remark = (string?)null,
    };

    [Fact]
    public async Task OAuthApps_PlatformManagesAll_AndUsersOnlyTheirOwn()
    {
        var (server, platform, tenant) = await SettingsTestHost.StartAsync(_database.ConnectionString);
        await using var running = server;

        // 机密客户端返回 64 位十六进制密钥；公开客户端没有密钥；ClientId 全局唯一、不填即生成
        var (created, confidential) = await SendAsync(server, HttpMethod.Post, "/api/oauth-apps", App("Wiki", clientId: "wiki-app"), platform);
        Assert.Equal(HttpStatusCode.Created, created);
        Assert.Equal(("wiki-app", "Confidential", 64), (confidential.GetProperty("clientId").GetString(), confidential.GetProperty("clientType").GetString(), confidential.GetProperty("clientSecret").GetString()!.Length));
        var (_, spa) = await SendAsync(server, HttpMethod.Post, "/api/oauth-apps", App("SPA", clientType: "Public", redirects: ["http://localhost:5173/callback", "com.example.app:/oauth"]), platform);
        Assert.Equal((32, JsonValueKind.Null), (spa.GetProperty("clientId").GetString()!.Length, spa.GetProperty("clientSecret").ValueKind));
        Assert.Equal("OPENPLATFORM_CONFLICT_002", Code((await SendAsync(server, HttpMethod.Post, "/api/oauth-apps", App("Dup", clientId: "wiki-app"), platform)).Body));

        // 规则：授权类型只有授权码（必选）与刷新令牌，范围只有身份类（须带 openid），回调地址绝对、不带片段、http 只限本机，主页须是 http(s)，ClientId 字符集
        Assert.Equal("OPENPLATFORM_VALIDATION_009", Code((await SendAsync(server, HttpMethod.Post, "/api/oauth-apps", App("X", grants: ["client_credentials"]), platform)).Body));
        Assert.Equal("OPENPLATFORM_VALIDATION_009", Code((await SendAsync(server, HttpMethod.Post, "/api/oauth-apps", App("X", grants: ["refresh_token"]), platform)).Body));
        Assert.Equal("OPENPLATFORM_VALIDATION_010", Code((await SendAsync(server, HttpMethod.Post, "/api/oauth-apps", App("X", scopes: ["openid", "basicapp"]), platform)).Body));
        Assert.Equal("OPENPLATFORM_VALIDATION_010", Code((await SendAsync(server, HttpMethod.Post, "/api/oauth-apps", App("X", scopes: ["profile"]), platform)).Body));
        Assert.Equal("OPENPLATFORM_VALIDATION_011", Code((await SendAsync(server, HttpMethod.Post, "/api/oauth-apps", App("X", redirects: ["http://evil.example.com/cb"]), platform)).Body));
        Assert.Equal("OPENPLATFORM_VALIDATION_011", Code((await SendAsync(server, HttpMethod.Post, "/api/oauth-apps", App("X", redirects: ["https://app.example.com/cb#frag"]), platform)).Body));
        Assert.Equal("OPENPLATFORM_VALIDATION_011", Code((await SendAsync(server, HttpMethod.Post, "/api/oauth-apps", App("X", redirects: ["javascript:alert(1)"]), platform)).Body));
        Assert.Equal("OPENPLATFORM_VALIDATION_012", Code((await SendAsync(server, HttpMethod.Post, "/api/oauth-apps", App("X", homepage: "ftp://example.com"), platform)).Body));
        Assert.Equal("OPENPLATFORM_VALIDATION_008", Code((await SendAsync(server, HttpMethod.Post, "/api/oauth-apps", App("X", clientId: "has space"), platform)).Body));

        // 修改整体覆盖（去重、范围转小写）；重置密钥只给机密客户端
        var wikiId = confidential.GetProperty("id").GetString()!;
        var update = JsonSerializer.Serialize(App("Wiki 2", scopes: ["OPENID", "email", "email"])).Replace("\"clientId\":null,", string.Empty, StringComparison.Ordinal).Replace("\"clientType\":\"Confidential\",", string.Empty, StringComparison.Ordinal);
        var (_, updated) = await SendAsync(server, HttpMethod.Put, $"/api/oauth-apps/{wikiId}", update, platform);
        Assert.Equal(("Wiki 2", "wiki-app", "openid email"), (updated.GetProperty("appName").GetString(), updated.GetProperty("clientId").GetString(), string.Join(' ', updated.GetProperty("scopes").EnumerateArray().Select(static s => s.GetString()))));
        var (_, reset) = await SendAsync(server, HttpMethod.Post, $"/api/oauth-apps/{wikiId}:reset-secret", "{}", platform);
        Assert.NotEqual(confidential.GetProperty("clientSecret").GetString(), reset.GetProperty("clientSecret").GetString());
        Assert.Equal("OPENPLATFORM_CONFLICT_003", Code((await SendAsync(server, HttpMethod.Post, $"/api/oauth-apps/{spa.GetProperty("id").GetString()}:reset-secret", "{}", platform)).Body));

        // 我的应用：租户管理员建的只有自己看得到；授权类型与范围固定；平台的列表看得到它与归属人
        var (myCreated, mine) = await SendAsync(server, HttpMethod.Post, "/api/my-oauth-apps", new { appName = "My Tool", clientType = "Public", homepage = "https://tool.example.com", appDescription = "x", redirectUris = _toolCallback, logo = (string?)null }, tenant);
        Assert.Equal((HttpStatusCode.Created, JsonValueKind.Null), (myCreated, mine.GetProperty("clientSecret").ValueKind));
        var mineId = mine.GetProperty("id").GetString()!;
        var (_, myList) = await SendAsync(server, HttpMethod.Get, "/api/my-oauth-apps", null, tenant);
        var item = Assert.Single(myList.GetProperty("items").EnumerateArray());
        Assert.Equal(("authorization_code refresh_token", "openid profile email", "admin"), (string.Join(' ', item.GetProperty("grantTypes").EnumerateArray().Select(static g => g.GetString())), string.Join(' ', item.GetProperty("scopes").EnumerateArray().Select(static s => s.GetString())), item.GetProperty("ownerUserName").GetString()));
        Assert.Empty((await SendAsync(server, HttpMethod.Get, "/api/my-oauth-apps", null, platform)).Body.GetProperty("items").EnumerateArray());
        Assert.Equal("OPENPLATFORM_NOTFOUND_001", Code((await SendAsync(server, HttpMethod.Get, $"/api/my-oauth-apps/{mineId}", null, platform)).Body));
        Assert.Equal("OPENPLATFORM_NOTFOUND_001", Code((await SendAsync(server, HttpMethod.Get, $"/api/my-oauth-apps/{wikiId}", null, tenant)).Body));
        var (_, renamed) = await SendAsync(server, HttpMethod.Put, $"/api/my-oauth-apps/{mineId}", new { appName = "My Tool 2", homepage = (string?)null, appDescription = (string?)null, redirectUris = _toolCallback2, logo = (string?)null, isEnabled = false }, tenant);
        Assert.Equal(("My Tool 2", false, "openid profile email"), (renamed.GetProperty("appName").GetString(), renamed.GetProperty("isEnabled").GetBoolean(), string.Join(' ', renamed.GetProperty("scopes").EnumerateArray().Select(static s => s.GetString()))));
        Assert.Equal("OPENPLATFORM_CONFLICT_003", Code((await SendAsync(server, HttpMethod.Post, $"/api/my-oauth-apps/{mineId}:reset-secret", "{}", tenant)).Body));

        var (_, page) = await SendAsync(server, HttpMethod.Post, "/api/oauth-apps:list", """{"page":{"index":1,"size":50},"filters":[{"field":"personal","operator":"eq","value":"true"}]}""", platform);
        Assert.Equal(("My Tool 2", "admin"), (Assert.Single(page.GetProperty("items").EnumerateArray()).GetProperty("appName").GetString(), page.GetProperty("items")[0].GetProperty("ownerUserName").GetString()));
        var (_, publics) = await SendAsync(server, HttpMethod.Post, "/api/oauth-apps:list", """{"page":{"index":1,"size":50},"keyword":"SPA","filters":[{"field":"clientType","operator":"eq","value":"Public"}]}""", platform);
        Assert.Equal(1, publics.GetProperty("total").GetInt32());
        Assert.Equal("OPENPLATFORM_VALIDATION_013", Code((await SendAsync(server, HttpMethod.Post, "/api/oauth-apps:list", """{"page":{"index":1,"size":50},"filters":[{"field":"secret","operator":"eq","value":"x"}]}""", platform)).Body));

        // 租户管理员没有平台的应用管理权限；我的应用只删本人的
        Assert.Equal(HttpStatusCode.Forbidden, (await SendAsync(server, HttpMethod.Post, "/api/oauth-apps:list", """{"page":{"index":1,"size":50}}""", tenant)).Status);
        var (_, denied) = await SendAsync(server, HttpMethod.Delete, $"/api/my-oauth-apps?ids={wikiId}", null, tenant);
        Assert.Equal("OPENPLATFORM_NOTFOUND_001", Assert.Single(denied.GetProperty("failed").EnumerateArray()).GetProperty("code").GetString());
        var (_, removed) = await SendAsync(server, HttpMethod.Delete, $"/api/my-oauth-apps?ids={mineId}", null, tenant);
        Assert.Equal(mineId, Assert.Single(removed.GetProperty("succeeded").EnumerateArray()).GetString());
        var (_, deleted) = await SendAsync(server, HttpMethod.Delete, $"/api/oauth-apps?ids={wikiId}", null, platform);
        Assert.Single(deleted.GetProperty("succeeded").EnumerateArray());
        Assert.Equal("OPENPLATFORM_NOTFOUND_001", Code((await SendAsync(server, HttpMethod.Get, $"/api/oauth-apps/{wikiId}", null, platform)).Body));
    }
}
