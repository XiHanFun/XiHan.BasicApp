// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Net;
using System.Text.Json;
using XiHan.BasicApp.Integration.Tests.Infrastructure;

namespace XiHan.BasicApp.Integration.Tests;

/// <summary>
/// 验证服务端强制的锁屏。
/// </summary>
/// <remarks>
/// 4.x 行为，服务端强制。
/// 没有密码不能锁；锁定的会话除解锁与注销外一律 <c>IDENTITY_DENIED_002</c>（带原因），同一用户的其他会话不受影响；
/// 刷新照旧可用但新令牌仍锁着；密码不对带剩余次数，连错 5 次会话被吊销；解对即恢复，没锁时解锁无事；锁定中能注销。
/// </remarks>
public sealed class LockScreenTests : IDisposable
{
    private readonly SqliteDatabase _database = new();

    public void Dispose() => _database.Dispose();

    private static async Task<(HttpStatusCode Status, JsonElement Body)> SendAsync(BasicAppTestServer server, HttpMethod method, string path, string? json, string? token)
    {
        var (response, body) = await server.SendAsync(BasicAppTestServer.Request(method, path, json, token));
        return (response.StatusCode, body);
    }

    private static async Task<string?> MeCodeAsync(BasicAppTestServer server, string token)
    {
        var (status, body) = await SendAsync(server, HttpMethod.Get, "/api/auth:me", null, token);
        return status == HttpStatusCode.OK ? null : body.GetProperty("code").GetString();
    }

    [Fact]
    public async Task LockedSessions_OnlyUnlockOrLogout_AndWrongPasswordsEndTheSession()
    {
        var (server, _, _) = await SettingsTestHost.StartAsync(_database.ConnectionString);
        await using var running = server;
        var first = await AuthFlowTests.LoginAsync(server, "superadmin", "SuperAdmin@123");
        var access = first.GetProperty("accessToken").GetString()!;
        var other = (await AuthFlowTests.LoginAsync(server, "superadmin", "SuperAdmin@123")).GetProperty("accessToken").GetString()!;

        // 没有密码不能锁；锁上后这个会话只放行解锁与注销，同一用户的另一个会话照常
        Assert.Equal("IDENTITY_VALIDATION_011", (await SendAsync(server, HttpMethod.Post, "/api/auth:lock", """{"password":"  "}""", access)).Body.GetProperty("code").GetString());
        var (lockedStatus, locked) = await SendAsync(server, HttpMethod.Post, "/api/auth:lock", """{"password":"open-sesame"}""", access);
        Assert.Equal((HttpStatusCode.OK, "ScreenLock"), (lockedStatus, locked.GetProperty("reason").GetString()));
        var (deniedStatus, denied) = await SendAsync(server, HttpMethod.Get, "/api/auth:me", null, access);
        Assert.Equal((HttpStatusCode.Forbidden, "IDENTITY_DENIED_002", "ScreenLock"), (deniedStatus, denied.GetProperty("code").GetString(), denied.GetProperty("details").GetProperty("reason").GetString()));
        Assert.Equal("IDENTITY_DENIED_002", (await SendAsync(server, HttpMethod.Post, "/api/auth:lock", """{"password":"again"}""", access)).Body.GetProperty("code").GetString());
        Assert.Null(await MeCodeAsync(server, other));

        // 刷新照旧，换来的令牌仍锁着（锁在会话上）
        var (refreshed, refreshedBody) = await SendAsync(server, HttpMethod.Post, "/api/auth:refresh", JsonSerializer.Serialize(new { refreshToken = first.GetProperty("refreshToken").GetString() }), null);
        Assert.Equal(HttpStatusCode.OK, refreshed);
        access = refreshedBody.GetProperty("accessToken").GetString()!;
        Assert.Equal("IDENTITY_DENIED_002", await MeCodeAsync(server, access));

        // 密码不对带剩余次数；解对即恢复；没锁时解锁无事
        var (wrongStatus, wrong) = await SendAsync(server, HttpMethod.Post, "/api/auth:unlock", """{"password":"nope"}""", access);
        Assert.Equal((HttpStatusCode.Forbidden, "IDENTITY_DENIED_013", 4), (wrongStatus, wrong.GetProperty("code").GetString(), wrong.GetProperty("details").GetProperty("remaining").GetInt32()));
        Assert.Equal(3, (await SendAsync(server, HttpMethod.Post, "/api/auth:unlock", "{}", access)).Body.GetProperty("details").GetProperty("remaining").GetInt32());
        Assert.True((await SendAsync(server, HttpMethod.Post, "/api/auth:unlock", """{"password":"open-sesame"}""", access)).Body.GetProperty("unlocked").GetBoolean());
        Assert.Null(await MeCodeAsync(server, access));
        Assert.False((await SendAsync(server, HttpMethod.Post, "/api/auth:unlock", """{"password":"whatever"}""", access)).Body.GetProperty("unlocked").GetBoolean());

        // 重新锁：失败计数从零开始；连错 5 次会话被吊销，之后一切请求都是会话失效
        Assert.Equal(HttpStatusCode.OK, (await SendAsync(server, HttpMethod.Post, "/api/auth:lock", """{"password":"second"}""", access)).Status);
        for (var attempt = 1; attempt < 5; attempt++)
        {
            Assert.Equal(5 - attempt, (await SendAsync(server, HttpMethod.Post, "/api/auth:unlock", """{"password":"bad"}""", access)).Body.GetProperty("details").GetProperty("remaining").GetInt32());
        }

        Assert.Equal("IDENTITY_DENIED_014", (await SendAsync(server, HttpMethod.Post, "/api/auth:unlock", """{"password":"bad"}""", access)).Body.GetProperty("code").GetString());
        Assert.Equal("IDENTITY_DENIED_001", await MeCodeAsync(server, access));
        Assert.Equal("IDENTITY_DENIED_001", (await SendAsync(server, HttpMethod.Post, "/api/auth:unlock", """{"password":"second"}""", access)).Body.GetProperty("code").GetString());

        // 锁定中能注销
        Assert.Equal(HttpStatusCode.OK, (await SendAsync(server, HttpMethod.Post, "/api/auth:lock", """{"password":"third"}""", other)).Status);
        Assert.Equal(HttpStatusCode.OK, (await SendAsync(server, HttpMethod.Post, "/api/auth:logout", "{}", other)).Status);
        Assert.Equal("IDENTITY_DENIED_001", await MeCodeAsync(server, other));
    }
}
