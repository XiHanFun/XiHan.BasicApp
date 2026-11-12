// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using XiHan.BasicApp.Identity.Domain.Services;
using XiHan.BasicApp.Integration.Tests.Infrastructure;
using XiHan.Framework.Testing;

namespace XiHan.BasicApp.Integration.Tests;

/// <summary>
/// 验证通行密钥的登记与登录
/// </summary>
/// <remarks>
/// 宿主配了依赖方、系统参数 <c>identity.login-methods</c> 列出 <c>passkey</c> 才开放；登记先核对当前密码、成功后发安全通知；
/// 登录不用填账号，按凭据找到账号与注册地；认证器做了用户验证即顶替双因素；票据只能用一次，签名计数回退、签名不对、删除了的凭据、用户句柄不是凭据所属用户都登录不了
/// </remarks>
public sealed class PasskeyTests : IDisposable
{
    private const string RelyingParty = "localhost";
    private const string Origin = "http://localhost:9100";

    private readonly SqliteDatabase _database = new();

    public void Dispose() => _database.Dispose();

    private static Action<WebApplicationBuilder> WithPasskeys(string userVerification = "Required") => builder => builder.Configuration.AddInMemoryCollection(
    [
        new("XiHan:Security:WebAuthn:RelyingPartyId", RelyingParty),
        new("XiHan:Security:WebAuthn:Origins:0", Origin),
        new("XiHan:Security:WebAuthn:UserVerification", userVerification),
    ]);

    private static async Task<(HttpStatusCode Status, JsonElement Body)> SendAsync(BasicAppTestServer server, HttpMethod method, string path, object? body, string? token = null)
    {
        var (response, json) = await server.SendAsync(BasicAppTestServer.Request(method, path, body is null ? null : JsonSerializer.Serialize(body, JsonSerializerOptions.Web), token));
        return (response.StatusCode, json);
    }

    private static async Task<string?> CodeOfAsync(BasicAppTestServer server, HttpMethod method, string path, object? body, string? token = null)
        => (await SendAsync(server, method, path, body, token)).Body.TryGetProperty("code", out var code) ? code.GetString() : null;

    private static async Task<List<string>> MethodsAsync(BasicAppTestServer server)
        => [.. (await SendAsync(server, HttpMethod.Get, "/api/auth:login-config", null)).Body.GetProperty("loginMethods").EnumerateArray().Select(static m => m.GetString()!)];

    private static async Task OpenPasskeysAsync(BasicAppTestServer server, string platform)
    {
        var (created, _) = await SendAsync(server, HttpMethod.Post, "/api/configs", new { key = LoginMethods.ConfigKey, name = "Login methods", dataType = "Text", value = "passkey" }, platform);
        Assert.Equal(HttpStatusCode.Created, created);
    }

    /// <summary>
    /// 登记一个通行密钥
    /// </summary>
    /// <returns>登记结果与创建选项里的用户句柄（认证器登录时交回）</returns>
    private static async Task<(JsonElement Passkey, string UserHandle)> RegisterAsync(BasicAppTestServer server, string token, string password, WebAuthnTestAuthenticator authenticator, string? name = null)
    {
        var (begun, options) = await SendAsync(server, HttpMethod.Post, "/api/my-passkeys:begin-create", new { currentPassword = password }, token);
        Assert.Equal(HttpStatusCode.OK, begun);
        var credential = authenticator.Register(new WebAuthnTestCeremony { Challenge = options.GetProperty("options").GetProperty("challenge").GetString()!, Origin = Origin, RelyingPartyId = RelyingParty });
        var (created, passkey) = await SendAsync(server, HttpMethod.Post, "/api/my-passkeys", new { ticket = options.GetProperty("ticket").GetString(), name, credential }, token);
        Assert.Equal(HttpStatusCode.Created, created);
        return (passkey, options.GetProperty("options").GetProperty("user").GetProperty("id").GetString()!);
    }

