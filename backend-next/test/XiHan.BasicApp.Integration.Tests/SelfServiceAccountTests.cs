// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Concurrent;
using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.Identity;
using XiHan.BasicApp.Identity.Infrastructure;
using XiHan.BasicApp.Integration.Tests.Infrastructure;
using XiHan.BasicApp.Notification.Infrastructure;

namespace XiHan.BasicApp.Integration.Tests;

/// <summary>
/// 验证自助注册与找回密码。
/// </summary>
/// <remarks>
/// 注册缺省关、配置打开后注册平台账号（立即可登录、不带角色，配了邮件发欢迎邮件），登录名与邮箱不能重复；
/// 找回密码按邮箱或已验证的手机号发码，账号不存在也回答已发送而不发；码对即设新密码、吊销全部会话，码用过作废，密码不合格不消费码。
/// </remarks>
public sealed class SelfServiceAccountTests : IDisposable
{
    private const string Code = "246810";

    private readonly SqliteDatabase _database = new();
    private readonly FakeEmailTransport _email = new();

    public void Dispose() => _database.Dispose();

    private sealed class FixedCodes(string code) : ICaptchaCodeSource
    {
        public string Generate(int length) => code[..length];
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

    private static async Task<(HttpStatusCode Status, JsonElement Body)> SendAsync(BasicAppTestServer server, HttpMethod method, string path, object? body, string? token = null)
    {
        var (response, json) = await server.SendAsync(BasicAppTestServer.Request(method, path, body is null ? null : JsonSerializer.Serialize(body), token, "zh-CN"));
        return (response.StatusCode, json);
    }

    private async Task<EmailEnvelope> MailToAsync(string to)
    {
        for (var attempt = 0; attempt < 100 && !_email.Sent.Any(m => m.To == to); attempt++)
        {
            await Task.Delay(50);
        }

        return Assert.Single(_email.Sent, m => m.To == to);
    }

    private async Task<(BasicAppTestServer Server, string Platform)> StartAsync(bool registration)
    {
        var (server, platform, _) = await SettingsTestHost.StartAsync(_database.ConnectionString, configure: builder =>
        {
            builder.Configuration.AddInMemoryCollection([new("BasicApp:Notification:DispatchInterval", "00:00:00.100"), new("BasicApp:Auth:SelfRegistrationEnabled", registration ? "true" : "false")]);
            builder.Services.AddSingleton<ICaptchaCodeSource>(new FixedCodes(Code + Code));
            builder.Services.AddSingleton<IEmailTransport>(_email);
        });
        await SendAsync(server, HttpMethod.Post, "/api/email-configs", new { code = "primary", name = "Primary", smtpHost = "smtp.example.com", smtpPort = 465, fromEmail = "noreply@example.com", userName = "noreply", password = "s3cret-pass" }, platform);
        return (server, platform);
    }

