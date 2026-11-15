// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Net;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.Contracts;
using XiHan.BasicApp.Identity.Domain.Entities;
using XiHan.BasicApp.Identity.Domain.Services;
using XiHan.BasicApp.Integration.Tests.Infrastructure;
using XiHan.BasicApp.Settings.Domain.Entities;
using XiHan.BasicApp.Settings.Infrastructure;
using XiHan.Framework.Domain.Specifications;
using XiHan.Framework.Persistence.Abstractions.Queries;
using XiHan.Framework.Persistence.Abstractions.Repositories;

namespace XiHan.BasicApp.Integration.Tests;

/// <summary>
/// 验证密码策略：复杂度、历史深度与过期
/// </summary>
/// <remarks>
/// 规则都在系统参数 <c>identity.password</c>：缺省至少 8 位、须含字母与数字、不得与最近 3 个相同、不过期；
/// 登录页配置带出同一份规则；写错的值保存时即拒绝；过期后用密码登录锁到改密为止，提醒说明是过期
/// </remarks>
public sealed class PasswordPolicyTests : IDisposable
{
    /// <summary>
    /// 保存时拒绝写错的值：不是 JSON 对象是类型不符，字段不对或越界是不符合要求
    /// </summary>
    private static readonly string[] _rejections = ["SETTINGS_VALIDATION_002", "SETTINGS_VALIDATION_011"];

    private readonly SqliteDatabase _database = new();

    public void Dispose() => _database.Dispose();

    private async Task<(BasicAppTestServer Server, string Superadmin)> StartAsync()
    {
        var server = await BasicAppTestServer.StartAsync(_database.ConnectionString);
        await server.SeedDefaultsAsync();
        await server.RunSeedersAsync();
        return (server, (await AuthFlowTests.LoginAsync(server, "superadmin", "SuperAdmin@123")).GetProperty("accessToken").GetString()!);
    }

    /// <summary>
    /// 把账号最近一次改密的时间挪到 <paramref name="days"/> 天前
    /// </summary>
    /// <remarks>
    /// 签发令牌用真实时间，拨钟会让新令牌还没生效，所以挪改密时间而不拨钟
    /// </remarks>
    private static async Task BackdateAsync(BasicAppTestServer server, string userName, int days)
        => await SettingsTestHost.InvokeAsync(server, null, async ctx =>
        {
            var users = ctx.Services.GetRequiredService<IRepository<SysUser, Guid>>();
            var securities = ctx.Services.GetRequiredService<IRepository<SysUserSecurity, Guid>>();
            var user = (await users.ListAsync(EntityQuery.All<SysUser>().Where(new Specification<SysUser>(u => u.UserName == userName)).Page(0, 1), ctx.CancellationToken))[0];
            var security = (await securities.ListAsync(EntityQuery.All<SysUserSecurity>().Where(new Specification<SysUserSecurity>(s => s.UserId == user.Id)).Page(0, 1), ctx.CancellationToken))[0];
            security.PasswordChangedAt = DateTimeOffset.UtcNow.AddDays(-days);
            await securities.UpdateAsync(security, ctx.CancellationToken);
            return 0;
        });

    private static async Task ConfigureAsync(BasicAppTestServer server, string superadmin, string value)
    {
        var (updated, body) = await SettingsTestHost.SetBuiltInConfigAsync(server, superadmin, PasswordSettings.ConfigKey, value);
        Assert.True(updated == HttpStatusCode.OK, body.ToString());
    }

