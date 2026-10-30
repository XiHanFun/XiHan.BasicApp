// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Concurrent;
using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.Identity.Infrastructure;
using XiHan.BasicApp.Integration.Tests.Infrastructure;
using XiHan.BasicApp.Notification.Domain.Entities;
using XiHan.BasicApp.Notification.Infrastructure;

namespace XiHan.BasicApp.Integration.Tests;

/// <summary>
/// 验证个人中心的邮箱 / 手机号验证与换绑。
/// </summary>
/// <remarks>
/// 验证码经 Notification 的发件箱发出（没有可用的发送配置即不签发）；同一目标 60 秒内不能再发，码错计次、5 次锁定；
/// 验证当前联系方式后记为已验证，管理员改了即复位；换绑要当前密码、新的合格式且同一租户里没人用，收码确认后换成新的并记为已验证。
/// </remarks>
public sealed class ProfileContactsTests : IDisposable
{
    private const string Code = "246810";

    private readonly SqliteDatabase _database = new();
    private readonly FakeEmailTransport _email = new();
    private readonly FakeSmsTransport _sms = new();

    public void Dispose() => _database.Dispose();

    private sealed class FixedCodes(string code) : ICaptchaCodeSource
    {
        public string Generate(int length) => code[..length];
    }

    private static async Task<(HttpStatusCode Status, JsonElement Body)> SendAsync(BasicAppTestServer server, HttpMethod method, string path, object? body, string token)
    {
        var (response, json) = await server.SendAsync(BasicAppTestServer.Request(method, path, body is null ? null : JsonSerializer.Serialize(body), token));
        return (response.StatusCode, json);
    }

    private static async Task<string> CodeOfAsync(BasicAppTestServer server, HttpMethod method, string path, object? body, string token)
        => (await SendAsync(server, method, path, body, token)).Body.GetProperty("code").GetString()!;

    private static async Task<JsonElement> ProfileAsync(BasicAppTestServer server, string token)
        => (await SendAsync(server, HttpMethod.Get, "/api/profile", null, token)).Body;

    private static async Task<T> WaitForAsync<T>(Func<T?> probe)
        where T : class
    {
        for (var attempt = 0; attempt < 100; attempt++)
        {
            if (probe() is { } found)
            {
                return found;
            }

            await Task.Delay(50);
        }

        throw new TimeoutException("The message was not sent.");
    }

