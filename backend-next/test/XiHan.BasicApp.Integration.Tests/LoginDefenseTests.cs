// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.Auditing.Domain.Services;
using XiHan.BasicApp.Identity.Domain.Services;
using XiHan.BasicApp.Identity.Infrastructure;
using XiHan.BasicApp.Integration.Tests.Infrastructure;
using XiHan.Framework.Auditing.Database.Trail;

namespace XiHan.BasicApp.Integration.Tests;

/// <summary>
/// 登录防护：图形验证码（开启后必填、一次性、错码不能重试）、登录节流（按固定窗口数失败，超限连正确密码也拒绝，下个窗口恢复）、
/// 认证器双因素（绑定先暂存密钥、凭码确认；登录拿到票据而不是令牌；第二步码错计次、同一步的码不能重放、票据用一次即作废；关闭要当前的码；管理员可重置），
/// 以及这些结果都进登录日志。
/// </summary>
public sealed class LoginDefenseTests : IDisposable
{
    private const string SuperAdminLogin = """{"userName":"superadmin","password":"SuperAdmin@123"}""";

    private readonly SqliteDatabase _database = new();

    public void Dispose() => _database.Dispose();

    private sealed class FixedCodes(string code) : ICaptchaCodeSource
    {
        public string Generate(int length) => code;
    }

    private static async Task FlushAsync(BasicAppTestServer server)
    {
        await server.Services.GetRequiredService<DatabaseAuditTrail>().FlushAsync(CancellationToken.None);
        await server.Services.GetRequiredService<AuditLogWriter>().FlushAsync(CancellationToken.None);
    }

