// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Net;
using System.Text.Json;
using XiHan.BasicApp.Integration.Tests.Infrastructure;

namespace XiHan.BasicApp.Integration.Tests;

/// <summary>
/// 验证字典与字典项的约束、选项与删除守护。
/// </summary>
/// <remarks>
/// 编码平台与租户之间也不重、树形项（同字典、无环）、缺省项唯一、元数据必须是 JSON、选项深度优先展平且停用项标出、
/// 停用字典没有选项也不能加项、删除的守护（有项的字典、有下级的项、另一侧的字典）、选项只要求已登录。
/// </remarks>
public sealed class SettingsDictsTests : IDisposable
{
    private readonly SqliteDatabase _database = new();

    public void Dispose() => _database.Dispose();

    private static async Task<string> CreateItemAsync(BasicAppTestServer server, string token, string dictId, string code, string name, string? parentId = null, bool isDefault = false, int order = 0)
    {
        var parent = parentId is null ? "null" : $"\"{parentId}\"";
        var (response, body) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/dict-items", $$"""{"dictId":"{{dictId}}","code":"{{code}}","name":"{{name}}","parentId":{{parent}},"isDefault":{{(isDefault ? "true" : "false")}},"order":{{order}}}""", token));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return body.GetProperty("id").GetString()!;
    }

    [Fact]
    public async Task Dicts_TreeItems_Options_AndGuards()
    {
        var (server, platform, tenant) = await SettingsTestHost.StartAsync(_database.ConnectionString);
        await using var running = server;

        // 平台字典；租户不能再用同一个编码（字典不覆盖）
        var (created, createdBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/dicts", """{"code":"customer_level","name":"Customer level","category":"crm"}""", platform));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var levelId = createdBody.GetProperty("id").GetString()!;
        var (taken, takenBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/dicts", """{"code":"customer_level","name":"Mine"}""", tenant));
        Assert.Equal(HttpStatusCode.Conflict, taken.StatusCode);
        Assert.Equal("SETTINGS_CONFLICT_002", takenBody.GetProperty("code").GetString());
        await CreateItemAsync(server, platform, levelId, "vip", "VIP", order: 1);
        await CreateItemAsync(server, platform, levelId, "normal", "Normal", isDefault: true, order: 2);

        // 租户字典：树形项，深度优先、同级按排序号
        var (region, regionBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/dicts", """{"code":"region","name":"Region"}""", tenant));
        Assert.Equal(HttpStatusCode.Created, region.StatusCode);
        var regionId = regionBody.GetProperty("id").GetString()!;
        var east = await CreateItemAsync(server, tenant, regionId, "east", "East", isDefault: true, order: 1);
        var shanghai = await CreateItemAsync(server, tenant, regionId, "shanghai", "Shanghai", parentId: east, order: 2);
        await CreateItemAsync(server, tenant, regionId, "hangzhou", "Hangzhou", parentId: east, order: 1);
        var north = await CreateItemAsync(server, tenant, regionId, "north", "North", order: 2);
        var (platformTaken, platformTakenBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/dicts", """{"code":"region","name":"Platform region"}""", platform));
        Assert.Equal(HttpStatusCode.Conflict, platformTaken.StatusCode);
        Assert.Equal("SETTINGS_CONFLICT_002", platformTakenBody.GetProperty("code").GetString());

        var (items, itemsBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, $"/api/dict-items?dictId={regionId}", token: tenant));
        Assert.Equal(HttpStatusCode.OK, items.StatusCode);
        var rows = itemsBody.GetProperty("items").EnumerateArray().ToList();
        Assert.Equal(["east", "hangzhou", "shanghai", "north"], rows.Select(static r => r.GetProperty("code").GetString()).ToList());
        Assert.Equal([0, 1, 1, 0], rows.Select(static r => r.GetProperty("depth").GetInt32()).ToList());

        // 上级不合法：不同字典、自己、自己的下级
        var (cycle, cycleBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Put, $"/api/dict-items/{east}", $$"""{"name":"East","parentId":"{{shanghai}}","isDefault":true,"order":1}""", tenant));
        Assert.Equal(HttpStatusCode.BadRequest, cycle.StatusCode);
        Assert.Equal("SETTINGS_VALIDATION_004", cycleBody.GetProperty("code").GetString());
        var (metadata, metadataBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/dict-items", $$"""{"dictId":"{{regionId}}","code":"west","name":"West","metadata":"not json"}""", tenant));
        Assert.Equal(HttpStatusCode.BadRequest, metadata.StatusCode);
        Assert.Equal("SETTINGS_VALIDATION_003", metadataBody.GetProperty("code").GetString());
        var (duplicateItem, duplicateItemBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/dict-items", $$"""{"dictId":"{{regionId}}","code":"north","name":"North again"}""", tenant));
        Assert.Equal(HttpStatusCode.Conflict, duplicateItem.StatusCode);
        Assert.Equal("SETTINGS_CONFLICT_003", duplicateItemBody.GetProperty("code").GetString());

        // 缺省项唯一：设北为缺省，东即取消；停用项照常给出并标出
        var (switched, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Put, $"/api/dict-items/{north}", """{"name":"North","isDefault":true,"order":2,"status":"Disabled"}""", tenant));
        Assert.Equal(HttpStatusCode.OK, switched.StatusCode);
        var viewer = TestTokens.Issue("bob", "contoso", "guest");
        var (options, optionsBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/api/dicts:options?code=region", token: tenant));
        Assert.Equal(HttpStatusCode.OK, options.StatusCode);
        var list = optionsBody.GetProperty("items").EnumerateArray().ToList();
        Assert.Equal(["east", "hangzhou", "shanghai", "north"], list.Select(static o => o.GetProperty("value").GetString()).ToList());
        Assert.Equal("east", list[2].GetProperty("parentValue").GetString());
        Assert.Equal(JsonValueKind.Null, list[0].GetProperty("parentValue").ValueKind);
        Assert.Equal(["north"], list.Where(static o => o.GetProperty("isDefault").GetBoolean()).Select(static o => o.GetProperty("value").GetString()).ToList());
        Assert.Equal(["north"], list.Where(static o => o.GetProperty("disabled").GetBoolean()).Select(static o => o.GetProperty("value").GetString()).ToList());

        // 选项只要求已登录：没有任何授予的角色也能取平台字典的选项；未知编码 404
        var (shared, sharedBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/api/dicts:options?code=customer_level", token: viewer));
        Assert.Equal(HttpStatusCode.OK, shared.StatusCode);
        Assert.Equal(["vip", "normal"], sharedBody.GetProperty("items").EnumerateArray().Select(static o => o.GetProperty("value").GetString()).ToList());
        var (unknown, unknownBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/api/dicts:options?code=nothing", token: tenant));
        Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
        Assert.Equal("SETTINGS_NOTFOUND_002", unknownBody.GetProperty("code").GetString());

        // 删除的守护：有下级的项、有项的字典、另一侧的字典
        var (parentDelete, parentDeleteBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Delete, $"/api/dict-items?ids={east}", token: tenant));
        Assert.Equal(HttpStatusCode.OK, parentDelete.StatusCode);
        Assert.Equal("SETTINGS_CONFLICT_005", parentDeleteBody.GetProperty("failed")[0].GetProperty("code").GetString());
        var (dictDelete, dictDeleteBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Delete, $"/api/dicts?ids={regionId}", token: tenant));
        Assert.Equal("SETTINGS_CONFLICT_004", dictDeleteBody.GetProperty("failed")[0].GetProperty("code").GetString());
        Assert.Equal(HttpStatusCode.OK, dictDelete.StatusCode);
        var (foreignDelete, foreignDeleteBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Delete, $"/api/dicts?ids={levelId}", token: tenant));
        Assert.Equal(HttpStatusCode.OK, foreignDelete.StatusCode);
        Assert.Equal("SETTINGS_DENIED_001", foreignDeleteBody.GetProperty("failed")[0].GetProperty("code").GetString());

        // 停用的字典：没有选项，也不能再加项
        var (disabled, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Put, $"/api/dicts/{regionId}", """{"name":"Region","status":"Disabled"}""", tenant));
        Assert.Equal(HttpStatusCode.OK, disabled.StatusCode);
        var (empty, emptyBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/api/dicts:options?code=region", token: tenant));
        Assert.Equal(HttpStatusCode.OK, empty.StatusCode);
        Assert.Equal(0, emptyBody.GetProperty("items").GetArrayLength());
        var (closed, closedBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/dict-items", $$"""{"dictId":"{{regionId}}","code":"west","name":"West"}""", tenant));
        Assert.Equal(HttpStatusCode.Conflict, closed.StatusCode);
        Assert.Equal("SETTINGS_CONFLICT_007", closedBody.GetProperty("code").GetString());

        // 平台看不到租户字典
        var (platformList, platformListBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/dicts:list", """{"page":{"index":1,"size":50}}""", platform));
        Assert.Equal(HttpStatusCode.OK, platformList.StatusCode);
        Assert.Equal(["customer_level"], platformListBody.GetProperty("items").EnumerateArray().Select(static d => d.GetProperty("code").GetString()).ToList());
    }
}