    private static async Task<(string Ticket, string Challenge)> BeginLoginAsync(BasicAppTestServer server)
    {
        var (begun, options) = await SendAsync(server, HttpMethod.Post, "/api/auth:begin-passkey-login", new { });
        Assert.Equal(HttpStatusCode.OK, begun);
        Assert.Empty(options.GetProperty("options").GetProperty("allowCredentials").EnumerateArray());
        return (options.GetProperty("ticket").GetString()!, options.GetProperty("options").GetProperty("challenge").GetString()!);
    }

    /// <summary>
    /// 用通行密钥登录
    /// </summary>
    /// <param name="server">宿主</param>
    /// <param name="authenticator">测试用认证器</param>
    /// <param name="userHandle">断言交回的用户句柄；可发现凭据总会交回登记时的句柄，为空即不交回</param>
    /// <param name="userVerified">认证器是否做了用户验证</param>
    /// <param name="corrupt">是否把签名改坏</param>
    private static async Task<(HttpStatusCode Status, JsonElement Body)> LoginAsync(BasicAppTestServer server, WebAuthnTestAuthenticator authenticator, string? userHandle, bool userVerified = true, bool corrupt = false)
    {
        var (ticket, challenge) = await BeginLoginAsync(server);
        var credential = authenticator.Authenticate(new WebAuthnTestCeremony { Challenge = challenge, Origin = Origin, RelyingPartyId = RelyingParty, UserVerified = userVerified, CorruptSignature = corrupt, UserHandle = userHandle });
        return await SendAsync(server, HttpMethod.Post, "/api/auth:login-by-passkey", new { ticket, credential });
    }

