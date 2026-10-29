// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.Auditing.Domain.Services;
using XiHan.BasicApp.Integration.Tests.Infrastructure;

namespace XiHan.BasicApp.Integration.Tests;

/// <summary>
/// 验证签名开放接口。
/// </summary>
/// <remarks>
/// 沿用 4.x 的请求头与规范串。
/// 个人中心自助建凭证；签名对了自检通过，重放、不签名、签名 / 内容签名不对、时间窗外都拒；
/// 加密的请求体解密后绑定、响应随之加密，明文请求可以要求加密响应；调用安全设置（算法与 IP 白名单）立即生效；停用、滚动、删除立即生效；
/// 凭证上限 5 个；无论成败都记开放接口日志（签名是否有效、明文摘要），验签通过即记下最近使用时间。
/// </remarks>
public sealed class OpenApiSigningTests : IDisposable
{
    private const string ClientIpHeader = "X-Test-Client-Ip";
    private static readonly string[] _normalizedAllowList = ["10.0.0.0/24", "192.168.1.9"];
    private readonly SqliteDatabase _database = new();

    public void Dispose() => _database.Dispose();

    private sealed record Credential(string Id, string AppKey, string Secret);

    private static async Task<BasicAppTestServer> StartAsync(SqliteDatabase database)
    {
        var server = await BasicAppTestServer.StartAsync(database.ConnectionString, configureApp: app => app.Use(async (http, next) =>
        {
            if (http.Request.Headers.TryGetValue(ClientIpHeader, out var ip))
            {
                http.Connection.RemoteIpAddress = IPAddress.Parse(ip.ToString());
            }

            await next();
        }));
        await server.SeedDefaultsAsync();
        await server.RunSeedersAsync();
        return server;
    }

    private static async Task<(HttpStatusCode Status, JsonElement Body)> SendAsync(BasicAppTestServer server, HttpMethod method, string path, string? json, string token)
    {
        var (response, body) = await server.SendAsync(BasicAppTestServer.Request(method, path, json, token));
        return (response.StatusCode, body);
    }

    private static async Task<Credential> CreateAsync(BasicAppTestServer server, string token, string? name = null)
    {
        var (status, body) = await SendAsync(server, HttpMethod.Post, "/api/my-api-credentials", JsonSerializer.Serialize(new { name }), token);
        Assert.Equal(HttpStatusCode.Created, status);
        return new Credential(body.GetProperty("id").GetString()!, body.GetProperty("appKey").GetString()!, body.GetProperty("secret").GetString()!);
    }

