// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Net;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.Identity.Domain.Services;
using XiHan.BasicApp.Identity.Infrastructure;
using XiHan.BasicApp.Integration.Tests.Infrastructure;
using XiHan.BasicApp.Notification.Infrastructure;

namespace XiHan.BasicApp.Integration.Tests;

/// <summary>
/// 验证强制修改由他人设置的密码
/// </summary>
/// <remarks>
/// 管理员创建或重置、开通租户管理员、种子写入都记下密码由他人设置，系统参数 <c>identity.password</c> 的 <c>forceChange</c> 决定要不要逼（缺省不逼）；
/// 打开之后凭密码登录锁定到改密为止（只放行改密与注销，刷新照旧），改完解开并不再要求，同一账号的验证码与第三方登录不走这一步
/// </remarks>
public sealed class ForcePasswordChangeTests : IDisposable
{
    private const string Reason = "PasswordChangeRequired";

    private readonly SqliteDatabase _database = new();

    public void Dispose() => _database.Dispose();

    private sealed class FixedCodes(string code) : ICaptchaCodeSource
    {
        public string Generate(int length) => code[..length];
    }

    private sealed class FakeEmailTransport : IEmailTransport
    {
        public Task<TransportResult> SendAsync(EmailServer server, EmailEnvelope envelope, CancellationToken cancellationToken)
            => Task.FromResult(TransportResult.Sent(null));
    }

    private async Task<(BasicAppTestServer Server, string Superadmin)> StartAsync()
    {
        var server = await BasicAppTestServer.StartAsync(_database.ConnectionString, settings: [new("BasicApp:Seed:EnableDemoData", "true")]);
        await server.SeedDefaultsAsync();
        await server.RunSeedersAsync();
        return (server, (await AuthFlowTests.LoginAsync(server, "superadmin", "SuperAdmin@123")).GetProperty("accessToken").GetString()!);
    }

