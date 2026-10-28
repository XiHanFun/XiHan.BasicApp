// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Net;
using System.Text.Json;
using XiHan.BasicApp.Integration.Tests.Infrastructure;

namespace XiHan.BasicApp.Integration.Tests;

/// <summary>
/// 用户设置（偏好、收藏与页面设置的跨端同步存储）：没存过即空值、整体覆盖写入、按（用户, 场景, 键）隔离、空值清空、键的文法、未登录被拒。
/// </summary>
public sealed class UserSettingsTests : IDisposable
{
    private readonly SqliteDatabase _database = new();

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task Settings_AreStoredPerUserSceneAndKey()
    {
        await using var server = await BasicAppTestServer.StartAsync(_database.ConnectionString);
        await server.SeedDefaultsAsync();
        await server.RunSeedersAsync();
        var superadmin = (await AuthFlowTests.LoginAsync(server, "superadmin", "SuperAdmin@123")).GetProperty("accessToken").GetString()!;
        var (createdUser, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/users", """{"userName":"carol","displayName":"Carol","password":"Carol@123456"}""", superadmin));
        Assert.Equal(HttpStatusCode.Created, createdUser.StatusCode);
        var carol = (await AuthFlowTests.LoginAsync(server, "carol", "Carol@123456")).GetProperty("accessToken").GetString()!;

        // 没存过：成功、值为空
        var (missing, missingBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/api/user-settings?scene=Preference&key=global", token: superadmin));
        Assert.Equal(HttpStatusCode.OK, missing.StatusCode);
        Assert.Equal(JsonValueKind.Null, missingBody.GetProperty("value").ValueKind);
        Assert.Equal(JsonValueKind.Null, missingBody.GetProperty("modifiedAt").ValueKind);

        // 写入即可读回；再写整体覆盖
        var (saved, savedBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Put, "/api/user-settings", """{"scene":"Preference","key":"global","value":"{\"theme\":\"dark\"}"}""", superadmin));
        Assert.Equal(HttpStatusCode.OK, saved.StatusCode);
        Assert.Equal("{\"theme\":\"dark\"}", savedBody.GetProperty("value").GetString());
        var (overwritten, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Put, "/api/user-settings", """{"scene":"Preference","key":"global","value":"{\"theme\":\"light\"}"}""", superadmin));
        Assert.Equal(HttpStatusCode.OK, overwritten.StatusCode);
        var (read, readBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/api/user-settings?scene=Preference&key=global", token: superadmin));
        Assert.Equal(HttpStatusCode.OK, read.StatusCode);
        Assert.Equal("{\"theme\":\"light\"}", readBody.GetProperty("value").GetString());
        Assert.NotEqual(JsonValueKind.Null, readBody.GetProperty("modifiedAt").ValueKind);

        // 场景与用户各自独立
        var (pageScene, pageSceneBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/api/user-settings?scene=Page&key=global", token: superadmin));
        Assert.Equal(HttpStatusCode.OK, pageScene.StatusCode);
        Assert.Equal(JsonValueKind.Null, pageSceneBody.GetProperty("value").ValueKind);
        var (otherUser, otherUserBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/api/user-settings?scene=Preference&key=global", token: carol));
        Assert.Equal(HttpStatusCode.OK, otherUser.StatusCode);
        Assert.Equal(JsonValueKind.Null, otherUserBody.GetProperty("value").ValueKind);

        // 空值清空；键的文法；未登录被拒
        var (cleared, clearedBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Put, "/api/user-settings", """{"scene":"Preference","key":"global","value":null}""", superadmin));
        Assert.Equal(HttpStatusCode.OK, cleared.StatusCode);
        Assert.Equal(JsonValueKind.Null, clearedBody.GetProperty("value").ValueKind);
        var (badKey, badKeyBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Put, "/api/user-settings", """{"scene":"Page","key":"bad key!","value":"{}"}""", superadmin));
        Assert.Equal(HttpStatusCode.BadRequest, badKey.StatusCode);
        Assert.True(badKeyBody.GetProperty("errors").TryGetProperty("key", out _));
        var (anonymous, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/api/user-settings?scene=Preference&key=global"));
        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
    }
}
