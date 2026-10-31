// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Globalization;
using System.Net;
using System.Text.Json;
using XiHan.BasicApp.Integration.Tests.Infrastructure;

namespace XiHan.BasicApp.Integration.Tests;

/// <summary>
/// 验证用户的启停与高级搜索。
/// </summary>
/// <remarks>
/// 停用即吊销会话、登录给 <c>IDENTITY_DENIED_018</c>，启用回到正常（锁定中的要先解锁）；
/// 列表按手机号、状态多选、创建时间、角色（启用的角色、生效的指派）与部门（含下级）筛选。
/// </remarks>
public sealed class UsersStatusSearchTests : IDisposable
{
    private readonly SqliteDatabase _database = new();

    public void Dispose() => _database.Dispose();

    private static async Task<(HttpStatusCode Status, JsonElement Body)> SendAsync(BasicAppTestServer server, HttpMethod method, string path, string? body, string token)
    {
        var (response, json) = await server.SendAsync(BasicAppTestServer.Request(method, path, body, token));
        return (response.StatusCode, json);
    }

    private static async Task<string> CreateUserAsync(BasicAppTestServer server, string token, string userName, string? phone = null)
    {
        var body = phone is null
            ? $$"""{"userName":"{{userName}}","displayName":"{{userName}}","password":"Pass@123456"}"""
            : $$"""{"userName":"{{userName}}","displayName":"{{userName}}","password":"Pass@123456","phoneNumber":"{{phone}}"}""";
        var (status, created) = await SendAsync(server, HttpMethod.Post, "/api/users", body, token);
        Assert.Equal(HttpStatusCode.Created, status);
        return created.GetProperty("id").GetString()!;
    }

    private static async Task<List<string>> ListAsync(BasicAppTestServer server, string token, string filters)
    {
        var (status, body) = await SendAsync(server, HttpMethod.Post, "/api/users:list", $$"""{"page":{"index":1,"size":100},"filters":{{filters}}}""", token);
        Assert.Equal(HttpStatusCode.OK, status);
        return body.GetProperty("items").EnumerateArray().Select(static u => u.GetProperty("userName").GetString()!).Order(StringComparer.Ordinal).ToList();
    }

