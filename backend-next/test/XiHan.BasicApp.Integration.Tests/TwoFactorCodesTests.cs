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
/// 验证邮件与短信双因素。
/// </summary>
/// <remarks>
/// 开关要本人已验证的联系方式与发到那里的码；登录第二步凭票据发码、按票据能用的方式核对；
/// 验证码登录的第二步不能再用收码的那种（只剩它即拒绝）；管理员改了联系方式即不再可用，只有它时登录被拒。
/// </remarks>
public sealed class TwoFactorCodesTests : IDisposable
{
    private const string Code = "246801";

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

    private static async Task<string> CodeOfAsync(BasicAppTestServer server, string path, object body, string? token = null)
        => (await SendAsync(server, HttpMethod.Post, path, body, token)).Body.GetProperty("code").GetString()!;

    private static async Task<JsonElement> ChallengeOfAsync(BasicAppTestServer server, string path, object body)
    {
        var (status, response) = await SendAsync(server, HttpMethod.Post, path, body);
        Assert.Equal(HttpStatusCode.OK, status);
        Assert.Equal(JsonValueKind.Null, response.GetProperty("token").ValueKind);
        return response.GetProperty("twoFactor");
    }

    private static string[] MethodsOf(JsonElement element, string property)
        => [.. element.GetProperty(property).EnumerateArray().Select(static m => m.GetString()!)];

    private static async Task WaitForAsync(Func<bool> sent)
    {
        for (var attempt = 0; attempt < 100 && !sent(); attempt++)
        {
            await Task.Delay(50);
        }

        Assert.True(sent());
    }

    [Fact]
    public async Task EmailAndSmsTwoFactor_NeedVerifiedContacts_AndTheTicketsMethods()
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
        await SendAsync(server, HttpMethod.Post, "/api/sms-configs", new { code = "ali", name = "Aliyun", provider = "Aliyun", accessKeyId = "id", accessKeySecret = "ali-secret", signName = "Acme", templateMap = """{"auth-sms-login-code":"SMS_0003","auth-sms-verification-code":"SMS_0004"}""" }, platform);
        var (_, created) = await SendAsync(server, HttpMethod.Post, "/api/users", new { userName = "ann", displayName = "Ann", password = "Pass@123456", email = "ann@example.com", phoneNumber = "13800000000" }, platform);
        var userId = created.GetProperty("id").GetString()!;
        var password = new { userName = "ann", password = "Pass@123456" };
        var token = (await SendAsync(server, HttpMethod.Post, "/api/auth:login", password)).Body.GetProperty("token").GetProperty("accessToken").GetString()!;

        // 开启：认证器不发码，没验证的联系方式不行；验证后发码，码错不行、码对开启
        Assert.Equal("IDENTITY_VALIDATION_008", await CodeOfAsync(server, "/api/profile:send-two-factor-code", new { method = "Totp" }, token));
        Assert.Equal("IDENTITY_VALIDATION_023", await CodeOfAsync(server, "/api/profile:send-two-factor-code", new { method = "Email" }, token));
        Assert.Equal("IDENTITY_VALIDATION_023", await CodeOfAsync(server, "/api/profile:enable-two-factor", new { method = "Email", code = Code }, token));
        await SendAsync(server, HttpMethod.Post, "/api/profile:send-verification-code", new { channel = "Email" }, token);
        Assert.Equal(HttpStatusCode.OK, (await SendAsync(server, HttpMethod.Post, "/api/profile:verify-contact", new { channel = "Email", code = Code }, token)).Status);
        Assert.Equal(HttpStatusCode.OK, (await SendAsync(server, HttpMethod.Post, "/api/profile:send-two-factor-code", new { method = "Email" }, token)).Status);
        Assert.Equal("IDENTITY_VALIDATION_018", await CodeOfAsync(server, "/api/profile:enable-two-factor", new { method = "Email", code = "000000" }, token));
        var (enabled, profile) = await SendAsync(server, HttpMethod.Post, "/api/profile:enable-two-factor", new { method = "Email", code = Code }, token);
        Assert.Equal(HttpStatusCode.OK, enabled);
        Assert.Equal(["Email"], MethodsOf(profile, "twoFactorMethods"));
        await WaitForAsync(() => _email.Sent.Count == 2);