    [Fact]
    public async Task Contacts_AreVerifiedAndChanged_WithCodesSentThroughTheOutbox()
    {
        var (server, platform, _) = await SettingsTestHost.StartAsync(_database.ConnectionString, configure: builder =>
        {
            builder.Configuration.AddInMemoryCollection([new("BasicApp:Notification:DispatchInterval", "00:00:00.100")]);
            builder.Services.AddSingleton<ICaptchaCodeSource>(new FixedCodes(Code + Code));
            builder.Services.AddSingleton<IEmailTransport>(_email);
            builder.Services.AddSingleton<ISmsTransport>(_sms);
        });
        await using var running = server;
        var (_, created) = await SendAsync(server, HttpMethod.Post, "/api/users", new { userName = "ann", displayName = "Ann", password = "Pass@123456", email = "Ann@Example.com", phoneNumber = "13800000000" }, platform);
        var annId = created.GetProperty("id").GetString()!;
        await SendAsync(server, HttpMethod.Post, "/api/users", new { userName = "bob", displayName = "Bob", password = "Pass@123456", email = "bob@example.com" }, platform);
        var ann = (await AuthFlowTests.LoginAsync(server, "ann", "Pass@123456")).GetProperty("accessToken").GetString()!;
        Assert.Equal((false, false), ((await ProfileAsync(server, ann)).GetProperty("emailVerified").GetBoolean(), (await ProfileAsync(server, ann)).GetProperty("phoneVerified").GetBoolean()));

        // 没有可用的发送配置：不签发码
        Assert.Equal("IDENTITY_UNAVAILABLE_006", await CodeOfAsync(server, HttpMethod.Post, "/api/profile:send-verification-code", new { channel = "Email" }, ann));
        await SendAsync(server, HttpMethod.Post, "/api/email-configs", new { code = "primary", name = "Primary", smtpHost = "smtp.example.com", smtpPort = 465, fromEmail = "noreply@example.com", userName = "noreply", password = "s3cret-pass" }, platform);
        await SendAsync(server, HttpMethod.Post, "/api/sms-configs", new { code = "ali", name = "Aliyun", provider = "Aliyun", accessKeyId = "id", accessKeySecret = "ali-secret", signName = "Acme", templateMap = """{"auth-sms-verification-code":"SMS_0002"}""" }, platform);

        // 验证当前邮箱：码经发件箱发出（带用途标题），60 秒内不能再发，码错不算，码对即已验证
        var (sent, sentBody) = await SendAsync(server, HttpMethod.Post, "/api/profile:send-verification-code", new { channel = "Email" }, ann);
        Assert.Equal((HttpStatusCode.OK, 600), (sent, sentBody.GetProperty("expiresInSeconds").GetInt32()));
        var mail = await WaitForAsync(() => _email.Sent.FirstOrDefault(m => m.To == "Ann@Example.com"));
        Assert.Contains(Code, mail.Body, StringComparison.Ordinal);
        Assert.Equal("IDENTITY_UNAVAILABLE_002", await CodeOfAsync(server, HttpMethod.Post, "/api/profile:send-verification-code", new { channel = "Email" }, ann));
        Assert.Equal("IDENTITY_VALIDATION_018", await CodeOfAsync(server, HttpMethod.Post, "/api/profile:verify-contact", new { channel = "Email", code = "000000" }, ann));
        Assert.True((await SendAsync(server, HttpMethod.Post, "/api/profile:verify-contact", new { channel = "Email", code = Code }, ann)).Body.GetProperty("emailVerified").GetBoolean());
        Assert.Equal("IDENTITY_VALIDATION_018", await CodeOfAsync(server, HttpMethod.Post, "/api/profile:verify-contact", new { channel = "Email", code = Code }, ann));

        // 验证当前手机号走短信；管理员改了手机号即复位
        Assert.Equal(HttpStatusCode.OK, (await SendAsync(server, HttpMethod.Post, "/api/profile:send-verification-code", new { channel = "Phone" }, ann)).Status);
        var sms = await WaitForAsync(() => _sms.Sent.FirstOrDefault(m => m.Phone == "13800000000"));
        Assert.Equal(("SMS_0002", Code), (sms.ProviderTemplateId, sms.Parameters.Single(p => p.Key == "code").Value));
        Assert.True((await SendAsync(server, HttpMethod.Post, "/api/profile:verify-contact", new { channel = "Phone", code = Code }, ann)).Body.GetProperty("phoneVerified").GetBoolean());
        await SendAsync(server, HttpMethod.Put, $"/api/users/{annId}", new { displayName = "Ann", email = "Ann@Example.com", phoneNumber = "13800000001" }, platform);
        var reset = await ProfileAsync(server, ann);
        Assert.Equal((true, false), (reset.GetProperty("emailVerified").GetBoolean(), reset.GetProperty("phoneVerified").GetBoolean()));

        // 换绑邮箱：要当前密码、合格式、与现在的不同、同一租户没人用（不分大小写）；码发到新的，确认后换成新的（转小写）并记为已验证
        Assert.Equal("IDENTITY_DENIED_003", await CodeOfAsync(server, HttpMethod.Post, "/api/profile:send-change-contact-code", new { channel = "Email", newValue = "ann.new@example.com", password = "wrong-pass" }, ann));
        Assert.Equal("IDENTITY_VALIDATION_020", await CodeOfAsync(server, HttpMethod.Post, "/api/profile:send-change-contact-code", new { channel = "Email", newValue = "not-an-email", password = "Pass@123456" }, ann));
        Assert.Equal("IDENTITY_VALIDATION_020", await CodeOfAsync(server, HttpMethod.Post, "/api/profile:send-change-contact-code", new { channel = "Email", newValue = "ann@example.com", password = "Pass@123456" }, ann));
        Assert.Equal("IDENTITY_CONFLICT_010", await CodeOfAsync(server, HttpMethod.Post, "/api/profile:send-change-contact-code", new { channel = "Email", newValue = "BOB@example.com", password = "Pass@123456" }, ann));
        Assert.Equal(HttpStatusCode.OK, (await SendAsync(server, HttpMethod.Post, "/api/profile:send-change-contact-code", new { channel = "Email", newValue = "Ann.New@Example.com", password = "Pass@123456" }, ann)).Status);
        await WaitForAsync(() => _email.Sent.FirstOrDefault(m => m.To == "ann.new@example.com"));
        var (changed, changedBody) = await SendAsync(server, HttpMethod.Post, "/api/profile:change-contact", new { channel = "Email", code = Code }, ann);
        Assert.Equal((HttpStatusCode.OK, "ann.new@example.com", true), (changed, changedBody.GetProperty("user").GetProperty("email").GetString(), changedBody.GetProperty("emailVerified").GetBoolean()));

        // 码错 5 次即锁定：之后码对也不行
        Assert.Equal(HttpStatusCode.OK, (await SendAsync(server, HttpMethod.Post, "/api/profile:send-change-contact-code", new { channel = "Phone", newValue = "13900000000", password = "Pass@123456" }, ann)).Status);
        for (var attempt = 1; attempt < 5; attempt++)
        {
            Assert.Equal("IDENTITY_VALIDATION_018", await CodeOfAsync(server, HttpMethod.Post, "/api/profile:change-contact", new { channel = "Phone", code = "000000" }, ann));
        }

        Assert.Equal("IDENTITY_UNAVAILABLE_005", await CodeOfAsync(server, HttpMethod.Post, "/api/profile:change-contact", new { channel = "Phone", code = "000000" }, ann));
        Assert.Equal("IDENTITY_UNAVAILABLE_005", await CodeOfAsync(server, HttpMethod.Post, "/api/profile:change-contact", new { channel = "Phone", code = Code }, ann));
        Assert.Equal("13800000001", (await ProfileAsync(server, ann)).GetProperty("user").GetProperty("phoneNumber").GetString());
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
}