    [Fact]
    public async Task Disable_RevokesSessionsAndBlocksSignIn_Enable_RestoresIt()
    {
        var (server, _, tenant) = await SettingsTestHost.StartAsync(_database.ConnectionString);
        await using var running = server;
        var carlId = await CreateUserAsync(server, tenant, "carl");
        var carl = (await AuthFlowTests.LoginAsync(server, "carl", "Pass@123456", "contoso")).GetProperty("accessToken").GetString()!;

        var (disabled, disabledBody) = await SendAsync(server, HttpMethod.Post, "/api/users:disable", $$"""{"ids":["{{carlId}}"]}""", tenant);
        Assert.Equal(HttpStatusCode.OK, disabled);
        Assert.Equal([carlId], disabledBody.GetProperty("succeeded").EnumerateArray().Select(static e => e.GetString()));

        // 停用即吊销会话，再登录给明确的错误码
        Assert.Equal(HttpStatusCode.Forbidden, (await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/api/auth:me", token: carl))).Response.StatusCode);
        var (login, loginBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/auth:login", """{"userName":"carl","password":"Pass@123456","tenant":"contoso"}"""));
        Assert.Equal((HttpStatusCode.Forbidden, "IDENTITY_DENIED_018"), (login.StatusCode, loginBody.GetProperty("code").GetString()));

        // 启用两次：第二次本来就正常，照样算成功
        for (var round = 0; round < 2; round++)
        {
            var (enabled, enabledBody) = await SendAsync(server, HttpMethod.Post, "/api/users:enable", $$"""{"ids":["{{carlId}}"]}""", tenant);
            Assert.Equal(HttpStatusCode.OK, enabled);
            Assert.Equal([carlId], enabledBody.GetProperty("succeeded").EnumerateArray().Select(static e => e.GetString()));
        }

        await AuthFlowTests.LoginAsync(server, "carl", "Pass@123456", "contoso");

        // 锁定中的用户启用不适用，要先解锁
        await SendAsync(server, HttpMethod.Post, "/api/users:lock", $$"""{"ids":["{{carlId}}"]}""", tenant);
        var (_, lockedBody) = await SendAsync(server, HttpMethod.Post, "/api/users:enable", $$"""{"ids":["{{carlId}}"]}""", tenant);
        Assert.Equal("IDENTITY_CONFLICT_012", lockedBody.GetProperty("failed")[0].GetProperty("code").GetString());

        // 不能停用自己
        var (_, me) = await SendAsync(server, HttpMethod.Get, "/api/auth:me", null, tenant);
        var (_, selfBody) = await SendAsync(server, HttpMethod.Post, "/api/users:disable", $$"""{"ids":["{{me.GetProperty("user").GetProperty("id").GetString()}}"]}""", tenant);
        Assert.Equal("IDENTITY_CONFLICT_002", selfBody.GetProperty("failed")[0].GetProperty("code").GetString());
    }

    [Fact]
    public async Task List_FiltersByPhoneStatusCreatedAtRoleAndDepartment()
    {
        var (server, _, tenant) = await SettingsTestHost.StartAsync(_database.ConnectionString);
        await using var running = server;
        var annId = await CreateUserAsync(server, tenant, "ann", "13800000001");
        var benId = await CreateUserAsync(server, tenant, "ben", "13900000002");
        await SendAsync(server, HttpMethod.Post, "/api/users:disable", $$"""{"ids":["{{benId}}"]}""", tenant);

        var (_, role) = await SendAsync(server, HttpMethod.Post, "/api/roles", """{"code":"viewer","name":"Viewer"}""", tenant);
        var roleId = role.GetProperty("id").GetString()!;
        await SendAsync(server, HttpMethod.Post, $"/api/roles/{roleId}:assign", $$"""{"userIds":["{{annId}}"]}""", tenant);
        var (_, hq) = await SendAsync(server, HttpMethod.Post, "/api/departments", """{"code":"hq","name":"Headquarters"}""", tenant);
        var hqId = hq.GetProperty("id").GetString()!;
        var (_, rd) = await SendAsync(server, HttpMethod.Post, "/api/departments", $$"""{"code":"rd","name":"R&D","parentId":"{{hqId}}"}""", tenant);
        await SendAsync(server, HttpMethod.Post, "/api/memberships", $$"""{"userId":"{{benId}}","departmentId":"{{rd.GetProperty("id").GetString()}}"}""", tenant);

        Assert.Equal(["ann"], await ListAsync(server, tenant, """[{"field":"phoneNumber","operator":"contains","value":"138"}]"""));
        Assert.Equal(["ben"], await ListAsync(server, tenant, """[{"field":"status","operator":"in","value":"Disabled,Locked"}]"""));
        Assert.Equal(["ann"], await ListAsync(server, tenant, $$"""[{"field":"roleId","operator":"eq","value":"{{roleId}}"}]"""));
        Assert.Equal(["ben"], await ListAsync(server, tenant, $$"""[{"field":"departmentId","operator":"in","value":"{{hqId}}"}]"""));
        var hourAgo = DateTimeOffset.UtcNow.AddHours(-1).ToString("O", CultureInfo.InvariantCulture);
        Assert.Contains("ann", await ListAsync(server, tenant, $$"""[{"field":"createdAt","operator":"ge","value":"{{hourAgo}}"}]"""));
        Assert.Empty(await ListAsync(server, tenant, """[{"field":"createdAt","operator":"lt","value":"2000-01-01T00:00:00Z"}]"""));

        // 角色或部门写得不对：校验错误
        var (invalid, invalidBody) = await SendAsync(server, HttpMethod.Post, "/api/users:list", """{"page":{"index":1,"size":10},"filters":[{"field":"roleId","operator":"eq","value":"nope"}]}""", tenant);
        Assert.Equal(HttpStatusCode.BadRequest, invalid);
        Assert.StartsWith("IDENTITY_VALIDATION_", invalidBody.GetProperty("code").GetString(), StringComparison.Ordinal);
    }
}