    /// <summary>
    /// 照接入文档独立算签名（不借用服务端的实现）。
    /// </summary>
    private static HttpRequestMessage Signed(HttpMethod method, string path, Credential credential, byte[]? plainBody = null, string algorithm = "HmacSHA256", long? timestamp = null, string? nonce = null, bool encrypt = false, string? ip = null)
    {
        var plain = plainBody ?? [];
        var ts = (timestamp ?? DateTimeOffset.UtcNow.ToUnixTimeSeconds()).ToString(CultureInfo.InvariantCulture);
        var n = nonce ?? Guid.NewGuid().ToString("N");
        var contentSign = Convert.ToHexStringLower(SHA256.HashData(plain));
        var uri = new Uri("http://localhost" + path);
        var query = string.Join('&', System.Web.HttpUtility.ParseQueryString(uri.Query).AllKeys
            .Where(static k => k is not null).Order(StringComparer.Ordinal)
            .Select(k => Uri.EscapeDataString(k!) + "=" + Uri.EscapeDataString(System.Web.HttpUtility.ParseQueryString(uri.Query)[k]!)));
        var canonical = string.Join('\n', method.Method.ToUpperInvariant(), uri.AbsolutePath, query, contentSign, ts, n);
        var key = Encoding.UTF8.GetBytes(credential.Secret);
        var data = Encoding.UTF8.GetBytes(canonical);
        var signature = Convert.ToHexString(algorithm == "HmacSHA512" ? HMACSHA512.HashData(key, data) : HMACSHA256.HashData(key, data));

        var request = new HttpRequestMessage(method, path);
        if (plain.Length > 0 || method == HttpMethod.Post)
        {
            var body = encrypt
                ? Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { data = Convert.ToBase64String(Encrypt(credential.Secret, plain, SHA256.HashData(Encoding.UTF8.GetBytes(n))[..16])) }))
                : plain;
            request.Content = new ByteArrayContent(body);
            request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
        }

        request.Headers.Add("X-Access-Key", credential.AppKey);
        request.Headers.Add("X-Timestamp", ts);
        request.Headers.Add("X-Nonce", n);
        request.Headers.Add("X-Content-Sign", contentSign);
        request.Headers.Add("X-Signature", signature);
        if (encrypt)
        {
            request.Headers.Add("X-Encrypt-Algorithm", "AES-CBC");
        }

        if (ip is not null)
        {
            request.Headers.Add(ClientIpHeader, ip);
        }

        return request;
    }

    private static byte[] KeyOf(string secret) => SHA256.HashData(Encoding.UTF8.GetBytes(secret));

    private static byte[] Encrypt(string secret, byte[] plain, byte[] iv)
    {
        using var aes = Aes.Create();
        aes.Key = KeyOf(secret);
        return aes.EncryptCbc(plain, iv, PaddingMode.PKCS7);
    }

    private static JsonElement DecryptEnvelope(string secret, JsonElement envelope)
    {
        Assert.Equal("AES-CBC", envelope.GetProperty("alg").GetString());
        using var aes = Aes.Create();
        aes.Key = KeyOf(secret);
        var plain = aes.DecryptCbc(Convert.FromBase64String(envelope.GetProperty("data").GetString()!), Convert.FromBase64String(envelope.GetProperty("iv").GetString()!), PaddingMode.PKCS7);
        Assert.Equal(Convert.ToHexStringLower(SHA256.HashData(plain)), envelope.GetProperty("contentSign").GetString());
        return JsonDocument.Parse(plain).RootElement.Clone();
    }

    private static string? Code(JsonElement body) => body.ValueKind == JsonValueKind.Object && body.TryGetProperty("code", out var code) ? code.GetString() : null;

    [Fact]
    public async Task SignedOpenApis_VerifyEncryptAndLog()
    {
        var server = await StartAsync(_database);
        await using var running = server;
        var token = (await AuthFlowTests.LoginAsync(server, "superadmin", "SuperAdmin@123")).GetProperty("accessToken").GetString()!;
        var credential = await CreateAsync(server, token);
        var (_, listed) = await SendAsync(server, HttpMethod.Get, "/api/my-api-credentials", null, token);
        var item = Assert.Single(listed.GetProperty("items").EnumerateArray());
        Assert.Equal((credential.AppKey, true, JsonValueKind.Null), (item.GetProperty("appKey").GetString(), item.GetProperty("isEnabled").GetBoolean(), item.GetProperty("lastUsedAt").ValueKind));
        Assert.False(item.TryGetProperty("secret", out _));
        Assert.StartsWith("ak_", credential.AppKey, StringComparison.Ordinal);
        Assert.Equal(46, credential.Secret.Length);

        // 签名对了即通过；同一个随机串再来一次是重放；不签名、签名不对、内容签名不对、时间窗外都拒
        var ping = Signed(HttpMethod.Get, "/api/open:ping?b=2&a=1", credential, nonce: "fixed-nonce-1");
        var replay = Signed(HttpMethod.Get, "/api/open:ping?b=2&a=1", credential, nonce: "fixed-nonce-1");
        var (okResponse, ok) = await server.SendAsync(ping);
        Assert.Equal(HttpStatusCode.OK, okResponse.StatusCode);
        Assert.Equal((true, credential.AppKey), (ok.GetProperty("ok").GetBoolean(), ok.GetProperty("appKey").GetString()));
        var (replayed, replayBody) = await server.SendAsync(replay);
        Assert.Equal((HttpStatusCode.Conflict, "OPENPLATFORM_CONFLICT_001"), (replayed.StatusCode, Code(replayBody)));

        var (unsigned, unsignedBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/api/open:ping"));
        Assert.Equal((HttpStatusCode.Forbidden, "OPENPLATFORM_DENIED_001"), (unsigned.StatusCode, Code(unsignedBody)));
        var forged = Signed(HttpMethod.Get, "/api/open:ping", credential with { Secret = "sk_wrong" });
        Assert.Equal("OPENPLATFORM_DENIED_007", Code((await server.SendAsync(forged)).Body));
        var tampered = Signed(HttpMethod.Post, "/api/open:echo", credential, """{"message":"hi"}"""u8.ToArray());
        tampered.Content = new StringContent("""{"message":"changed"}""", Encoding.UTF8, "application/json");
        Assert.Equal("OPENPLATFORM_DENIED_006", Code((await server.SendAsync(tampered)).Body));
        var stale = Signed(HttpMethod.Get, "/api/open:ping", credential, timestamp: DateTimeOffset.UtcNow.AddMinutes(-10).ToUnixTimeSeconds());
        Assert.Equal("OPENPLATFORM_DENIED_002", Code((await server.SendAsync(stale)).Body));

        // 加密的请求体解密后绑定，响应随之加密；明文请求可以要求加密响应；不要求即明文
        var (encryptedResponse, envelope) = await server.SendAsync(Signed(HttpMethod.Post, "/api/open:echo", credential, """{"message":"secret hello","number":7}"""u8.ToArray(), encrypt: true));
        Assert.Equal(HttpStatusCode.OK, encryptedResponse.StatusCode);
        Assert.Equal("1", Assert.Single(encryptedResponse.Headers.GetValues("X-Secure-Response")));
        var echoed = DecryptEnvelope(credential.Secret, envelope);
        Assert.Equal(("secret hello", 7), (echoed.GetProperty("echo").GetProperty("message").GetString(), echoed.GetProperty("echo").GetProperty("number").GetInt32()));
        var askEncrypted = Signed(HttpMethod.Post, "/api/open:echo", credential, """{"message":"plain"}"""u8.ToArray());
        askEncrypted.Headers.Add("X-Encrypt-Response", "1");
        var (_, askBody) = await server.SendAsync(askEncrypted);
        Assert.Equal("plain", DecryptEnvelope(credential.Secret, askBody).GetProperty("echo").GetProperty("message").GetString());
        var (_, plainBody) = await server.SendAsync(Signed(HttpMethod.Post, "/api/open:echo", credential, """{"message":"plain"}"""u8.ToArray()));
        Assert.Equal("plain", plainBody.GetProperty("echo").GetProperty("message").GetString());

        // 调用安全设置：不认识的算法与不是 IP 的白名单拒；改成 SHA512 + 网段后，旧算法的签名不对、网段外的地址被拒
        Assert.Equal("IDENTITY_VALIDATION_012", Code((await SendAsync(server, HttpMethod.Post, "/api/my-api-credentials:update-settings", """{"signatureAlgorithm":"RSA-SHA256","ipAllowList":[]}""", token)).Body));
        Assert.Equal("IDENTITY_VALIDATION_013", Code((await SendAsync(server, HttpMethod.Post, "/api/my-api-credentials:update-settings", """{"signatureAlgorithm":"HmacSHA256","ipAllowList":["nope"]}""", token)).Body));
        var (_, settings) = await SendAsync(server, HttpMethod.Post, "/api/my-api-credentials:update-settings", """{"signatureAlgorithm":"hmacsha512","ipAllowList":["10.0.0.0/24","10.0.0.0/24"," 192.168.1.9 "]}""", token);
        Assert.Equal("HmacSHA512", settings.GetProperty("signatureAlgorithm").GetString());
        Assert.Equal(_normalizedAllowList, settings.GetProperty("ipAllowList").EnumerateArray().Select(static e => e.GetString()!));
        Assert.Equal("OPENPLATFORM_DENIED_007", Code((await server.SendAsync(Signed(HttpMethod.Get, "/api/open:ping", credential, ip: "10.0.0.5"))).Body));
        Assert.Equal(HttpStatusCode.OK, (await server.SendAsync(Signed(HttpMethod.Get, "/api/open:ping", credential, algorithm: "HmacSHA512", ip: "10.0.0.5"))).Response.StatusCode);
        Assert.Equal("OPENPLATFORM_DENIED_004", Code((await server.SendAsync(Signed(HttpMethod.Get, "/api/open:ping", credential, algorithm: "HmacSHA512", ip: "10.0.1.5"))).Body));
        var mismatched = Signed(HttpMethod.Get, "/api/open:ping", credential, algorithm: "HmacSHA512", ip: "192.168.1.9");
        mismatched.Headers.Add("X-Sign-Algorithm", "HMACSHA256");
        Assert.Equal("OPENPLATFORM_VALIDATION_003", Code((await server.SendAsync(mismatched)).Body));
        Assert.Equal(HttpStatusCode.OK, (await SendAsync(server, HttpMethod.Post, "/api/my-api-credentials:update-settings", """{"signatureAlgorithm":"HmacSHA256","ipAllowList":null}""", token)).Status);

        // 停用、滚动密钥立即生效
        Assert.False((await SendAsync(server, HttpMethod.Put, $"/api/my-api-credentials/{credential.Id}", """{"isEnabled":false}""", token)).Body.GetProperty("isEnabled").GetBoolean());
        Assert.Equal("OPENPLATFORM_DENIED_003", Code((await server.SendAsync(Signed(HttpMethod.Get, "/api/open:ping", credential))).Body));
        Assert.True((await SendAsync(server, HttpMethod.Put, $"/api/my-api-credentials/{credential.Id}", """{"isEnabled":true}""", token)).Body.GetProperty("isEnabled").GetBoolean());
        var (_, rotatedBody) = await SendAsync(server, HttpMethod.Post, $"/api/my-api-credentials/{credential.Id}:rotate-secret", "{}", token);
        var rotated = credential with { Secret = rotatedBody.GetProperty("secret").GetString()! };
        Assert.NotEqual(credential.Secret, rotated.Secret);
        Assert.Equal("OPENPLATFORM_DENIED_007", Code((await server.SendAsync(Signed(HttpMethod.Get, "/api/open:ping", credential))).Body));
        Assert.Equal(HttpStatusCode.OK, (await server.SendAsync(Signed(HttpMethod.Get, "/api/open:ping", rotated))).Response.StatusCode);
        var (_, used) = await SendAsync(server, HttpMethod.Get, "/api/my-api-credentials", null, token);
        Assert.Equal(JsonValueKind.String, Assert.Single(used.GetProperty("items").EnumerateArray()).GetProperty("lastUsedAt").ValueKind);

        // 最多 5 个；删除后 AppKey 立即不能用
        for (var i = 0; i < 4; i++)
        {
            await CreateAsync(server, token, $"extra-{i}");
        }

        Assert.Equal("IDENTITY_CONFLICT_007", Code((await SendAsync(server, HttpMethod.Post, "/api/my-api-credentials", """{"name":"sixth"}""", token)).Body));
        var (_, deleted) = await SendAsync(server, HttpMethod.Delete, $"/api/my-api-credentials?ids={credential.Id}", null, token);
        Assert.Equal(credential.Id, Assert.Single(deleted.GetProperty("succeeded").EnumerateArray()).GetString());
        Assert.Equal("OPENPLATFORM_DENIED_003", Code((await server.SendAsync(Signed(HttpMethod.Get, "/api/open:ping", rotated))).Body));

        // 开放接口日志：成败都记，签名是否有效、明文摘要与归属人
        await server.Services.GetRequiredService<AuditLogWriter>().FlushAsync(CancellationToken.None);
        var (_, logs) = await SendAsync(server, HttpMethod.Post, "/api/open-api-logs:list", """{"page":{"index":1,"size":100}}""", token);
        var rows = logs.GetProperty("items").EnumerateArray().ToList();
        Assert.Contains(rows, r => r.GetProperty("operationId").GetString() == "open.ping" && r.GetProperty("signatureValid").GetBoolean() && r.GetProperty("succeeded").GetBoolean() && r.GetProperty("clientId").GetString() == credential.AppKey && r.GetProperty("userName").GetString() == "superadmin");
        Assert.Contains(rows, r => r.GetProperty("errorCode").GetString() == "OPENPLATFORM_DENIED_007" && !r.GetProperty("signatureValid").GetBoolean());
        Assert.Contains(rows, r => r.GetProperty("errorCode").GetString() == "OPENPLATFORM_DENIED_001" && r.GetProperty("statusCode").GetInt32() == 403);
        var details = new List<JsonElement>();
        foreach (var echo in rows.Where(static r => r.GetProperty("operationId").GetString() == "open.echo" && r.GetProperty("succeeded").GetBoolean()))
        {
            details.Add((await SendAsync(server, HttpMethod.Get, $"/api/open-api-logs/{echo.GetProperty("id").GetString()}", null, token)).Body);
        }

        // 加密的那次记的是解密后的明文，响应摘要也是明文
        var decrypted = Assert.Single(details, static d => d.GetProperty("requestBody").GetString() == """{"message":"secret hello","number":7}""");
        Assert.Contains("secret hello", decrypted.GetProperty("responseBody").GetString(), StringComparison.Ordinal);
    }
}