    private static async Task<(HttpStatusCode Status, string? Code, string? Detail)> ResetAsync(BasicAppTestServer server, string superadmin, string userId, string password)
    {
        var (response, body) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, $"/api/users/{userId}:reset-password", JsonSerializer.Serialize(new { newPassword = password }), superadmin, "en-US"));
        return response.StatusCode == HttpStatusCode.OK
            ? (response.StatusCode, null, null)
            : (response.StatusCode, body.GetProperty("code").GetString(), body.GetProperty("detail").GetString());
    }

    private static async Task<string> CreateUserAsync(BasicAppTestServer server, string superadmin, string userName, string password)
    {
        var (created, body) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/users", JsonSerializer.Serialize(new { userName, displayName = userName, password }), superadmin));
        Assert.True(created.StatusCode == HttpStatusCode.Created, body.ToString());
        return body.GetProperty("id").GetString()!;
    }

    [Fact]
    public async Task Complexity_FollowsTheParameter_AndTheLoginPageSeesTheSameRules()
    {
        var (server, superadmin) = await StartAsync();
        await using (server)
        {
            var bob = await CreateUserAsync(server, superadmin, "bob", "Bob@123456");

            // 缺省：至少 8 位、须含字母与数字；登录页配置带出同样的规则
            var (_, config) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/api/auth:login-config"));
            Assert.Equal("""{"minLength":8,"requireLetter":true,"requireDigit":true,"requireUppercase":false,"requireLowercase":false,"requireSymbol":false}""", config.GetProperty("passwordPolicy").GetRawText());
            Assert.Equal((HttpStatusCode.BadRequest, "IDENTITY_VALIDATION_003", "The password must be at least 8 characters long."), await ResetAsync(server, superadmin, bob, "Ab1"));
            Assert.Equal((HttpStatusCode.BadRequest, "IDENTITY_VALIDATION_003", "The password must contain a digit."), await ResetAsync(server, superadmin, bob, "abcdefgh"));
            Assert.Equal((HttpStatusCode.OK, null, null), await ResetAsync(server, superadmin, bob, "abcdefg1"));

            // 收紧：12 位、大小写与符号都要，按顺序报第一条不满足的
            await ConfigureAsync(server, superadmin, """{"minLength":12,"requireUppercase":true,"requireLowercase":true,"requireSymbol":true}""");
            var (_, strict) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/api/auth:login-config"));
            Assert.Equal((12, true, true), (strict.GetProperty("passwordPolicy").GetProperty("minLength").GetInt32(), strict.GetProperty("passwordPolicy").GetProperty("requireUppercase").GetBoolean(), strict.GetProperty("passwordPolicy").GetProperty("requireSymbol").GetBoolean()));
            Assert.Equal("The password must be at least 12 characters long.", (await ResetAsync(server, superadmin, bob, "abcdefg12")).Detail);
            Assert.Equal("The password must contain an uppercase letter.", (await ResetAsync(server, superadmin, bob, "abcdefgh1234")).Detail);
            Assert.Equal("The password must contain a lowercase letter.", (await ResetAsync(server, superadmin, bob, "ABCDEFGH1234")).Detail);
            Assert.Equal("The password must contain a character other than letters and digits.", (await ResetAsync(server, superadmin, bob, "Abcdefgh1234")).Detail);
            Assert.Equal((HttpStatusCode.OK, null, null), await ResetAsync(server, superadmin, bob, "Abcdefgh-1234"));

            // 放宽：6 位、只要字母（请求不再自带 8 位的下限），不查历史时可以换回当前密码
            await ConfigureAsync(server, superadmin, """{"minLength":6,"requireDigit":false,"historyDepth":0}""");
            Assert.Equal((HttpStatusCode.OK, null, null), await ResetAsync(server, superadmin, bob, "abcdef"));
            Assert.Equal((HttpStatusCode.OK, null, null), await ResetAsync(server, superadmin, bob, "abcdef"));

            // 历史深度按参数：最近 1 个不能用，再早的可以
            await ConfigureAsync(server, superadmin, """{"minLength":6,"requireDigit":false,"historyDepth":1}""");
            Assert.Equal((HttpStatusCode.BadRequest, "IDENTITY_VALIDATION_004", "The password must differ from the last 1 passwords."), await ResetAsync(server, superadmin, bob, "abcdef"));
            Assert.Equal((HttpStatusCode.OK, null, null), await ResetAsync(server, superadmin, bob, "ghijkl"));
            Assert.Equal((HttpStatusCode.OK, null, null), await ResetAsync(server, superadmin, bob, "Abcdefgh-1234"));

            // 导入用户的初始密码同样按参数
            var (import, importBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/users:import", """{"rows":[{"userName":"imp1","displayName":"imp1"}],"password":"12345"}""", superadmin));
            Assert.Equal((HttpStatusCode.BadRequest, "IDENTITY_VALIDATION_003"), (import.StatusCode, importBody.GetProperty("code").GetString()));
        }
    }

    [Fact]
    public async Task WrongValues_AreRejectedWhenSaved_AndAValueBrokenInTheStoreStopsPasswordSettingAndLogin()
    {
        var (server, superadmin) = await StartAsync();
        await using (server)
        {
            foreach (var wrong in new[] { """{"forceChange":"yes please"}""", """{"minLength":4}""", """{"minLength":200}""", """{"historyDepth":1001}""", """{"expiryDays":-1}""", """{"expiryDays":3651}""", "not json" })
            {
                var (status, body) = await SettingsTestHost.SetBuiltInConfigAsync(server, superadmin, PasswordSettings.ConfigKey, wrong);
                Assert.Equal(HttpStatusCode.BadRequest, status);
                Assert.Contains(body.GetProperty("code").GetString(), _rejections);
            }

            Assert.Equal("SETTINGS_VALIDATION_011", (await SettingsTestHost.SetBuiltInConfigAsync(server, superadmin, PasswordSettings.ConfigKey, """{"minLength":4}""")).Body.GetProperty("code").GetString());
            Assert.False((await SettingsTestHost.BuiltInConfigAsync(server, superadmin, PasswordSettings.ConfigKey)).GetProperty("hasValue").GetBoolean());

            // 绕过参数配置改坏了库里的值：设密码与用密码登录都报参数写错，已登录的会话照常
            await SettingsTestHost.InvokeAsync(server, null, async ctx =>
            {
                var configs = ctx.Services.GetRequiredService<IRepository<SysConfig, Guid>>();
                var row = (await configs.ListAsync(EntityQuery.All<SysConfig>().Where(new Specification<SysConfig>(c => c.Key == PasswordSettings.ConfigKey)).Page(0, 1), ctx.CancellationToken))[0];
                row.Value = """{"minLength":2}""";
                await configs.UpdateAsync(row, ctx.CancellationToken);
                await ctx.Services.GetRequiredService<ConfigEntries>().InvalidateAsync(TenantIds.Platform, PasswordSettings.ConfigKey, ctx.CancellationToken);
                return 0;
            });
            var (login, loginBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/auth:login", """{"userName":"superadmin","password":"SuperAdmin@123"}"""));
            Assert.Equal((HttpStatusCode.BadRequest, "IDENTITY_VALIDATION_025"), (login.StatusCode, loginBody.GetProperty("code").GetString()));
            var (me, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/api/auth:me", token: superadmin));
            Assert.Equal(HttpStatusCode.OK, me.StatusCode);
        }
    }

    [Fact]
    public async Task ExpiredPasswords_LockThePasswordLoginUntilChanged_AndTheNoticeSaysWhy()
    {
        var (server, superadmin) = await StartAsync();
        await using (server)
        {
            await CreateUserAsync(server, superadmin, "ann", "Ann@123456");
            var first = await AuthFlowTests.LoginAsync(server, "ann", "Ann@123456");
            var (changed, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/profile:change-password", """{"currentPassword":"Ann@123456","newPassword":"Ann@654321"}""", first.GetProperty("accessToken").GetString()));
            Assert.Equal(HttpStatusCode.OK, changed.StatusCode);
            await ConfigureAsync(server, superadmin, """{"expiryDays":30}""");

            // 有效期内照常
            await BackdateAsync(server, "ann", 29);
            Assert.False((await AuthFlowTests.LoginAsync(server, "ann", "Ann@654321")).GetProperty("mustChangePassword").GetBoolean());

            // 过了有效期：会话锁到改密为止，提醒说明是过期
            await BackdateAsync(server, "ann", 31);
            var expired = await AuthFlowTests.LoginAsync(server, "ann", "Ann@654321");
            Assert.True(expired.GetProperty("mustChangePassword").GetBoolean());
            var token = expired.GetProperty("accessToken").GetString()!;
            var (locked, lockedBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/api/auth:me", token: token, acceptLanguage: "en-US"));
            Assert.Equal((HttpStatusCode.Forbidden, "IDENTITY_DENIED_002", "PasswordChangeRequired"), (locked.StatusCode, lockedBody.GetProperty("code").GetString(), lockedBody.GetProperty("details").GetProperty("reason").GetString()));
            Assert.Equal("The platform's password rules require you to change your password first.", lockedBody.GetProperty("detail").GetString());

            var (renewed, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/profile:change-password", """{"currentPassword":"Ann@654321","newPassword":"Ann@777777"}""", token));
            Assert.Equal(HttpStatusCode.OK, renewed.StatusCode);
            var (inbox, inboxBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/inbox:list", "{}", token, "en-US"));
            Assert.Equal(HttpStatusCode.OK, inbox.StatusCode);
            var notice = Assert.Single(inboxBody.GetProperty("items").EnumerateArray());
            Assert.Equal(("Security", "Your password has expired"), (notice.GetProperty("notificationType").GetString(), notice.GetProperty("title").GetString()));

            // 改完重新计时；关掉有效期即不再锁
            Assert.False((await AuthFlowTests.LoginAsync(server, "ann", "Ann@777777")).GetProperty("mustChangePassword").GetBoolean());
            await BackdateAsync(server, "ann", 365);
            Assert.True((await AuthFlowTests.LoginAsync(server, "ann", "Ann@777777")).GetProperty("mustChangePassword").GetBoolean());
            await ConfigureAsync(server, superadmin, """{"expiryDays":0}""");
            Assert.False((await AuthFlowTests.LoginAsync(server, "ann", "Ann@777777")).GetProperty("mustChangePassword").GetBoolean());
        }
    }
}