    private static async Task<List<JsonElement>> LoginLogsAsync(BasicAppTestServer server, string token)
    {
        await FlushAsync(server);
        var (response, json) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/login-logs:list", """{"page":{"index":1,"size":200}}""", token));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return json.GetProperty("items").EnumerateArray().ToList();
    }

    [Fact]
    public async Task Captcha_IsRequiredOnceEnabled_AndEachCodeWorksOnlyOnce()
    {
        await using var server = await BasicAppTestServer.StartAsync(
            _database.ConnectionString,
            settings: [new("BasicApp:Auth:CaptchaEnabled", "true")],
            configure: builder => builder.Services.AddSingleton<ICaptchaCodeSource>(new FixedCodes("4321")));
        await server.SeedDefaultsAsync();
        await server.RunSeedersAsync();

        var (config, configBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/api/auth:login-config"));
        Assert.Equal(HttpStatusCode.OK, config.StatusCode);
        Assert.True(configBody.GetProperty("captchaEnabled").GetBoolean());
        Assert.Equal(["password"], configBody.GetProperty("loginMethods").EnumerateArray().Select(static m => m.GetString()));

        // 不带验证码：凭据还没核对就拒绝
        var (missing, missingBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/auth:login", SuperAdminLogin, acceptLanguage: "zh-CN"));
        Assert.Equal(HttpStatusCode.BadRequest, missing.StatusCode);
        Assert.Equal("IDENTITY_VALIDATION_007", missingBody.GetProperty("code").GetString());
        Assert.Equal("验证码错误或已过期，请重试。", missingBody.GetProperty("detail").GetString());

        async Task<string> IssueAsync()
        {
            var (issued, issuedBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/auth:captcha", "{}"));
            Assert.Equal(HttpStatusCode.OK, issued.StatusCode);
            Assert.StartsWith("data:image/svg+xml;base64,", issuedBody.GetProperty("image").GetString(), StringComparison.Ordinal);
            Assert.Equal(300, issuedBody.GetProperty("expiresInSeconds").GetInt32());
            return issuedBody.GetProperty("captchaId").GetString()!;
        }

        string LoginWith(string id, string code) => $$"""{"userName":"superadmin","password":"SuperAdmin@123","captchaId":"{{id}}","captchaCode":"{{code}}"}""";

        // 错码作废这一枚：换回正确的码也不行
        var first = await IssueAsync();
        var (wrong, wrongBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/auth:login", LoginWith(first, "0000")));
        Assert.Equal(HttpStatusCode.BadRequest, wrong.StatusCode);
        Assert.Equal("IDENTITY_VALIDATION_007", wrongBody.GetProperty("code").GetString());
        var (replayed, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/auth:login", LoginWith(first, "4321")));
        Assert.Equal(HttpStatusCode.BadRequest, replayed.StatusCode);

        // 新的一枚、正确的码：登录成功；同一枚再用即失效
        var second = await IssueAsync();
        var (ok, okBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/auth:login", LoginWith(second, "4321")));
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        var token = okBody.GetProperty("token").GetProperty("accessToken").GetString()!;
        var (again, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/auth:login", LoginWith(second, "4321")));
        Assert.Equal(HttpStatusCode.BadRequest, again.StatusCode);

        // 验证码的拒绝记作失败的登录，带错误码
        var logs = await LoginLogsAsync(server, token);
        Assert.Contains(logs, l => l.GetProperty("event").GetString() == "Login" && !l.GetProperty("succeeded").GetBoolean() && l.GetProperty("errorCode").GetString() == "IDENTITY_VALIDATION_007");
    }

    [Fact]
    public async Task Throttle_RejectsFurtherAttemptsInTheWindow_AndRecoversInTheNextOne()
    {
        var clock = new ManualClock(DateTimeOffset.UtcNow);
        await using var server = await BasicAppTestServer.StartAsync(
            _database.ConnectionString,
            settings: [new("BasicApp:Auth:LoginThrottleAccountLimit", "2"), new("BasicApp:Auth:MaxFailedAccessAttempts", "10")],
            configure: builder => builder.Services.AddSingleton<TimeProvider>(clock));
        await server.SeedDefaultsAsync();
        await server.RunSeedersAsync();
        const string wrong = """{"userName":"superadmin","password":"nope-12345"}""";
        for (var attempt = 0; attempt < 2; attempt++)
        {
            var (denied, deniedBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/auth:login", wrong));
            Assert.Equal("IDENTITY_DENIED_003", deniedBody.GetProperty("code").GetString());
            Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
        }

        // 本窗口的两次失败用完：正确密码也直接拒绝（503，带重试秒数），不核对密码
        var (throttled, throttledBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/auth:login", SuperAdminLogin, acceptLanguage: "en-US"));
        Assert.Equal(HttpStatusCode.ServiceUnavailable, throttled.StatusCode);
        Assert.Equal("IDENTITY_UNAVAILABLE_001", throttledBody.GetProperty("code").GetString());
        Assert.StartsWith("Too many sign-in attempts", throttledBody.GetProperty("detail").GetString(), StringComparison.Ordinal);

        // 别的账号不受这个账号键影响（地址键的上限是 30）
        var (other, otherBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/auth:login", """{"userName":"nobody","password":"nope-12345"}"""));
        Assert.Equal("IDENTITY_DENIED_003", otherBody.GetProperty("code").GetString());

        // 下一个窗口恢复
        clock.Set(clock.GetUtcNow().AddSeconds(61));
        var token = (await AuthFlowTests.LoginAsync(server, "superadmin", "SuperAdmin@123")).GetProperty("accessToken").GetString()!;
        var logs = await LoginLogsAsync(server, token);
        Assert.Contains(logs, l => !l.GetProperty("succeeded").GetBoolean() && l.GetProperty("errorCode").GetString() == "IDENTITY_UNAVAILABLE_001");
        Assert.Equal(HttpStatusCode.Forbidden, other.StatusCode);
    }

    [Fact]
    public async Task Authenticator_GatesSignInWithATicket_AndRejectsReplayedOrWrongCodes()
    {
        var clock = new ManualClock(DateTimeOffset.UtcNow);
        await using var server = await BasicAppTestServer.StartAsync(_database.ConnectionString, configure: builder => builder.Services.AddSingleton<TimeProvider>(clock));
        await server.SeedDefaultsAsync();
        await server.RunSeedersAsync();
        var token = (await AuthFlowTests.LoginAsync(server, "superadmin", "SuperAdmin@123")).GetProperty("accessToken").GetString()!;

        var (profile, profileBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/api/profile", token: token));
        Assert.Equal(HttpStatusCode.OK, profile.StatusCode);
        Assert.False(profileBody.GetProperty("twoFactorEnabled").GetBoolean());
        Assert.Equal("superadmin", profileBody.GetProperty("user").GetProperty("userName").GetString());

        // 没生成密钥就开启：拒绝
        var (early, earlyBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/profile:enable-two-factor", """{"method":"Totp","code":"123456"}""", token));
        Assert.Equal(HttpStatusCode.BadRequest, early.StatusCode);
        Assert.Equal("IDENTITY_VALIDATION_009", earlyBody.GetProperty("code").GetString());

        var (setup, setupBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/profile:setup-totp", "{}", token));
        Assert.Equal(HttpStatusCode.OK, setup.StatusCode);
        var secret = setupBody.GetProperty("sharedKey").GetString()!.Replace(" ", string.Empty, StringComparison.Ordinal);
        Assert.StartsWith("otpauth://totp/XiHan%20BasicApp:superadmin?secret=" + secret, setupBody.GetProperty("authenticatorUri").GetString(), StringComparison.Ordinal);
        string CodeNow() => Totp.CodeAt(secret, Totp.StepOf(clock.GetUtcNow()));

        // 码不对不开启；码对即开启（账号此前不变）
        var (rejected, rejectedBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/profile:enable-two-factor", $$"""{"method":"Totp","code":"{{(int.Parse(CodeNow(), System.Globalization.CultureInfo.InvariantCulture) + 1) % 1_000_000:D6}}"}""", token));
        Assert.Equal(HttpStatusCode.BadRequest, rejected.StatusCode);
        Assert.Equal("IDENTITY_VALIDATION_010", rejectedBody.GetProperty("code").GetString());
        var (enabled, enabledBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/profile:enable-two-factor", $$"""{"method":"Totp","code":"{{CodeNow()}}"}""", token));
        Assert.Equal(HttpStatusCode.OK, enabled.StatusCode);
        Assert.True(enabledBody.GetProperty("twoFactorEnabled").GetBoolean());
        Assert.Equal(["Totp"], enabledBody.GetProperty("twoFactorMethods").EnumerateArray().Select(static m => m.GetString()));
        var (twice, twiceBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/profile:setup-totp", "{}", token));
        Assert.Equal(HttpStatusCode.Conflict, twice.StatusCode);
        Assert.Equal("IDENTITY_CONFLICT_004", twiceBody.GetProperty("code").GetString());

        // 登录：密码通过只拿到票据，没有令牌
        var (challenged, challengedBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/auth:login", SuperAdminLogin));
        Assert.Equal(HttpStatusCode.OK, challenged.StatusCode);
        Assert.Equal(JsonValueKind.Null, challengedBody.GetProperty("token").ValueKind);
        var challenge = challengedBody.GetProperty("twoFactor");
        Assert.Equal("Totp", challenge.GetProperty("method").GetString());
        Assert.Equal(["Totp"], challenge.GetProperty("methods").EnumerateArray().Select(static m => m.GetString()));
        Assert.False(challenge.GetProperty("codeSent").GetBoolean());
        var ticket = challenge.GetProperty("ticket").GetString()!;
        string Verify(string code) => $$"""{"ticket":"{{ticket}}","method":"Totp","code":"{{code}}"}""";

        // 开启时用过的那一步不能重放；错码计次
        var (replayed, replayedBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/auth:verify-two-factor", Verify(CodeNow())));
        Assert.Equal(HttpStatusCode.Forbidden, replayed.StatusCode);
        Assert.Equal("IDENTITY_DENIED_009", replayedBody.GetProperty("code").GetString());

        // 下一步的码：登录完成，票据作废
        clock.Set(clock.GetUtcNow().AddSeconds(Totp.PeriodSeconds));
        var (verified, verifiedBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/auth:verify-two-factor", Verify(CodeNow())));
        Assert.Equal(HttpStatusCode.OK, verified.StatusCode);
        var second = verifiedBody.GetProperty("accessToken").GetString()!;
        Assert.Equal("superadmin", verifiedBody.GetProperty("user").GetProperty("userName").GetString());
        clock.Set(clock.GetUtcNow().AddSeconds(Totp.PeriodSeconds));
        var (reused, reusedBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/auth:verify-two-factor", Verify(CodeNow())));
        Assert.Equal(HttpStatusCode.Forbidden, reused.StatusCode);
        Assert.Equal("IDENTITY_DENIED_008", reusedBody.GetProperty("code").GetString());

        // 关闭要当前认证器的码；关闭后登录直接拿令牌
        var (disabled, disabledBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/profile:disable-two-factor", $$"""{"method":"Totp","code":"{{CodeNow()}}"}""", second));
        Assert.Equal(HttpStatusCode.OK, disabled.StatusCode);
        Assert.False(disabledBody.GetProperty("twoFactorEnabled").GetBoolean());
        var plain = await AuthFlowTests.LoginAsync(server, "superadmin", "SuperAdmin@123");
        Assert.Equal(JsonValueKind.String, plain.GetProperty("accessToken").ValueKind);

        var events = (await LoginLogsAsync(server, second)).Select(static l => (Event: l.GetProperty("event").GetString(), Succeeded: l.GetProperty("succeeded").GetBoolean(), Error: l.GetProperty("errorCode").GetString())).ToList();
        Assert.Contains(("TwoFactorEnable", true, (string?)null), events);
        Assert.Contains(("TwoFactorChallenge", true, (string?)null), events);
        Assert.Contains(("Login", false, "IDENTITY_DENIED_009"), events);
        Assert.Contains(("TwoFactorDisable", true, (string?)null), events);
    }

    [Fact]
    public async Task TicketsWearOut_AndAdministratorsCanResetTwoFactor()
    {
        var clock = new ManualClock(DateTimeOffset.UtcNow);
        // 锁定与节流的上限放宽：这里只看票据自己的输错上限
        var (server, _, tenant) = await SettingsTestHost.StartAsync(_database.ConnectionString, configure: builder =>
        {
            builder.Services.AddSingleton<TimeProvider>(clock);
            builder.Configuration.AddInMemoryCollection([new("BasicApp:Auth:MaxFailedAccessAttempts", "20"), new("BasicApp:Auth:LoginThrottleAccountLimit", "20")]);
        });
        await using var running = server;
        var (created, createdBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/users", """{"userName":"bob","displayName":"Bob","password":"Bob@123456"}""", tenant));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var bobId = createdBody.GetProperty("id").GetString()!;
        var bob = (await AuthFlowTests.LoginAsync(server, "bob", "Bob@123456", "contoso")).GetProperty("accessToken").GetString()!;
        var (_, setupBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/profile:setup-totp", "{}", bob));
        var secret = setupBody.GetProperty("sharedKey").GetString()!.Replace(" ", string.Empty, StringComparison.Ordinal);
        var (enabled, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/profile:enable-two-factor", $$"""{"method":"Totp","code":"{{Totp.CodeAt(secret, Totp.StepOf(clock.GetUtcNow()))}}"}""", bob));
        Assert.Equal(HttpStatusCode.OK, enabled.StatusCode);

        // 一张票据最多输错 5 次：第 5 次即作废，之后连对的码也不认
        var (_, challengedBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/auth:login", """{"userName":"bob","password":"Bob@123456","tenant":"contoso"}"""));
        var ticket = challengedBody.GetProperty("twoFactor").GetProperty("ticket").GetString()!;
        for (var attempt = 1; attempt <= 4; attempt++)
        {
            var (_, wrongBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/auth:verify-two-factor", $$"""{"ticket":"{{ticket}}","method":"Totp","code":"000000"}"""));
            Assert.Equal("IDENTITY_DENIED_009", wrongBody.GetProperty("code").GetString());
        }

        var (worn, wornBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/auth:verify-two-factor", $$"""{"ticket":"{{ticket}}","method":"Totp","code":"000000"}"""));
        Assert.Equal(HttpStatusCode.Forbidden, worn.StatusCode);
        Assert.Equal("IDENTITY_DENIED_008", wornBody.GetProperty("code").GetString());

        // 管理员重置：不能重置自己；重置后 Bob 只凭密码登录
        var (self, selfBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/api/auth:me", token: tenant));
        var adminId = selfBody.GetProperty("user").GetProperty("id").GetString()!;
        Assert.Equal(HttpStatusCode.OK, self.StatusCode);
        var (reset, resetBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/users:reset-two-factor", $$"""{"ids":["{{bobId}}","{{adminId}}"]}""", tenant));
        Assert.Equal(HttpStatusCode.OK, reset.StatusCode);
        Assert.Equal([bobId], resetBody.GetProperty("succeeded").EnumerateArray().Select(static id => id.GetString()));
        Assert.Equal("IDENTITY_CONFLICT_002", resetBody.GetProperty("failed")[0].GetProperty("code").GetString());
        var plain = await AuthFlowTests.LoginAsync(server, "bob", "Bob@123456", "contoso");
        Assert.Equal(JsonValueKind.String, plain.GetProperty("accessToken").ValueKind);

        var logs = await LoginLogsAsync(server, tenant);
        Assert.Contains(logs, l => l.GetProperty("event").GetString() == "TwoFactorReset" && l.GetProperty("userName").GetString() == "bob");
    }
}