        // 密码登录：挑战不预先选定，凭票据发码（只能发票据能用的），码对即登录
        var challenge = await ChallengeOfAsync(server, "/api/auth:login", password);
        Assert.Equal(["Email"], MethodsOf(challenge, "methods"));
        Assert.Equal(JsonValueKind.Null, challenge.GetProperty("method").ValueKind);
        var ticket = challenge.GetProperty("ticket").GetString()!;
        Assert.Equal("IDENTITY_VALIDATION_008", await CodeOfAsync(server, "/api/auth:send-two-factor-code", new { ticket, method = "Totp" }));
        Assert.Equal("IDENTITY_VALIDATION_008", await CodeOfAsync(server, "/api/auth:send-two-factor-code", new { ticket, method = "Phone" }));
        Assert.Equal("IDENTITY_DENIED_008", await CodeOfAsync(server, "/api/auth:send-two-factor-code", new { ticket = "nope", method = "Email" }));
        var (sent, sentBody) = await SendAsync(server, HttpMethod.Post, "/api/auth:send-two-factor-code", new { ticket, method = "Email" });
        Assert.Equal((HttpStatusCode.OK, 600), (sent, sentBody.GetProperty("expiresInSeconds").GetInt32()));
        await WaitForAsync(() => _email.Sent.Count == 3);
        Assert.All(_email.Sent, m => Assert.Equal("ann@example.com", m.To));
        Assert.Equal("IDENTITY_DENIED_009", await CodeOfAsync(server, "/api/auth:verify-two-factor", new { ticket, method = "Email", code = "000000" }));
        var (verified, verifiedBody) = await SendAsync(server, HttpMethod.Post, "/api/auth:verify-two-factor", new { ticket, method = "Email", code = Code });
        Assert.Equal(HttpStatusCode.OK, verified);
        Assert.False(string.IsNullOrEmpty(verifiedBody.GetProperty("accessToken").GetString()));
        Assert.Equal("IDENTITY_DENIED_008", await CodeOfAsync(server, "/api/auth:verify-two-factor", new { ticket, method = "Email", code = Code }));

        // 验证码登录：第二步不能再用邮箱，只剩它即拒绝；再开短信，第二步就只给短信
        await SendAsync(server, HttpMethod.Post, "/api/configs", new { key = LoginMethods.ConfigKey, name = "Login methods", dataType = "Text", value = "email" }, platform);
        await SendAsync(server, HttpMethod.Post, "/api/auth:send-login-code", new { channel = "Email", target = "ann@example.com" });
        Assert.Equal("IDENTITY_DENIED_017", await CodeOfAsync(server, "/api/auth:login-by-code", new { channel = "Email", target = "ann@example.com", code = Code }));
        await SendAsync(server, HttpMethod.Post, "/api/profile:send-verification-code", new { channel = "Phone" }, token);
        Assert.Equal(HttpStatusCode.OK, (await SendAsync(server, HttpMethod.Post, "/api/profile:verify-contact", new { channel = "Phone", code = Code }, token)).Status);
        Assert.Equal(HttpStatusCode.OK, (await SendAsync(server, HttpMethod.Post, "/api/profile:send-two-factor-code", new { method = "Phone" }, token)).Status);
        Assert.Equal(HttpStatusCode.OK, (await SendAsync(server, HttpMethod.Post, "/api/profile:enable-two-factor", new { method = "Phone", code = Code }, token)).Status);
        await Task.Delay(TimeSpan.FromSeconds(1.2));
        await SendAsync(server, HttpMethod.Post, "/api/auth:send-login-code", new { channel = "Email", target = "ann@example.com" });
        challenge = await ChallengeOfAsync(server, "/api/auth:login-by-code", new { channel = "Email", target = "ann@example.com", code = Code });
        Assert.Equal(["Phone"], MethodsOf(challenge, "methods"));
        ticket = challenge.GetProperty("ticket").GetString()!;
        Assert.Equal("IDENTITY_VALIDATION_008", await CodeOfAsync(server, "/api/auth:send-two-factor-code", new { ticket, method = "Email" }));
        Assert.Equal(HttpStatusCode.OK, (await SendAsync(server, HttpMethod.Post, "/api/auth:send-two-factor-code", new { ticket, method = "Phone" })).Status);
        Assert.Equal("IDENTITY_VALIDATION_008", await CodeOfAsync(server, "/api/auth:verify-two-factor", new { ticket, method = "Email", code = Code }));
        Assert.Equal(HttpStatusCode.OK, (await SendAsync(server, HttpMethod.Post, "/api/auth:verify-two-factor", new { ticket, method = "Phone", code = Code })).Status);
        await WaitForAsync(() => _sms.Sent.Count == 3);
        Assert.All(_sms.Sent, m => Assert.Equal("13800000000", m.Phone));

        // 关闭邮箱：同样凭发到邮箱的码
        Assert.Equal(HttpStatusCode.OK, (await SendAsync(server, HttpMethod.Post, "/api/profile:send-two-factor-code", new { method = "Email" }, token)).Status);
        var (disabled, afterDisable) = await SendAsync(server, HttpMethod.Post, "/api/profile:disable-two-factor", new { method = "Email", code = Code }, token);
        Assert.Equal(HttpStatusCode.OK, disabled);
        Assert.Equal(["Phone"], MethodsOf(afterDisable, "twoFactorMethods"));

        // 管理员改了手机号：没验证的手机号不能做第二步，只有它时登录被拒（须重置双因素）
        Assert.Equal(HttpStatusCode.OK, (await SendAsync(server, HttpMethod.Put, $"/api/users/{userId}", new { displayName = "Ann", email = "ann@example.com", phoneNumber = "13900000000" }, platform)).Status);
        Assert.Equal("IDENTITY_DENIED_010", await CodeOfAsync(server, "/api/auth:login", password));
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