    [Fact]
    public async Task Passkeys_AreRegisteredWithThePassword_SignInWithoutAnAccount_AndStandInForTwoFactor()
    {
        var (server, platform, _) = await SettingsTestHost.StartAsync(_database.ConnectionString, configure: WithPasskeys());
        await using var running = server;
        await SendAsync(server, HttpMethod.Post, "/api/users", new { userName = "ann", displayName = "Ann", password = "Pass@123456" }, platform);
        await SendAsync(server, HttpMethod.Post, "/api/users", new { userName = "bob", displayName = "Bob", password = "Pass@123456" }, platform);
        var ann = (await AuthFlowTests.LoginAsync(server, "ann", "Pass@123456")).GetProperty("accessToken").GetString()!;
        var bob = (await AuthFlowTests.LoginAsync(server, "bob", "Pass@123456")).GetProperty("accessToken").GetString()!;

        // 没开放：登录页没有通行密钥，登记与登录都被拒
        Assert.Equal(["password"], await MethodsAsync(server));
        Assert.Equal("IDENTITY_DENIED_016", await CodeOfAsync(server, HttpMethod.Post, "/api/auth:begin-passkey-login", new { }));
        Assert.Equal("IDENTITY_DENIED_016", await CodeOfAsync(server, HttpMethod.Post, "/api/my-passkeys:begin-create", new { currentPassword = "Pass@123456" }, ann));
        await OpenPasskeysAsync(server, platform);
        Assert.Equal(["password", "passkey"], await MethodsAsync(server));

        // 登记：密码错不给选项；票据只认发起的人、只能用一次；成功后收件箱里有一条安全通知
        Assert.Equal("IDENTITY_DENIED_003", await CodeOfAsync(server, HttpMethod.Post, "/api/my-passkeys:begin-create", new { currentPassword = "wrong" }, ann));
        using var laptop = new WebAuthnTestAuthenticator();
        var (_, begun) = await SendAsync(server, HttpMethod.Post, "/api/my-passkeys:begin-create", new { currentPassword = "Pass@123456" }, ann);
        var options = begun.GetProperty("options");
        Assert.Equal(RelyingParty, options.GetProperty("rp").GetProperty("id").GetString());
        Assert.Equal("ann", options.GetProperty("user").GetProperty("name").GetString());
        var annHandle = options.GetProperty("user").GetProperty("id").GetString()!;
        Assert.Equal("required", options.GetProperty("authenticatorSelection").GetProperty("userVerification").GetString());
        var credential = laptop.Register(new WebAuthnTestCeremony { Challenge = options.GetProperty("challenge").GetString()!, Origin = Origin, RelyingPartyId = RelyingParty });
        var ticket = begun.GetProperty("ticket").GetString();
        Assert.Equal("IDENTITY_VALIDATION_028", await CodeOfAsync(server, HttpMethod.Post, "/api/my-passkeys", new { ticket, credential }, bob));
        var (created, passkey) = await SendAsync(server, HttpMethod.Post, "/api/my-passkeys", new { ticket = (await SendAsync(server, HttpMethod.Post, "/api/my-passkeys:begin-create", new { currentPassword = "Pass@123456" }, ann)).Body.GetProperty("ticket").GetString(), credential }, ann);
        Assert.Equal("SECURITY_DENIED_005", passkey.GetProperty("code").GetString());
        Assert.Equal(HttpStatusCode.Forbidden, created);
        var (registered, registeredHandle) = await RegisterAsync(server, ann, "Pass@123456", laptop, "Laptop");
        Assert.Equal(annHandle, registeredHandle);
        Assert.Equal("Laptop", registered.GetProperty("name").GetString());
        var (_, inbox) = await SendAsync(server, HttpMethod.Post, "/api/inbox:list", new { }, ann);
        var notice = Assert.Single(inbox.GetProperty("items").EnumerateArray());
        Assert.Equal("Security", notice.GetProperty("notificationType").GetString());
        Assert.Contains("Laptop", notice.GetProperty("content").GetString(), StringComparison.Ordinal);

        // 同一个认证器不再登记第二次：创建选项把它列进排除表
        var (_, again) = await SendAsync(server, HttpMethod.Post, "/api/my-passkeys:begin-create", new { currentPassword = "Pass@123456" }, ann);
        Assert.Equal(laptop.CredentialId, Assert.Single(again.GetProperty("options").GetProperty("excludeCredentials").EnumerateArray()).GetProperty("id").GetString());

        // 登录不用填账号：凭据找到 ann；用过的票据作废
        var (signedIn, signedInBody) = await LoginAsync(server, laptop, annHandle);
        Assert.Equal(HttpStatusCode.OK, signedIn);
        var token = signedInBody.GetProperty("token").GetProperty("accessToken").GetString()!;
        Assert.Equal("ann", (await SendAsync(server, HttpMethod.Get, "/api/auth:me", null, token)).Body.GetProperty("user").GetProperty("userName").GetString());
        var (replayTicket, replayChallenge) = await BeginLoginAsync(server);
        var assertion = laptop.Authenticate(new WebAuthnTestCeremony { Challenge = replayChallenge, Origin = Origin, RelyingPartyId = RelyingParty, UserHandle = annHandle });
        Assert.Equal(HttpStatusCode.OK, (await SendAsync(server, HttpMethod.Post, "/api/auth:login-by-passkey", new { ticket = replayTicket, credential = assertion })).Status);
        Assert.Equal("IDENTITY_VALIDATION_028", await CodeOfAsync(server, HttpMethod.Post, "/api/auth:login-by-passkey", new { ticket = replayTicket, credential = assertion }));
        var listed = Assert.Single((await SendAsync(server, HttpMethod.Get, "/api/my-passkeys", null, ann)).Body.GetProperty("items").EnumerateArray());
        Assert.Equal(JsonValueKind.String, listed.GetProperty("lastUsedAt").ValueKind);

        // 开了双因素：做了用户验证的通行密钥直接登录，不要第二步
        var (_, setup) = await SendAsync(server, HttpMethod.Post, "/api/profile:setup-totp", new { }, ann);
        var secret = setup.GetProperty("sharedKey").GetString()!.Replace(" ", string.Empty, StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.OK, (await SendAsync(server, HttpMethod.Post, "/api/profile:enable-two-factor", new { method = "Totp", code = Totp.CodeAt(secret, Totp.StepOf(DateTimeOffset.UtcNow)) }, ann)).Status);
        var (_, verified) = await LoginAsync(server, laptop, annHandle);
        Assert.Equal(JsonValueKind.Object, verified.GetProperty("token").ValueKind);

        // 签名不对、签名计数回退（克隆的认证器）：都登录不了
        Assert.Equal("SECURITY_DENIED_005", (await LoginAsync(server, laptop, annHandle, corrupt: true)).Body.GetProperty("code").GetString());
        laptop.SignCount = 5;
        Assert.Equal(HttpStatusCode.OK, (await LoginAsync(server, laptop, annHandle)).Status);
        laptop.SignCount = 3;
        Assert.Equal("SECURITY_DENIED_006", (await LoginAsync(server, laptop, annHandle)).Body.GetProperty("code").GetString());
        laptop.SignCount = 6;

        // 没登记的认证器、别人的通行密钥：登录不了，也改不了、删不了
        using var stranger = new WebAuthnTestAuthenticator();
        Assert.Equal("IDENTITY_DENIED_022", (await LoginAsync(server, stranger, annHandle)).Body.GetProperty("code").GetString());
        var id = registered.GetProperty("id").GetString();
        Assert.Equal("IDENTITY_NOTFOUND_006", await CodeOfAsync(server, HttpMethod.Put, $"/api/my-passkeys/{id}", new { name = "Mine now" }, bob));
        var (_, renamed) = await SendAsync(server, HttpMethod.Put, $"/api/my-passkeys/{id}", new { name = "Work laptop" }, ann);
        Assert.Equal("Work laptop", renamed.GetProperty("name").GetString());
        var (_, bobDeleted) = await SendAsync(server, HttpMethod.Delete, $"/api/my-passkeys?ids={id}", null, bob);
        Assert.Equal("IDENTITY_NOTFOUND_006", Assert.Single(bobDeleted.GetProperty("failed").EnumerateArray()).GetProperty("code").GetString());

        // 删除之后立即不能登录
        var (_, deleted) = await SendAsync(server, HttpMethod.Delete, $"/api/my-passkeys?ids={id}", null, ann);
        Assert.Equal(id, Assert.Single(deleted.GetProperty("succeeded").EnumerateArray()).GetString());
        Assert.Equal("IDENTITY_DENIED_022", (await LoginAsync(server, laptop, annHandle)).Body.GetProperty("code").GetString());
        Assert.Empty((await SendAsync(server, HttpMethod.Get, "/api/my-passkeys", null, ann)).Body.GetProperty("items").EnumerateArray());
    }

