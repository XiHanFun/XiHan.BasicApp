// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Net;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.Access.Infrastructure;
using XiHan.BasicApp.Auditing;
using XiHan.BasicApp.Chat.Domain.Services;
using XiHan.BasicApp.Contracts.Settings;
using XiHan.BasicApp.Identity.Domain.Services;
using XiHan.BasicApp.Integration.Tests.Infrastructure;
using XiHan.BasicApp.Notification.Infrastructure.Telegram;
using XiHan.BasicApp.Runtime.Seeding;
using XiHan.BasicApp.Settings.Domain.Entities;
using XiHan.Framework.Persistence.Abstractions.Repositories;

namespace XiHan.BasicApp.Integration.Tests;

/// <summary>
/// 验证系统参数的覆盖、校验、加密与生效值读取
/// </summary>
/// <remarks>
/// 平台参数与租户同键覆盖（不允许覆盖时被拒）、值按数据类型校验、加密值不回显也不明文落库、更新时加密的保留与开关、
/// 读共享写不共享（另一侧的参数改不了也删不了）、生效值读取器（租户优先、停用即回落平台、空值回落缺省值）；
/// 各包登记的内建参数由种子写入（元数据每次对齐、值归运营），先手建的同键参数值符合类型才收为内建
/// </remarks>
public sealed class SettingsConfigsTests : IDisposable
{
    private readonly SqliteDatabase _database = new();

    public void Dispose() => _database.Dispose();

    private static async Task<string?> EffectiveAsync(BasicAppTestServer server, string? tenantId, string key)
        => await SettingsTestHost.InvokeAsync(server, tenantId, async ctx =>
        {
            var value = await ctx.Services.GetRequiredService<IConfigValueReader>().GetAsync(key, ctx);
            Assert.True(value.TryGetValue(out var found), value.Error?.Code);
            return found.TryGetValue(out var text) ? text : null;
        });