    [Fact]
    public async Task Registration_IsClosedByDefault()
    {
        var (server, _) = await StartAsync(registration: false);
        await using var running = server;
        Assert.False((await SendAsync(server, HttpMethod.Get, "/api/auth:login-config", null)).Body.GetProperty("registrationEnabled").GetBoolean());
        var (closed, closedBody) = await SendAsync(server, HttpMethod.Post, "/api/auth:register", new { userName = "eve", password = "Pass@123456", email = "eve@example.com" });
        Assert.Equal(HttpStatusCode.Forbidden, closed);
        Assert.Equal(IdentityErrors.RegistrationClosed, closedBody.GetProperty("code").GetString());
        Assert.Equal("没有开放自助注册，请联系管理员开通账号。", closedBody.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task Registration_CreatesAPlatformAccount_WithAWelcomeMail_AndRejectsDuplicates()
    {
        var (server, _) = await StartAsync(registration: true);
        await using var running = server;
        Assert.True((await SendAsync(server, HttpMethod.Get, "/api/auth:login-config", null)).Body.GetProperty("registrationEnabled").GetBoolean());

        var (registered, registeredBody) = await SendAsync(server, HttpMethod.Post, "/api/auth:register", new { userName = "eve", password = "Pass@123456", email = "Eve@Example.com" });
        Assert.Equal(HttpStatusCode.OK, registered);
        Assert.Equal("eve", registeredBody.GetProperty("userName").GetString());
        var welcome = await MailToAsync("eve@example.com");
        Assert.Contains("欢迎", welcome.Subject, StringComparison.Ordinal);

        // 立即可登录，是平台账号、不带任何权限
        var login = await AuthFlowTests.LoginAsync(server, "eve", "Pass@123456");
        var token = login.GetProperty("accessToken").GetString()!;
        var (_, permissions) = await SendAsync(server, HttpMethod.Get, "/api/auth:permissions", null, token);
        Assert.True(permissions.GetProperty("isPlatform").GetBoolean());
        Assert.Empty(permissions.GetProperty("permissions").EnumerateArray());

        // 登录名、邮箱（不分大小写）不能重复；格式与密码长度在请求校验里拒绝
        Assert.Equal(IdentityErrors.UserNameTaken, (await SendAsync(server, HttpMethod.Post, "/api/auth:register", new { userName = "eve", password = "Pass@123456", email = "other@example.com" })).Body.GetProperty("code").GetString());
        var (emailTaken, emailTakenBody) = await SendAsync(server, HttpMethod.Post, "/api/auth:register", new { userName = "eva", password = "Pass@123456", email = "EVE@example.com" });
        Assert.Equal(HttpStatusCode.Conflict, emailTaken);
        Assert.Equal(IdentityErrors.EmailTaken, emailTakenBody.GetProperty("code").GetString());
        Assert.Equal(HttpStatusCode.BadRequest, (await SendAsync(server, HttpMethod.Post, "/api/auth:register", new { userName = "Bad Name", password = "Pass@123456", email = "bad@example.com" })).Status);
        Assert.Equal(IdentityErrors.PasswordPolicy, (await SendAsync(server, HttpMethod.Post, "/api/auth:register", new { userName = "weak", password = "onlyletters", email = "weak@example.com" })).Body.GetProperty("code").GetString());
    }

    [Fact]
    public async Task PasswordReset_SendsACode_SetsTheNewPassword_AndRevokesSessions()
    {
        var (server, platform) = await StartAsync(registration: false);
        await using var running = server;
        await SendAsync(server, HttpMethod.Post, "/api/users", new { userName = "ann", displayName = "Ann", password = "Pass@123456", email = "ann@example.com" }, platform);
        var login = await AuthFlowTests.LoginAsync(server, "ann", "Pass@123456");

        // 账号不存在也回答已发送，但不发
        var (nobody, nobodyBody) = await SendAsync(server, HttpMethod.Post, "/api/auth:request-password-reset", new { channel = "Email", target = "nobody@example.com" });
        Assert.Equal((HttpStatusCode.OK, 600), (nobody, nobodyBody.GetProperty("expiresInSeconds").GetInt32()));

        // 真发：验证类模板，标题「找回密码」
        Assert.Equal(HttpStatusCode.OK, (await SendAsync(server, HttpMethod.Post, "/api/auth:request-password-reset", new { channel = "Email", target = "ANN@example.com" })).Status);
        var mail = await MailToAsync("ann@example.com");
        Assert.Contains(Code, mail.Body, StringComparison.Ordinal);
        Assert.Contains("找回密码", mail.Subject + mail.Body, StringComparison.Ordinal);
        Assert.DoesNotContain(_email.Sent, m => m.To == "nobody@example.com");

        // 密码不合格先拒绝、不消费码；码错不行；码对即重设，旧密码与旧会话都失效
        Assert.Equal(IdentityErrors.PasswordPolicy, (await SendAsync(server, HttpMethod.Post, "/api/auth:reset-password", new { channel = "Email", target = "ann@example.com", code = Code, newPassword = "onlyletters" })).Body.GetProperty("code").GetString());
        Assert.Equal(IdentityErrors.CodeInvalid, (await SendAsync(server, HttpMethod.Post, "/api/auth:reset-password", new { channel = "Email", target = "ann@example.com", code = "000000", newPassword = "Fresh@123456" })).Body.GetProperty("code").GetString());
        var (reset, resetBody) = await SendAsync(server, HttpMethod.Post, "/api/auth:reset-password", new { channel = "Email", target = "Ann@Example.com", code = Code, newPassword = "Fresh@123456" });
        Assert.Equal(HttpStatusCode.OK, reset);
        Assert.Equal("ann", resetBody.GetProperty("userName").GetString());
        Assert.NotEqual(HttpStatusCode.OK, (await SendAsync(server, HttpMethod.Post, "/api/auth:refresh", new { refreshToken = login.GetProperty("refreshToken").GetString() })).Status);
        Assert.Equal(HttpStatusCode.Forbidden, (await SendAsync(server, HttpMethod.Post, "/api/auth:login", new { userName = "ann", password = "Pass@123456" })).Status);
        Assert.False(string.IsNullOrEmpty((await AuthFlowTests.LoginAsync(server, "ann", "Fresh@123456")).GetProperty("accessToken").GetString()));

        // 码用过即作废
        Assert.Equal(IdentityErrors.CodeInvalid, (await SendAsync(server, HttpMethod.Post, "/api/auth:reset-password", new { channel = "Email", target = "ann@example.com", code = Code, newPassword = "Other@123456" })).Body.GetProperty("code").GetString());
    }
}
