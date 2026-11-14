// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Net;
using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.Contracts;
using XiHan.BasicApp.Contracts.Settings;
using XiHan.BasicApp.Integration.Tests.Infrastructure;
using XiHan.BasicApp.Notification.Domain.Entities;
using XiHan.BasicApp.Notification.Domain.Services;
using XiHan.Framework.Caching.Abstractions.Caches;
using XiHan.Framework.Caching.Abstractions.Stores;

namespace XiHan.BasicApp.Integration.Tests;

/// <summary>
/// 验证参数、字典选项与消息模板的读穿缓存
/// </summary>
/// <remarks>
/// 三者都按侧各存一份、租户读时先看自己的再看平台的；每一步写完立刻读（不拨钟），读到的必须是新值：
/// 平台的改动租户马上看到，没有时缓存的「没有」在新建后马上失效；加密参数在缓存里只有密文
/// </remarks>
public sealed class ReadCachesTests : IDisposable
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

    private static async Task<string?> TemplateContentAsync(BasicAppTestServer server, string? tenantId, string code)
        => await SettingsTestHost.InvokeAsync(server, tenantId, async ctx =>
            (await ctx.Services.GetRequiredService<MessageTemplateService>().ResolveAsync(tenantId ?? TenantIds.Platform, TemplateChannel.Email, code, ctx.CancellationToken))?.Content);

    private static async Task<List<string>> OptionsAsync(BasicAppTestServer server, string token, string code)
    {
        var (response, body) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, $"/api/dicts:options?code={code}", token: token));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return body.GetProperty("items").EnumerateArray().Select(static o => o.GetProperty("label").GetString()!).ToList();
    }

    [Fact]
    public async Task Configs_EncryptedValueStaysCiphertextInCache_AndPlatformWritesReachTenantsAtOnce()
    {
        var (server, platform, _) = await SettingsTestHost.StartAsync(_database.ConnectionString);
        await using var running = server;

        var (created, createdBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/configs", """{"key":"cache.token","name":"Token","value":"t0ken-1","isEncrypted":true}""", platform));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var id = createdBody.GetProperty("id").GetString()!;

        // 租户先读一次：本侧的「没有」与平台那一份都进了缓存
        Assert.Equal("t0ken-1", await EffectiveAsync(server, "contoso", "cache.token"));
        var cached = (await server.Services.GetRequiredService<ISharedCache>().GetAsync<string>(new CacheKey($"settings:configs:{TenantIds.Platform}:cache.token"), CancellationToken.None)).ValueOrThrow();
        Assert.DoesNotContain("t0ken-1", cached, StringComparison.Ordinal);

        // 平台改值、删除，租户马上读到
        var (updated, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Put, $"/api/configs/{id}", """{"name":"Token","value":"t0ken-2","isEncrypted":true}""", platform));
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        Assert.Equal("t0ken-2", await EffectiveAsync(server, "contoso", "cache.token"));
        var (deleted, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Delete, $"/api/configs?ids={id}", token: platform));
        Assert.Equal(HttpStatusCode.OK, deleted.StatusCode);
        Assert.Null(await EffectiveAsync(server, "contoso", "cache.token"));
        Assert.Null(await EffectiveAsync(server, null, "cache.token"));
    }

    [Fact]
    public async Task DictOptions_PlatformWritesReachTenantsAtOnce_AndAMissingCodeAppearsWhenCreated()
    {
        var (server, platform, tenant) = await SettingsTestHost.StartAsync(_database.ConnectionString);
        await using var running = server;

        // 编码还没有：404，「没有」进了缓存；平台一建，租户马上读到
        var (missing, missingBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/api/dicts:options?code=priority", token: tenant));
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        Assert.Equal("SETTINGS_NOTFOUND_002", missingBody.GetProperty("code").GetString());
        var (created, createdBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/dicts", """{"code":"priority","name":"Priority"}""", platform));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var dictId = createdBody.GetProperty("id").GetString()!;
        Assert.Empty(await OptionsAsync(server, tenant, "priority"));

        // 加项、改项、删项、停用字典，租户每次马上读到
        var (high, highBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/dict-items", $$"""{"dictId":"{{dictId}}","code":"high","name":"High","order":1}""", platform));
        Assert.Equal(HttpStatusCode.Created, high.StatusCode);
        var highId = highBody.GetProperty("id").GetString()!;
        var (low, lowBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/dict-items", $$"""{"dictId":"{{dictId}}","code":"low","name":"Low","order":2}""", platform));
        Assert.Equal(HttpStatusCode.Created, low.StatusCode);
        Assert.Equal(["High", "Low"], await OptionsAsync(server, tenant, "priority"));
        var (renamed, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Put, $"/api/dict-items/{highId}", """{"name":"Urgent","order":1}""", platform));
        Assert.Equal(HttpStatusCode.OK, renamed.StatusCode);
        Assert.Equal(["Urgent", "Low"], await OptionsAsync(server, tenant, "priority"));
        var (removed, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Delete, $"/api/dict-items?ids={lowBody.GetProperty("id").GetString()}", token: platform));
        Assert.Equal(HttpStatusCode.OK, removed.StatusCode);
        Assert.Equal(["Urgent"], await OptionsAsync(server, tenant, "priority"));
        var (disabled, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Put, $"/api/dicts/{dictId}", """{"name":"Priority","status":"Disabled"}""", platform));
        Assert.Equal(HttpStatusCode.OK, disabled.StatusCode);
        Assert.Empty(await OptionsAsync(server, tenant, "priority"));
    }

    [Fact]
    public async Task MessageTemplates_TenantOverrideAndPlatformWrites_TakeEffectAtOnceWhenResolving()
    {
        var (server, platform, tenant) = await SettingsTestHost.StartAsync(_database.ConnectionString);
        await using var running = server;

        var (created, createdBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/message-templates", """{"code":"cache-note","channel":"Email","name":"Note","content":"platform v1"}""", platform));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var platformId = createdBody.GetProperty("id").GetString()!;
        Assert.Equal("platform v1", await TemplateContentAsync(server, "contoso", "cache-note"));

        // 租户建同编码模板即覆盖；停用即回落平台的
        var (own, ownBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/message-templates", """{"code":"cache-note","channel":"Email","name":"Note","content":"tenant v1"}""", tenant));
        Assert.Equal(HttpStatusCode.Created, own.StatusCode);
        Assert.Equal("tenant v1", await TemplateContentAsync(server, "contoso", "cache-note"));
        var (off, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Put, $"/api/message-templates/{ownBody.GetProperty("id").GetString()}", """{"name":"Note","content":"tenant v1","isEnabled":false}""", tenant));
        Assert.Equal(HttpStatusCode.OK, off.StatusCode);
        Assert.Equal("platform v1", await TemplateContentAsync(server, "contoso", "cache-note"));

        // 平台改正文、删除，租户马上读到
        var (updated, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Put, $"/api/message-templates/{platformId}", """{"name":"Note","content":"platform v2"}""", platform));
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        Assert.Equal("platform v2", await TemplateContentAsync(server, "contoso", "cache-note"));
        var (deleted, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Delete, $"/api/message-templates?ids={platformId}", token: platform));
        Assert.Equal(HttpStatusCode.OK, deleted.StatusCode);
        Assert.Null(await TemplateContentAsync(server, "contoso", "cache-note"));
    }
}