    [Fact]
    public async Task Configs_OverrideEncryptAndResolve_AcrossPlatformAndTenant()
    {
        var (server, platform, tenant) = await SettingsTestHost.StartAsync(_database.ConnectionString);
        await using var running = server;

        // 平台参数：值按数据类型校验，键本侧唯一
        var (created, createdBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/configs", """{"key":"demo.greeting","name":"Greeting","group":"demo","value":"hello","defaultValue":"hi there"}""", platform));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.True(createdBody.GetProperty("isPlatform").GetBoolean());
        Assert.True(createdBody.GetProperty("allowTenantOverride").GetBoolean());
        var greetingId = createdBody.GetProperty("id").GetString()!;
        var (badNumber, badNumberBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/configs", """{"key":"demo.limit","name":"Limit","dataType":"Number","value":"many"}""", platform));
        Assert.Equal(HttpStatusCode.BadRequest, badNumber.StatusCode);
        Assert.Equal("SETTINGS_VALIDATION_002", badNumberBody.GetProperty("code").GetString());
        var (badJson, badJsonBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/configs", """{"key":"demo.policy","name":"Policy","dataType":"Json","value":"[1,2]"}""", platform));
        Assert.Equal(HttpStatusCode.BadRequest, badJson.StatusCode);
        Assert.Equal("SETTINGS_VALIDATION_002", badJsonBody.GetProperty("code").GetString());
        var (duplicate, duplicateBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/configs", """{"key":"demo.greeting","name":"Again"}""", platform));
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        Assert.Equal("SETTINGS_CONFLICT_001", duplicateBody.GetProperty("code").GetString());
        var (badKey, badKeyBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/configs", """{"key":"Not A Key","name":"Bad"}""", platform));
        Assert.Equal(HttpStatusCode.BadRequest, badKey.StatusCode);
        Assert.True(badKeyBody.GetProperty("errors").TryGetProperty("key", out _));
        var (locked, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/configs", """{"key":"demo.locked","name":"Locked","dataType":"Boolean","value":"true","allowTenantOverride":false}""", platform));
        Assert.Equal(HttpStatusCode.Created, locked.StatusCode);

        // 加密：响应不回显，库里是密文，读取器还原
        var (secret, secretBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/configs", """{"key":"demo.secret","name":"Secret","value":"s3cr3t","isEncrypted":true}""", platform));
        Assert.Equal(HttpStatusCode.Created, secret.StatusCode);
        Assert.Equal(JsonValueKind.Null, secretBody.GetProperty("value").ValueKind);
        Assert.True(secretBody.GetProperty("hasValue").GetBoolean());
        var secretId = secretBody.GetProperty("id").GetString()!;
        var stored = await SettingsTestHost.InvokeAsync(server, null, async ctx => (await ctx.Services.GetRequiredService<IRepository<SysConfig, Guid>>().FindAsync(Guid.Parse(secretId), ctx.CancellationToken)).ValueOrThrow().Value);
        Assert.StartsWith("dp:", stored, StringComparison.Ordinal);
        Assert.DoesNotContain("s3cr3t", stored, StringComparison.Ordinal);
        Assert.Equal("s3cr3t", await EffectiveAsync(server, "contoso", "demo.secret"));

        // 加密参数不给新值即保留；关掉加密且不给值即清空，不解密（只有改权限的人不能借此读到明文）
        var (kept, keptBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Put, $"/api/configs/{secretId}", """{"name":"Secret v2","isEncrypted":true}""", platform));
        Assert.Equal(HttpStatusCode.OK, kept.StatusCode);
        Assert.Equal("Secret v2", keptBody.GetProperty("name").GetString());
        Assert.Equal("s3cr3t", await EffectiveAsync(server, "contoso", "demo.secret"));
        var (plain, plainBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Put, $"/api/configs/{secretId}", """{"name":"Secret v2","isEncrypted":false}""", platform));
        Assert.Equal(HttpStatusCode.OK, plain.StatusCode);
        Assert.Equal(JsonValueKind.Null, plainBody.GetProperty("value").ValueKind);
        Assert.False(plainBody.GetProperty("hasValue").GetBoolean());
        Assert.DoesNotContain("s3cr3t", plainBody.ToString(), StringComparison.Ordinal);

        // 租户：读共享（看得见平台参数），写不共享（平台参数改不了、删不了），同键新建即覆盖
        var (tenantList, tenantListBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/configs:list", """{"page":{"index":1,"size":50}}""", tenant));
        Assert.Equal(HttpStatusCode.OK, tenantList.StatusCode);
        Assert.Equal(3, tenantListBody.GetProperty("items").EnumerateArray().Count(static c => c.GetProperty("key").GetString()!.StartsWith("demo.", StringComparison.Ordinal)));
        var (foreign, foreignBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Put, $"/api/configs/{greetingId}", """{"name":"Mine now"}""", tenant));
        Assert.Equal(HttpStatusCode.Forbidden, foreign.StatusCode);
        Assert.Equal("SETTINGS_DENIED_001", foreignBody.GetProperty("code").GetString());
        var (foreignDelete, foreignDeleteBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Delete, $"/api/configs?ids={greetingId}", token: tenant));
        Assert.Equal(HttpStatusCode.OK, foreignDelete.StatusCode);
        Assert.Equal("SETTINGS_DENIED_001", foreignDeleteBody.GetProperty("failed")[0].GetProperty("code").GetString());
        Assert.Equal("hello", await EffectiveAsync(server, "contoso", "demo.greeting"));
        var (overridden, overriddenBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/configs", """{"key":"demo.greeting","name":"Greeting","value":"hola","allowTenantOverride":true}""", tenant));
        Assert.Equal(HttpStatusCode.Created, overridden.StatusCode);
        Assert.False(overriddenBody.GetProperty("isPlatform").GetBoolean());
        Assert.False(overriddenBody.GetProperty("allowTenantOverride").GetBoolean());
        var overrideId = overriddenBody.GetProperty("id").GetString()!;
        Assert.Equal("hola", await EffectiveAsync(server, "contoso", "demo.greeting"));
        var (forbidden, forbiddenBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/configs", """{"key":"demo.locked","name":"Locked","dataType":"Boolean","value":"false"}""", tenant));
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
        Assert.Equal("SETTINGS_DENIED_002", forbiddenBody.GetProperty("code").GetString());

        // 平台看不到租户的覆盖
        var (platformList, platformListBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/configs:list", """{"page":{"index":1,"size":50},"filters":[{"field":"key","operator":"eq","value":"demo.greeting"}]}""", platform));
        Assert.Equal(HttpStatusCode.OK, platformList.StatusCode);
        Assert.Equal(1, platformListBody.GetProperty("total").GetInt64());

        // 停用的覆盖视为不存在：回落平台值；平台值清空后回落缺省值
        var (disabled, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Put, $"/api/configs/{overrideId}", """{"name":"Greeting","value":"hola","status":"Disabled"}""", tenant));
        Assert.Equal(HttpStatusCode.OK, disabled.StatusCode);
        Assert.Equal("hello", await EffectiveAsync(server, "contoso", "demo.greeting"));
        var (cleared, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Put, $"/api/configs/{greetingId}", """{"name":"Greeting","group":"demo","defaultValue":"hi there","allowTenantOverride":true}""", platform));
        Assert.Equal(HttpStatusCode.OK, cleared.StatusCode);
        Assert.Equal("hi there", await EffectiveAsync(server, "contoso", "demo.greeting"));
        Assert.Null(await EffectiveAsync(server, "contoso", "demo.missing"));

        // 删除本侧参数
        var (deleted, deletedBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Delete, $"/api/configs?ids={overrideId}", token: tenant));
        Assert.Equal(HttpStatusCode.OK, deleted.StatusCode);
        Assert.Equal(overrideId, deletedBody.GetProperty("succeeded")[0].GetString());
        var (gone, goneBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, $"/api/configs/{overrideId}", token: tenant));
        Assert.Equal(HttpStatusCode.NotFound, gone.StatusCode);
        Assert.Equal("SETTINGS_NOTFOUND_001", goneBody.GetProperty("code").GetString());
    }

    [Fact]
    public async Task BuiltInConfigs_AreSeededFromTheCatalog_AndKeepTheOperatorsValue()
    {
        var (server, platform, tenant) = await SettingsTestHost.StartAsync(_database.ConnectionString);
        await using var running = server;

        // 种子按登记建成平台的内建参数：值留空，生效值是缺省值；不许租户覆盖
        var seeded = await SettingsTestHost.BuiltInConfigAsync(server, platform, LoginMethods.ConfigKey);
        var id = seeded.GetProperty("id").GetString()!;
        Assert.Equal(LoginMethods.Config.Name, seeded.GetProperty("name").GetString());
        Assert.Equal(JsonValueKind.Null, seeded.GetProperty("value").ValueKind);
        Assert.Equal("Array", seeded.GetProperty("dataType").GetString());
        Assert.Equal("""["password"]""", seeded.GetProperty("defaultValue").GetString());
        Assert.False(seeded.GetProperty("allowTenantOverride").GetBoolean());
        Assert.Equal("""["password"]""", await EffectiveAsync(server, null, LoginMethods.ConfigKey));
        var (overridden, overriddenBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/configs", $$"""{"key":"{{LoginMethods.ConfigKey}}","name":"Mine","value":"email"}""", tenant));
        Assert.Equal(HttpStatusCode.Forbidden, overridden.StatusCode);
        Assert.Equal("SETTINGS_DENIED_002", overriddenBody.GetProperty("code").GetString());

        // 运营改了值，顺手改了名称、清了缺省值、放开了覆盖：再跑种子，值留着，元数据对齐回登记的
        var (edited, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Put, $"/api/configs/{id}", """{"name":"Renamed","dataType":"Array","value":"[\"password\",\"email\"]","allowTenantOverride":true}""", platform));
        Assert.Equal(HttpStatusCode.OK, edited.StatusCode);
        await server.RunSeedersAsync();
        var realigned = await SettingsTestHost.BuiltInConfigAsync(server, platform, LoginMethods.ConfigKey);
        Assert.Equal(id, realigned.GetProperty("id").GetString());
        Assert.Equal("""["password","email"]""", realigned.GetProperty("value").GetString());
        Assert.Equal(LoginMethods.Config.Name, realigned.GetProperty("name").GetString());
        Assert.Equal("""["password"]""", realigned.GetProperty("defaultValue").GetString());
        Assert.False(realigned.GetProperty("allowTenantOverride").GetBoolean());

        // 已对齐的不再写
        await server.RunSeedersAsync();
        Assert.Equal(realigned.GetProperty("modifiedAt").GetString(), (await SettingsTestHost.BuiltInConfigAsync(server, platform, LoginMethods.ConfigKey)).GetProperty("modifiedAt").GetString());

        // 内建参数删不掉
        var (deleted, deletedBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Delete, $"/api/configs?ids={id}", token: platform));
        Assert.Equal(HttpStatusCode.OK, deleted.StatusCode);
        Assert.Equal("SETTINGS_CONFLICT_006", deletedBody.GetProperty("failed")[0].GetProperty("code").GetString());
    }

    [Fact]
    public async Task BuiltInConfigs_CoverEveryParameterTheCodeReads_AndTheirDefaultsReadLikeNoParameter()
    {
        var (server, platform, _) = await SettingsTestHost.StartAsync(_database.ConnectionString);
        await using var running = server;

        // 每个代码读取的参数都登记了，种子建成内建参数、值留空
        var registered = server.Services.GetServices<ConfigCatalogSection>().SelectMany(static s => s.Configs).ToList();
        Assert.Equal(
            ["access.security.login-auto-ban", "auditing.retention-days", "chat.policy", "identity.impersonation", "identity.login-methods", "identity.password", "notification.telegram", "notification.telegram.webhook-secret-token"],
            registered.Select(static c => c.Key).Order(StringComparer.Ordinal));
        foreach (var descriptor in registered)
        {
            var config = await SettingsTestHost.BuiltInConfigAsync(server, platform, descriptor.Key);
            Assert.False(config.GetProperty("hasValue").GetBoolean(), descriptor.Key);
            Assert.Equal(descriptor.IsEncrypted, config.GetProperty("isEncrypted").GetBoolean());
            Assert.Equal(descriptor.AllowTenantOverride, config.GetProperty("allowTenantOverride").GetBoolean());
        }

        // 缺省值读出来与没建参数时一样
        var read = await SettingsTestHost.InvokeAsync(server, null, async ctx => (
            Methods: (await LoginMethods.ReadAsync(ctx)).ValueOrThrow(),
            Password: (await PasswordSettings.ReadAsync(ctx)).ValueOrThrow(),
            Impersonation: (await ImpersonationSettings.ReadAsync(ctx)).ValueOrThrow(),
            Ban: (await LoginDefense.SettingsAsync(ctx)).ValueOrThrow(),
            Telegram: (await TelegramSettings.LoadAsync(ctx)).ValueOrThrow(),
            Chat: (await ctx.Services.GetRequiredService<ChatPolicy>().ReadAsync(ctx)).ValueOrThrow(),
            Retention: (await ctx.Services.GetRequiredService<IConfigValueReader>().GetAsync(AuditLogOptions.RetentionDaysConfigKey, ctx)).ValueOrThrow()));
        Assert.Equal([LoginMethods.Password], read.Methods);
        Assert.Equal(PasswordSettings.Default, read.Password);
        Assert.Equal(ImpersonationSettings.Default, read.Impersonation);
        Assert.Equal(LoginAutoBanSettings.Default, read.Ban);
        Assert.Equal(TelegramSettings.Default with { Network = new TelegramNetwork() }, read.Telegram);
        Assert.Equal((ChatPolicy.DefaultRetentionDays, 0), (read.Chat.RetentionDays, read.Chat.SensitiveWords.Count));
        Assert.False(read.Retention.HasValue);
    }

    [Fact]
    public async Task ConfigCatalogSeeder_AdoptsAHandMadeParameter_OnlyWhenItsValueFitsTheType()
    {
        var limit = new ConfigDescriptor("demo.limit", "Limit", "demo", ConfigDataType.Number, "10", "Upper bound");
        var secret = new ConfigDescriptor("demo.secret", "Secret", "demo", ConfigDataType.Text, null, "A secret", IsEncrypted: true);
        await using var server = await BasicAppTestServer.StartAsync(_database.ConnectionString, configure: builder => builder.Services.AddConfigCatalog("demo", [limit, secret]));
        var runner = server.Services.GetRequiredService<SeedRunner>();
        var others = (await runner.DescribeAsync(CancellationToken.None)).Select(static s => s.Name).Where(static n => n != "settings.configs").ToList();
        Assert.True((await runner.RunAsync(others, CancellationToken.None)).IsSuccess);
        var platform = (await AuthFlowTests.LoginAsync(server, "superadmin", "SuperAdmin@123")).GetProperty("accessToken").GetString()!;

        // 种子之前手建了同键的参数：数值参数的值不是数，密钥是明文
        var (created, createdBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/configs", """{"key":"demo.limit","name":"Mine","value":"many"}""", platform));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var id = createdBody.GetProperty("id").GetString()!;
        var (plain, plainBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/configs", """{"key":"demo.secret","name":"Mine","value":"s3cr3t"}""", platform));
        Assert.Equal(HttpStatusCode.Created, plain.StatusCode);
        var secretId = plainBody.GetProperty("id").GetString()!;

        // 值不符合类型：种子报错（消息带参数键），不改它
        var refused = await runner.RunAsync(["settings.configs"], CancellationToken.None);
        Assert.Equal("SETTINGS_VALIDATION_002", refused.Error?.Code);
        Assert.Equal("demo.limit", refused.Error!.Details?["field"]);
        var (kept, keptBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, $"/api/configs/{id}", token: platform));
        Assert.Equal(HttpStatusCode.OK, kept.StatusCode);
        Assert.False(keptBody.GetProperty("isBuiltIn").GetBoolean());

        // 值改成数之后收为内建：类型与元数据按登记对齐，值留着
        var (fixedValue, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Put, $"/api/configs/{id}", """{"name":"Mine","value":"20"}""", platform));
        Assert.Equal(HttpStatusCode.OK, fixedValue.StatusCode);
        Assert.True((await runner.RunAsync(["settings.configs"], CancellationToken.None)).IsSuccess);
        var adopted = await SettingsTestHost.BuiltInConfigAsync(server, platform, "demo.limit");
        Assert.Equal(id, adopted.GetProperty("id").GetString());
        Assert.Equal("Number", adopted.GetProperty("dataType").GetString());
        Assert.Equal("Limit", adopted.GetProperty("name").GetString());
        Assert.Equal("20", adopted.GetProperty("value").GetString());
        Assert.Equal("10", adopted.GetProperty("defaultValue").GetString());

        // 登记为加密的：收为内建时就地加密现值，读取照旧
        var encrypted = await SettingsTestHost.BuiltInConfigAsync(server, platform, "demo.secret");
        Assert.Equal(secretId, encrypted.GetProperty("id").GetString());
        Assert.True(encrypted.GetProperty("isEncrypted").GetBoolean());
        Assert.Equal(JsonValueKind.Null, encrypted.GetProperty("value").ValueKind);
        var stored = await SettingsTestHost.InvokeAsync(server, null, async ctx => (await ctx.Services.GetRequiredService<IRepository<SysConfig, Guid>>().FindAsync(Guid.Parse(secretId), ctx.CancellationToken)).ValueOrThrow().Value);
        Assert.StartsWith("dp:", stored, StringComparison.Ordinal);
        Assert.Equal("s3cr3t", await EffectiveAsync(server, null, "demo.secret"));
    }
}
