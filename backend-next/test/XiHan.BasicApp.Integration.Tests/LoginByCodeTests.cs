// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Concurrent;
using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.Identity.Domain.Services;
using XiHan.BasicApp.Identity.Infrastructure;
using XiHan.BasicApp.Integration.Tests.Infrastructure;
using XiHan.BasicApp.Notification.Domain.Entities;
using XiHan.BasicApp.Notification.Infrastructure;

namespace XiHan.BasicApp.Integration.Tests;

/// <summary>
/// 验证邮箱 / 手机验证码登录。
/// </summary>
/// <remarks>
/// 登录方式由系统参数 <c>identity.login-methods</c> 开放（没建只有密码，写错即报错）；发码对账号不存在、不唯一一律回答已发送而不发，
/// 只有恰好一个正常账号（邮箱不分大小写、手机号要已验证）才真发；码对即登录，用过作废，码错计入节流；开了双因素的账号照样要第二步。
/// </remarks>
public sealed class LoginByCodeTests : IDisposable
{
    private const string Code = "135790";

    private readonly SqliteDatabase _database = new();
    private readonly FakeEmailTransport _email = new();
    private readonly FakeSmsTransport _sms = new();

    public void Dispose() => _database.Dispose();

    private sealed class FixedCodes(string code) : ICaptchaCodeSource
    {
        public string Generate(int length) => code[..length];
    }

    private static async Task<(HttpStatusCode Status, JsonElement Body)> SendAsync(BasicAppTestServer server, HttpMethod method, string path, object? body, string? token = null)
    {
        var (response, json) = await server.SendAsync(BasicAppTestServer.Request(method, path, body is null ? null : JsonSerializer.Serialize(body), token));
        return (response.StatusCode, json);
    }

    private static async Task<string> CodeOfAsync(BasicAppTestServer server, string path, object body)
        => (await SendAsync(server, HttpMethod.Post, path, body)).Body.GetProperty("code").GetString()!;

    private static async Task<List<string>> MethodsAsync(BasicAppTestServer server)
        => [.. (await SendAsync(server, HttpMethod.Get, "/api/auth:login-config", null)).Body.GetProperty("loginMethods").EnumerateArray().Select(static m => m.GetString()!)];

