// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Net;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.Contracts.Settings;
using XiHan.BasicApp.Integration.Tests.Infrastructure;
using XiHan.BasicApp.Settings.Domain.Entities;
using XiHan.Framework.Persistence.Abstractions.Repositories;

namespace XiHan.BasicApp.Integration.Tests;

/// <summary>
/// 系统参数：平台参数与租户同键覆盖（不允许覆盖时被拒）、值按数据类型校验、加密值不回显也不明文落库、更新时加密的保留与开关、
/// 读共享写不共享（另一侧的参数改不了也删不了）、生效值读取器（租户优先、停用即回落平台、空值回落缺省值）。
/// </summary>
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

        // 加密参数不给新值即保留；关掉加密且不给值即解密成明文
        var (kept, keptBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Put, $"/api/configs/{secretId}", """{"name":"Secret v2","isEncrypted":true}""", platform));
        Assert.Equal(HttpStatusCode.OK, kept.StatusCode);
        Assert.Equal("Secret v2", keptBody.GetProperty("name").GetString());
        Assert.Equal("s3cr3t", await EffectiveAsync(server, "contoso", "demo.secret"));
        var (plain, plainBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Put, $"/api/configs/{secretId}", """{"name":"Secret v2","isEncrypted":false}""", platform));
        Assert.Equal(HttpStatusCode.OK, plain.StatusCode);
        Assert.Equal("s3cr3t", plainBody.GetProperty("value").GetString());

        // 租户：读共享（看得见平台参数），写不共享（平台参数改不了、删不了），同键新建即覆盖
        var (tenantList, tenantListBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/configs:list", """{"page":{"index":1,"size":50}}""", tenant));
        Assert.Equal(HttpStatusCode.OK, tenantList.StatusCode);
        Assert.Equal(3, tenantListBody.GetProperty("total").GetInt64());
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
}