    [Fact]
    public async Task TenantPasskeys_SignIntoTheirHome_AndWithoutUserVerificationTwoFactorStillApplies()
    {
        var (server, platform, tenant) = await SettingsTestHost.StartAsync(_database.ConnectionString, configure: WithPasskeys(userVerification: "Preferred"));
        await using var running = server;
        await OpenPasskeysAsync(server, platform);
        await SendAsync(server, HttpMethod.Post, "/api/users", new { userName = "cara", displayName = "Cara", password = "Pass@123456" }, tenant);
        var cara = (await AuthFlowTests.LoginAsync(server, "cara", "Pass@123456", "contoso")).GetProperty("accessToken").GetString()!;
        using var phone = new WebAuthnTestAuthenticator();
        var (_, caraHandle) = await RegisterAsync(server, cara, "Pass@123456", phone);

        // 租户账号：认证器上的名字带注册地，登录进注册地
        var (_, begun) = await SendAsync(server, HttpMethod.Post, "/api/my-passkeys:begin-create", new { currentPassword = "Pass@123456" }, cara);
        Assert.Equal("cara@contoso", begun.GetProperty("options").GetProperty("user").GetProperty("name").GetString());
        var (signedIn, signedInBody) = await LoginAsync(server, phone, caraHandle);
        Assert.Equal(HttpStatusCode.OK, signedIn);
        Assert.Equal("contoso", signedInBody.GetProperty("token").GetProperty("tenantId").GetString());
        Assert.Equal("Contoso", signedInBody.GetProperty("token").GetProperty("tenantName").GetString());

        // 开了双因素：没做用户验证即要第二步
        var (_, setup) = await SendAsync(server, HttpMethod.Post, "/api/profile:setup-totp", new { }, cara);
        var secret = setup.GetProperty("sharedKey").GetString()!.Replace(" ", string.Empty, StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.OK, (await SendAsync(server, HttpMethod.Post, "/api/profile:enable-two-factor", new { method = "Totp", code = Totp.CodeAt(secret, Totp.StepOf(DateTimeOffset.UtcNow)) }, cara)).Status);
        var (_, challenged) = await LoginAsync(server, phone, caraHandle, userVerified: false);
        Assert.Equal(JsonValueKind.Null, challenged.GetProperty("token").ValueKind);
        Assert.Equal(["Totp"], challenged.GetProperty("twoFactor").GetProperty("methods").EnumerateArray().Select(static m => m.GetString()));
        var (_, verified) = await LoginAsync(server, phone, caraHandle);
        Assert.Equal(JsonValueKind.Object, verified.GetProperty("token").ValueKind);
    }