    [Fact]
    public async Task CodeLogin_IsOpenedByASystemParameter_RevealsNoAccounts_AndKeepsTwoFactor()
    {
        var (server, platform, _) = await SettingsTestHost.StartAsync(_database.ConnectionString, configure: builder =>
        {
            builder.Configuration.AddInMemoryCollection([new("BasicApp:Notification:DispatchInterval", "00:00:00.100"), new("BasicApp:Auth:VerificationCodeIntervalSeconds", "1")]);
            builder.Services.AddSingleton<ICaptchaCodeSource>(new FixedCodes(Code + Code));
            builder.Services.AddSingleton<IEmailTransport>(_email);
            builder.Services.AddSingleton<ISmsTransport>(_sms);
        });
        await using var running = server;
        await SendAsync(server, HttpMethod.Post, "/api/email-configs", new { code = "primary", name = "Primary", smtpHost = "smtp.example.com", smtpPort = 465, fromEmail = "noreply@example.com", userName = "noreply", password = "s3cret-pass" }, platform);
        await SendAsync(server, HttpMethod.Post, "/api/sms-configs", new { code = "ali", name = "Aliyun", provider = "Aliyun", accessKeyId = "id", accessKeySecret = "ali-secret", signName = "Acme", templateMap = """{"auth-sms-login-code":"SMS_0003"}""" }, platform);
        await SendAsync(server, HttpMethod.Post, "/api/users", new { userName = "ann", displayName = "Ann", password = "Pass@123456", email = "Ann@Example.com", phoneNumber = "13800000000" }, platform);
        await SendAsync(server, HttpMethod.Post, "/api/users", new { userName = "cara", displayName = "Cara", password = "Pass@123456", email = "dup@example.com" }, platform);
        await SendAsync(server, HttpMethod.Post, "/api/users", new { userName = "dan", displayName = "Dan", password = "Pass@123456", email = "dup@example.com" }, platform);

        // 没开放：登录页只有密码登录，发码被拒；参数写错即报错
        Assert.Equal(["password"], await MethodsAsync(server));
        Assert.Equal("IDENTITY_DENIED_016", await CodeOfAsync(server, "/api/auth:send-login-code", new { channel = "Email", target = "ann@example.com" }));
        var (_, config) = await SendAsync(server, HttpMethod.Post, "/api/configs", new { key = LoginMethods.ConfigKey, name = "Login methods", dataType = "Text", value = "email,fax" }, platform);
        Assert.Equal("IDENTITY_VALIDATION_021", (await SendAsync(server, HttpMethod.Get, "/api/auth:login-config", null)).Body.GetProperty("code").GetString());
        await SendAsync(server, HttpMethod.Put, $"/api/configs/{config.GetProperty("id").GetString()}", new { name = "Login methods", dataType = "Text", value = "email, phone" }, platform);
        Assert.Equal(["password", "email", "phone"], await MethodsAsync(server));

        // 不存在与不唯一的账号：同样回答已发送，但不发；手机号没验证的也不发
        Assert.Equal("IDENTITY_VALIDATION_022", await CodeOfAsync(server, "/api/auth:send-login-code", new { channel = "Email", target = "not-an-email" }));
        var (nobody, nobodyBody) = await SendAsync(server, HttpMethod.Post, "/api/auth:send-login-code", new { channel = "Email", target = "nobody@example.com" });
        Assert.Equal((HttpStatusCode.OK, 600), (nobody, nobodyBody.GetProperty("expiresInSeconds").GetInt32()));
        Assert.Equal(HttpStatusCode.OK, (await SendAsync(server, HttpMethod.Post, "/api/auth:send-login-code", new { channel = "Email", target = "dup@example.com" })).Status);
        Assert.Equal(HttpStatusCode.OK, (await SendAsync(server, HttpMethod.Post, "/api/auth:send-login-code", new { channel = "Phone", target = "13800000000" })).Status);
        Assert.Equal("IDENTITY_VALIDATION_018", await CodeOfAsync(server, "/api/auth:login-by-code", new { channel = "Email", target = "nobody@example.com", code = Code }));

        // 恰好一个账号（邮箱不分大小写）：真发，码错不行，码对即登录，用过作废
        Assert.Equal(HttpStatusCode.OK, (await SendAsync(server, HttpMethod.Post, "/api/auth:send-login-code", new { channel = "Email", target = "ANN@example.com" })).Status);
        for (var attempt = 0; attempt < 100 && !_email.Sent.Any(m => m.To == "ann@example.com"); attempt++)
        {
            await Task.Delay(50);
        }

        var mail = Assert.Single(_email.Sent);
        Assert.Empty(_sms.Sent);
        Assert.Equal("ann@example.com", mail.To);
        Assert.Contains(Code, mail.Body, StringComparison.Ordinal);
        Assert.Equal("IDENTITY_VALIDATION_018", await CodeOfAsync(server, "/api/auth:login-by-code", new { channel = "Email", target = "ann@example.com", code = "000000" }));
        var (signedIn, signedInBody) = await SendAsync(server, HttpMethod.Post, "/api/auth:login-by-code", new { channel = "Email", target = "Ann@Example.com", code = Code });
        Assert.Equal(HttpStatusCode.OK, signedIn);
        var token = signedInBody.GetProperty("token").GetProperty("accessToken").GetString()!;
        Assert.Equal("ann", (await SendAsync(server, HttpMethod.Get, "/api/auth:me", null, token)).Body.GetProperty("user").GetProperty("userName").GetString());
        Assert.Equal("IDENTITY_VALIDATION_018", await CodeOfAsync(server, "/api/auth:login-by-code", new { channel = "Email", target = "ann@example.com", code = Code }));

        // 开了双因素：验证码登录照样返回第二步的挑战，不开会话
        var (_, setup) = await SendAsync(server, HttpMethod.Post, "/api/profile:setup-totp", new { }, token);
        var secret = setup.GetProperty("sharedKey").GetString()!.Replace(" ", string.Empty, StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.OK, (await SendAsync(server, HttpMethod.Post, "/api/profile:enable-two-factor", new { method = "Totp", code = Totp.CodeAt(secret, Totp.StepOf(DateTimeOffset.UtcNow)) }, token)).Status);
        await Task.Delay(TimeSpan.FromSeconds(1.2));
        Assert.Equal(HttpStatusCode.OK, (await SendAsync(server, HttpMethod.Post, "/api/auth:send-login-code", new { channel = "Email", target = "ann@example.com" })).Status);
        var (challenged, challenge) = await SendAsync(server, HttpMethod.Post, "/api/auth:login-by-code", new { channel = "Email", target = "ann@example.com", code = Code });
        Assert.Equal(HttpStatusCode.OK, challenged);
        Assert.Equal(JsonValueKind.Null, challenge.GetProperty("token").ValueKind);
        Assert.Equal(["Totp"], challenge.GetProperty("twoFactor").GetProperty("methods").EnumerateArray().Select(static m => m.GetString()));
    }

    private sealed class FakeSmsTransport : ISmsTransport
    {
        public SmsProvider Provider => SmsProvider.Aliyun;

        public ConcurrentQueue<SmsEnvelope> Sent { get; } = new();

        public Task<TransportResult> SendAsync(SmsAccount account, SmsEnvelope envelope, CancellationToken cancellationToken)
        {
            Sent.Enqueue(envelope);
            return Task.FromResult(TransportResult.Sent("biz-1"));
        }
    }

    private sealed class FakeEmailTransport : IEmailTransport
    {
        public ConcurrentQueue<EmailEnvelope> Sent { get; } = new();

        public Task<TransportResult> SendAsync(EmailServer server, EmailEnvelope envelope, CancellationToken cancellationToken)
        {
            Sent.Enqueue(envelope);
            return Task.FromResult(TransportResult.Sent(null));
        }
    }
}