    private static async Task<string> ConfigureAsync(BasicAppTestServer server, string superadmin, string value, string dataType = "Json")
    {
        var (created, createdBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/configs", JsonSerializer.Serialize(new { key = PasswordSettings.ConfigKey, name = "Password", dataType, value }), superadmin));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        return createdBody.GetProperty("id").GetString()!;
    }

    private static async Task<string> CreateUserAsync(BasicAppTestServer server, string superadmin, string userName, string password)
    {
        var (created, createdBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/users", JsonSerializer.Serialize(new { userName, displayName = userName, password }), superadmin));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        return createdBody.GetProperty("id").GetString()!;
    }

    private static async Task AssertLockedAsync(BasicAppTestServer server, string token)
    {
        var (me, meBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/api/auth:me", token: token, acceptLanguage: "en-US"));
        Assert.Equal(HttpStatusCode.Forbidden, me.StatusCode);
        Assert.Equal(("IDENTITY_DENIED_002", Reason), (meBody.GetProperty("code").GetString(), meBody.GetProperty("details").GetProperty("reason").GetString()));
        Assert.Equal("Your password was set by an administrator; change it first.", meBody.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task ByDefault_NobodyIsForced()
    {
        var (server, superadmin) = await StartAsync();
        await using (server)
        {
            var bob = await CreateUserAsync(server, superadmin, "bob", "Bob@123456");
            var (reset, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, $"/api/users/{bob}:reset-password", """{"newPassword":"Bob@654321"}""", superadmin));
            Assert.Equal(HttpStatusCode.OK, reset.StatusCode);
            var login = await AuthFlowTests.LoginAsync(server, "bob", "Bob@654321");
            Assert.False(login.GetProperty("mustChangePassword").GetBoolean());
            var (me, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/api/auth:me", token: login.GetProperty("accessToken").GetString()));
            Assert.Equal(HttpStatusCode.OK, me.StatusCode);
        }
    }

    [Fact]
    public async Task Forced_LocksThePasswordLoginUntilThePasswordIsChanged()
    {
        var (server, superadmin) = await StartAsync();
        await using (server)
        {
            var bob = await CreateUserAsync(server, superadmin, "bob", "Bob@123456");
            await ConfigureAsync(server, superadmin, """{"forceChange":true}""");
            var (reset, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, $"/api/users/{bob}:reset-password", """{"newPassword":"Bob@654321"}""", superadmin));
            Assert.Equal(HttpStatusCode.OK, reset.StatusCode);

            // 登录拿到令牌，但会话锁着：别的操作一律拒绝（带原因），锁屏密码解不开
            var login = await AuthFlowTests.LoginAsync(server, "bob", "Bob@654321");
            Assert.True(login.GetProperty("mustChangePassword").GetBoolean());
            var token = login.GetProperty("accessToken").GetString()!;
            await AssertLockedAsync(server, token);
            var (permissions, permissionsBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/api/auth:permissions", token: token));
            Assert.Equal((HttpStatusCode.Forbidden, "IDENTITY_DENIED_002"), (permissions.StatusCode, permissionsBody.GetProperty("code").GetString()));
            var (unlock, unlockBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/auth:unlock", """{"password":"Bob@654321"}""", token));
            Assert.Equal((HttpStatusCode.Conflict, "IDENTITY_CONFLICT_006"), (unlock.StatusCode, unlockBody.GetProperty("code").GetString()));

            // 刷新照旧可用，但新令牌还锁着
            var (refreshed, refreshedBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/auth:refresh", $$"""{"refreshToken":"{{login.GetProperty("refreshToken").GetString()}}"}"""));
            Assert.Equal(HttpStatusCode.OK, refreshed.StatusCode);
            Assert.True(refreshedBody.GetProperty("mustChangePassword").GetBoolean());
            token = refreshedBody.GetProperty("accessToken").GetString()!;
            await AssertLockedAsync(server, token);

            // 当前密码不对：还锁着；不合策略：还锁着
            var (wrong, wrongBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/profile:change-password", """{"currentPassword":"nope-12345","newPassword":"Bob@777777"}""", token));
            Assert.Equal((HttpStatusCode.Forbidden, "IDENTITY_DENIED_003"), (wrong.StatusCode, wrongBody.GetProperty("code").GetString()));
            await AssertLockedAsync(server, token);

            // 改密：立刻解开，同一枚令牌能用（不拨钟，门禁缓存提交后就失效）；提醒在收件箱里，类型是安全
            var (changed, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/profile:change-password", """{"currentPassword":"Bob@654321","newPassword":"Bob@777777"}""", token));
            Assert.Equal(HttpStatusCode.OK, changed.StatusCode);
            var (me, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/api/auth:me", token: token));
            Assert.Equal(HttpStatusCode.OK, me.StatusCode);
            var (inbox, inboxBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/inbox:list", "{}", token, "en-US"));
            Assert.Equal(HttpStatusCode.OK, inbox.StatusCode);
            var notice = Assert.Single(inboxBody.GetProperty("items").EnumerateArray());
            Assert.Equal("Security", notice.GetProperty("notificationType").GetString());

            // 之后再登录不再要求
            var again = await AuthFlowTests.LoginAsync(server, "bob", "Bob@777777");
            Assert.False(again.GetProperty("mustChangePassword").GetBoolean());
        }
    }

    [Fact]
    public async Task TurningItOn_AppliesToEveryPasswordSetBySomeoneElse_ButNotToOnesTheUserChose()
    {
        var (server, superadmin) = await StartAsync();
        await using (server)
        {
            // 打开之前管理员建的账号、开通的租户管理员（演示种子）与种子写的超管都有标记；本人改过的没有
            await CreateUserAsync(server, superadmin, "created", "Created@123456");
            await CreateUserAsync(server, superadmin, "changed", "Changed@123456");
            var changedLogin = await AuthFlowTests.LoginAsync(server, "changed", "Changed@123456");
            Assert.False(changedLogin.GetProperty("mustChangePassword").GetBoolean());
            var (selfChanged, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/profile:change-password", """{"currentPassword":"Changed@123456","newPassword":"Changed@654321"}""", changedLogin.GetProperty("accessToken").GetString()));
            Assert.Equal(HttpStatusCode.OK, selfChanged.StatusCode);

            await ConfigureAsync(server, superadmin, """{"forceChange":true}""");

            Assert.True((await AuthFlowTests.LoginAsync(server, "created", "Created@123456")).GetProperty("mustChangePassword").GetBoolean());
            var tenantAdmin = await AuthFlowTests.LoginAsync(server, "admin", "Demo@123456", "demo");
            Assert.True(tenantAdmin.GetProperty("mustChangePassword").GetBoolean());
            await AssertLockedAsync(server, tenantAdmin.GetProperty("accessToken").GetString()!);
            Assert.True((await AuthFlowTests.LoginAsync(server, "superadmin", "SuperAdmin@123")).GetProperty("mustChangePassword").GetBoolean());
            Assert.False((await AuthFlowTests.LoginAsync(server, "changed", "Changed@654321")).GetProperty("mustChangePassword").GetBoolean());

            // 早就在线的会话不受影响，下次凭密码登录才锁；模仿打开的会话也不锁
            var (stillOpen, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/api/auth:me", token: superadmin));
            Assert.Equal(HttpStatusCode.OK, stillOpen.StatusCode);
            var demoAdminId = tenantAdmin.GetProperty("user").GetProperty("id").GetString()!;
            var (started, startedBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/impersonation:start", $$"""{"targetUserId":"{{demoAdminId}}","tenantId":"demo"}""", superadmin));
            Assert.Equal(HttpStatusCode.OK, started.StatusCode);
            Assert.False(startedBody.GetProperty("mustChangePassword").GetBoolean());
            var (impersonating, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/api/auth:me", token: startedBody.GetProperty("accessToken").GetString()));
            Assert.Equal(HttpStatusCode.OK, impersonating.StatusCode);
        }
    }

    [Fact]
    public async Task OnlyPasswordLoginsAreLocked_IncludingTheirTwoFactorStep_NotCodeLogins()
    {
        const string code = "135790";
        var clock = new ManualClock(DateTimeOffset.UtcNow);
        await using var server = await BasicAppTestServer.StartAsync(_database.ConnectionString, configure: builder =>
        {
            builder.Services.AddSingleton<TimeProvider>(clock);
            builder.Services.AddSingleton<ICaptchaCodeSource>(new FixedCodes(code + code));
            builder.Services.AddSingleton<IEmailTransport>(new FakeEmailTransport());
        });
        await server.SeedDefaultsAsync();
        await server.RunSeedersAsync();
        var superadmin = (await AuthFlowTests.LoginAsync(server, "superadmin", "SuperAdmin@123")).GetProperty("accessToken").GetString()!;
        var (mailConfig, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/email-configs", JsonSerializer.Serialize(new { code = "primary", name = "Primary", smtpHost = "smtp.example.com", smtpPort = 465, fromEmail = "noreply@example.com", userName = "noreply", password = "s3cret-pass" }), superadmin));
        Assert.Equal(HttpStatusCode.Created, mailConfig.StatusCode);
        var (methods, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/configs", JsonSerializer.Serialize(new { key = LoginMethods.ConfigKey, name = "Login methods", dataType = "Text", value = "email" }), superadmin));
        Assert.Equal(HttpStatusCode.Created, methods.StatusCode);
        var (ann, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/users", """{"userName":"ann","displayName":"Ann","password":"Pass@123456","email":"ann@example.com"}""", superadmin));
        Assert.Equal(HttpStatusCode.Created, ann.StatusCode);
        await CreateUserAsync(server, superadmin, "tess", "Tess@123456");

        // 开双因素要在打开参数之前（打开之后 tess 一登录就锁着，开不了）
        var tess = (await AuthFlowTests.LoginAsync(server, "tess", "Tess@123456")).GetProperty("accessToken").GetString()!;
        var (setup, setupBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/profile:setup-totp", "{}", tess));
        Assert.Equal(HttpStatusCode.OK, setup.StatusCode);
        var secret = setupBody.GetProperty("sharedKey").GetString()!.Replace(" ", string.Empty, StringComparison.Ordinal);
        var (enabled, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/profile:enable-two-factor", $$"""{"method":"Totp","code":"{{Totp.CodeAt(secret, Totp.StepOf(clock.GetUtcNow()))}}"}""", tess));
        Assert.Equal(HttpStatusCode.OK, enabled.StatusCode);
        await ConfigureAsync(server, superadmin, """{"forceChange":true}""");

        // 验证码登录没有当前密码可核对，不锁
        var (sent, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/auth:send-login-code", """{"channel":"Email","target":"ann@example.com"}"""));
        Assert.Equal(HttpStatusCode.OK, sent.StatusCode);
        var (byCode, byCodeBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/auth:login-by-code", $$"""{"channel":"Email","target":"ann@example.com","code":"{{code}}"}"""));
        Assert.Equal(HttpStatusCode.OK, byCode.StatusCode);
        Assert.False(byCodeBody.GetProperty("token").GetProperty("mustChangePassword").GetBoolean());
        var (annMe, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/api/auth:me", token: byCodeBody.GetProperty("token").GetProperty("accessToken").GetString()));
        Assert.Equal(HttpStatusCode.OK, annMe.StatusCode);

        // 凭密码登录的第二步：第一步是密码，同样锁（认证器的码不能重放，拨到下一个时间步）
        clock.Set(clock.GetUtcNow().AddSeconds(Totp.PeriodSeconds));
        var (challenged, challenge) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/auth:login", """{"userName":"tess","password":"Tess@123456"}"""));
        Assert.Equal(HttpStatusCode.OK, challenged.StatusCode);
        var (verified, verifiedBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/auth:verify-two-factor", $$"""{"ticket":"{{challenge.GetProperty("twoFactor").GetProperty("ticket").GetString()}}","method":"Totp","code":"{{Totp.CodeAt(secret, Totp.StepOf(clock.GetUtcNow()))}}"}"""));
        Assert.Equal(HttpStatusCode.OK, verified.StatusCode);
        Assert.True(verifiedBody.GetProperty("mustChangePassword").GetBoolean());
    }

    [Fact]
    public async Task AWrongValue_StopsOnlyThePasswordLoginsItWouldLock_AndScreenLockStaysStrict()
    {
        var (server, superadmin) = await StartAsync();
        await using (server)
        {
            // 本人改过密码的账号不读这个参数，照常登录；有标记的账号报错（看不懂的参数不当缺省用）
            await CreateUserAsync(server, superadmin, "changed", "Changed@123456");
            var changedLogin = await AuthFlowTests.LoginAsync(server, "changed", "Changed@123456");
            var changedToken = changedLogin.GetProperty("accessToken").GetString()!;
            var (selfChanged, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/profile:change-password", """{"currentPassword":"Changed@123456","newPassword":"Changed@654321"}""", changedToken));
            Assert.Equal(HttpStatusCode.OK, selfChanged.StatusCode);
            await CreateUserAsync(server, superadmin, "created", "Created@123456");
            await ConfigureAsync(server, superadmin, "yes please", "Text");
            var (rejected, rejectedBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/auth:login", """{"userName":"created","password":"Created@123456"}"""));
            Assert.Equal((HttpStatusCode.BadRequest, "IDENTITY_VALIDATION_025"), (rejected.StatusCode, rejectedBody.GetProperty("code").GetString()));
            await AuthFlowTests.LoginAsync(server, "changed", "Changed@654321");

            // 锁屏不因此放宽：锁屏的会话不能靠改密绕过
            var (locked, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/auth:lock", """{"password":"screen-lock"}""", changedToken));
            Assert.Equal(HttpStatusCode.OK, locked.StatusCode);
            var (changeWhileLocked, changeWhileLockedBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/profile:change-password", """{"currentPassword":"Changed@654321","newPassword":"Changed@999999"}""", changedToken));
            Assert.Equal((HttpStatusCode.Forbidden, "IDENTITY_DENIED_002", "ScreenLock"), (changeWhileLocked.StatusCode, changeWhileLockedBody.GetProperty("code").GetString(), changeWhileLockedBody.GetProperty("details").GetProperty("reason").GetString()));
        }
    }
}