    [Fact]
    public async Task PasskeyLogin_IsDenied_WhenTheUserHandleIsNotTheCredentialsOwner()
    {
        var (server, platform, _) = await SettingsTestHost.StartAsync(_database.ConnectionString, configure: WithPasskeys());
        await using var running = server;
        await OpenPasskeysAsync(server, platform);
        await SendAsync(server, HttpMethod.Post, "/api/users", new { userName = "ann", displayName = "Ann", password = "Pass@123456" }, platform);
        await SendAsync(server, HttpMethod.Post, "/api/users", new { userName = "bob", displayName = "Bob", password = "Pass@123456" }, platform);
        var ann = (await AuthFlowTests.LoginAsync(server, "ann", "Pass@123456")).GetProperty("accessToken").GetString()!;
        var bob = (await AuthFlowTests.LoginAsync(server, "bob", "Pass@123456")).GetProperty("accessToken").GetString()!;
        using var annKey = new WebAuthnTestAuthenticator();
        using var bobKey = new WebAuthnTestAuthenticator();
        var (_, annHandle) = await RegisterAsync(server, ann, "Pass@123456", annKey);
        var (_, bobHandle) = await RegisterAsync(server, bob, "Pass@123456", bobKey);
        Assert.NotEqual(annHandle, bobHandle);

        // bob 自己的通行密钥签名合法，交回 ann 的句柄或不交回句柄：都不能登录
        var (claimed, claimedBody) = await LoginAsync(server, bobKey, annHandle);
        Assert.Equal(HttpStatusCode.Forbidden, claimed);
        Assert.Equal("SECURITY_DENIED_005", claimedBody.GetProperty("code").GetString());
        Assert.Equal("SECURITY_DENIED_005", (await LoginAsync(server, bobKey, userHandle: null)).Body.GetProperty("code").GetString());

        // 交回自己的句柄才登录，且登录的是凭据的所属用户
        var (signedIn, signedInBody) = await LoginAsync(server, bobKey, bobHandle);
        Assert.Equal(HttpStatusCode.OK, signedIn);
        var token = signedInBody.GetProperty("token").GetProperty("accessToken").GetString()!;
        Assert.Equal("bob", (await SendAsync(server, HttpMethod.Get, "/api/auth:me", null, token)).Body.GetProperty("user").GetProperty("userName").GetString());
    }

    [Fact]
    public async Task WithoutAHostConfiguration_PasskeysStayClosed_EvenWhenListed()
    {
        var (server, platform, _) = await SettingsTestHost.StartAsync(_database.ConnectionString);
        await using var running = server;
        await OpenPasskeysAsync(server, platform);

        Assert.Equal(["password"], await MethodsAsync(server));
        Assert.Equal("IDENTITY_UNAVAILABLE_007", await CodeOfAsync(server, HttpMethod.Post, "/api/auth:begin-passkey-login", new { }));
        Assert.Equal("IDENTITY_UNAVAILABLE_007", await CodeOfAsync(server, HttpMethod.Post, "/api/my-passkeys:begin-create", new { currentPassword = "SuperAdmin@123" }, platform));
        Assert.Empty((await SendAsync(server, HttpMethod.Get, "/api/my-passkeys", null, platform)).Body.GetProperty("items").EnumerateArray());
    }
}
