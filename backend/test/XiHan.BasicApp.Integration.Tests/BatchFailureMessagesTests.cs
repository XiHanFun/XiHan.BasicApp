// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Net;
using System.Text.Json;
using XiHan.BasicApp.Integration.Tests.Infrastructure;

namespace XiHan.BasicApp.Integration.Tests;

/// <summary>
/// 验证批量结果的失败项带文案
/// </summary>
/// <remarks>
/// 前端直接显示 <c>message</c>：要按调用方的区域本地化，占位符要替换掉
/// </remarks>
public sealed class BatchFailureMessagesTests : IDisposable
{
    private readonly SqliteDatabase _database = new();

    public void Dispose() => _database.Dispose();

    private static async Task<JsonElement> FailureAsync(BasicAppTestServer server, HttpMethod method, string path, string? body, string token, string language)
    {
        var (response, json) = await server.SendAsync(BasicAppTestServer.Request(method, path, body, token, language));
        Assert.True(response.StatusCode == HttpStatusCode.OK, json.ToString());
        return Assert.Single(json.GetProperty("failed").EnumerateArray());
    }

    private static async Task<string> BuiltInRoleIdAsync(BasicAppTestServer server, string token)
    {
        var (_, roles) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/roles:list", "{}", token));
        return roles.GetProperty("items").EnumerateArray().First(static r => r.GetProperty("isBuiltIn").GetBoolean()).GetProperty("id").GetString()!;
    }

    [Fact]
    public async Task Message_FollowsTheCallersLocale_AndFillsItsPlaceholders()
    {
        var (server, _, contoso) = await SettingsTestHost.StartAsync(_database.ConnectionString);
        await using var running = server;
        var roleId = await BuiltInRoleIdAsync(server, contoso);

        var zh = await FailureAsync(server, HttpMethod.Delete, $"/api/roles?ids={roleId}", null, contoso, "zh-CN");
        Assert.Equal("ACCESS_CONFLICT_002", zh.GetProperty("code").GetString());
        Assert.Equal("角色 'admin' 是内建角色，不能删除。", zh.GetProperty("message").GetString());

        var en = await FailureAsync(server, HttpMethod.Delete, $"/api/roles?ids={roleId}", null, contoso, "en-US");
        Assert.Equal("ACCESS_CONFLICT_002", en.GetProperty("code").GetString());
        Assert.DoesNotContain("内建", en.GetProperty("message").GetString()!, StringComparison.Ordinal);
        Assert.Contains("admin", en.GetProperty("message").GetString()!, StringComparison.Ordinal);
    }

    [Fact]
    public async Task NotFound_NamesTheItem()
    {
        var (server, _, contoso) = await SettingsTestHost.StartAsync(_database.ConnectionString);
        await using var running = server;
        var missing = Guid.NewGuid();
        var failure = await FailureAsync(server, HttpMethod.Delete, $"/api/roles?ids={missing}", null, contoso, "zh-CN");
        Assert.Equal($"角色 '{missing}' 不存在。", failure.GetProperty("message").GetString());
    }

    [Fact]
    public async Task ARowAtHand_IsNamedRatherThanShownAsAGuid()
    {
        var (server, _, contoso) = await SettingsTestHost.StartAsync(_database.ConnectionString);
        await using var running = server;
        var (created, createdBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/departments", """{"code":"hq","name":"总部"}""", contoso));
        Assert.True(created.StatusCode == HttpStatusCode.Created, createdBody.ToString());
        var parentId = createdBody.GetProperty("id").GetString()!;
        var (child, childBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/departments", $$"""{"code":"rd","name":"研发部","parentId":"{{parentId}}"}""", contoso));
        Assert.True(child.StatusCode == HttpStatusCode.Created, childBody.ToString());

        var failure = await FailureAsync(server, HttpMethod.Delete, $"/api/departments?ids={parentId}", null, contoso, "zh-CN");
        Assert.Equal("部门 '总部' 还有子部门或成员。", failure.GetProperty("message").GetString());
    }

    [Fact]
    public async Task PermissionBatch_CarriesTheReason()
    {
        var (server, _, contoso) = await SettingsTestHost.StartAsync(_database.ConnectionString);
        await using var running = server;
        var (created, createdBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/roles", """{"code":"reader","name":"Reader"}""", contoso));
        Assert.True(created.StatusCode == HttpStatusCode.Created, createdBody.ToString());
        var roleId = createdBody.GetProperty("id").GetString()!;

        var unknown = await FailureAsync(server, HttpMethod.Post, $"/api/roles/{roleId}:grant", """{"permissions":["no.such.permission"]}""", contoso, "zh-CN");
        Assert.Equal("no.such.permission", unknown.GetProperty("permission").GetString());
        Assert.Equal("权限 'no.such.permission' 不在目录里。", unknown.GetProperty("message").GetString());

        // 平台侧的权限码授给租户角色：两个占位符都替换掉
        var mismatch = await FailureAsync(server, HttpMethod.Post, $"/api/roles/{roleId}:grant", """{"permissions":["tenancy.tenants.read"]}""", contoso, "zh-CN");
        Assert.Equal("权限 'tenancy.tenants.read' 不能授予给 tenant 侧。", mismatch.GetProperty("message").GetString());
    }

    [Fact]
    public async Task ErrorsFromAResult_KeepTheirArguments()
    {
        var (server, _, contoso) = await SettingsTestHost.StartAsync(_database.ConnectionString);
        await using var running = server;

        // 不存在的用户：走 UserBatch 的结果，不是常量错误码
        var missing = Guid.NewGuid();
        var user = await FailureAsync(server, HttpMethod.Post, "/api/users:lock", $$"""{"ids":["{{missing}}"]}""", contoso, "zh-CN");
        Assert.Equal($"用户 '{missing}' 不存在。", user.GetProperty("message").GetString());

        // 锁定中的用户不能启用：新加的文案，带用户名
        var (created, createdBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/users", """{"userName":"locked-one","displayName":"Locked","password":"Passw0rd!x"}""", contoso));
        Assert.True(created.StatusCode == HttpStatusCode.Created, createdBody.ToString());
        var lockedId = createdBody.GetProperty("id").GetString()!;
        var (locked, lockedBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/users:lock", $$"""{"ids":["{{lockedId}}"]}""", contoso));
        Assert.True(locked.StatusCode == HttpStatusCode.OK, lockedBody.ToString());
        var enabled = await FailureAsync(server, HttpMethod.Post, "/api/users:enable", $$"""{"ids":["{{lockedId}}"]}""", contoso, "zh-CN");
        Assert.Equal("IDENTITY_CONFLICT_012", enabled.GetProperty("code").GetString());
        Assert.Equal("用户 'locked-one' 处于锁定状态，要先解锁。", enabled.GetProperty("message").GetString());

    }
}
