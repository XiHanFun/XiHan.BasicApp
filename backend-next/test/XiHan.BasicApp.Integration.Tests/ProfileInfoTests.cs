// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using XiHan.BasicApp.Integration.Tests.Infrastructure;

namespace XiHan.BasicApp.Integration.Tests;

/// <summary>
/// 个人资料：整体覆盖保存（空白当作没填）；改用户名要当前密码、与现在的相同或被占用即拒绝、改后新名字能登录、90 天一次；
/// 头像按文件头认 JPEG / PNG、不超过 2 MB，存平台侧的公开级别文件（租户一侧的人也能换签名地址、不进租户的文件列表），换头像与去掉头像都回收旧文件。
/// </summary>
public sealed class ProfileInfoTests : IDisposable
{
    private static readonly byte[] _png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52, 0x01];
    private static readonly byte[] _jpeg = [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0x02];

    private readonly SqliteDatabase _database = new();
    private readonly string _root = Path.Combine(Path.GetTempPath(), "xihan-avatars-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        _database.Dispose();
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    private static async Task<(HttpStatusCode Status, JsonElement Body)> SendAsync(BasicAppTestServer server, HttpMethod method, string path, string? json, string? token)
    {
        var (response, body) = await server.SendAsync(BasicAppTestServer.Request(method, path, json, token));
        return (response.StatusCode, body);
    }

    private static async Task<(HttpStatusCode Status, JsonElement Body)> AvatarAsync(BasicAppTestServer server, string token, string name, string contentType, byte[] content)
    {
        using var form = new MultipartFormDataContent();
        var file = new ByteArrayContent(content);
        file.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        form.Add(file, "file", name);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/profile/avatar") { Content = form };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var (response, body) = await server.SendAsync(request);
        return (response.StatusCode, body);
    }

    private static string Code(JsonElement body) => body.GetProperty("code").GetString()!;

    [Fact]
    public async Task Profile_UserName_AndAvatar_AreTheCallersOwn()
    {
        var (server, platform, tenant) = await SettingsTestHost.StartAsync(_database.ConnectionString, configure: builder => builder.Configuration.AddInMemoryCollection([new("BasicApp:Files:LocalRootPath", _root)]));
        await using var running = server;
        foreach (var name in new[] { "ann", "bob" })
        {
            Assert.Equal(HttpStatusCode.Created, (await SendAsync(server, HttpMethod.Post, "/api/users", JsonSerializer.Serialize(new { userName = name, displayName = name, password = "Pass@123456" }), platform)).Status);
        }

        var ann = (await AuthFlowTests.LoginAsync(server, "ann", "Pass@123456")).GetProperty("accessToken").GetString()!;

        // 资料整体覆盖，空白当作没填
        var (saved, savedBody) = await SendAsync(server, HttpMethod.Put, "/api/profile", """{"displayName":" Ann A ","realName":"  ","gender":"Female","birthday":"1990-05-01T00:00:00+08:00","country":"CN","remark":"hello"}""", ann);
        Assert.Equal(HttpStatusCode.OK, saved);
        Assert.Equal(("Ann A", JsonValueKind.Null, "Female", "CN", "hello"), (savedBody.GetProperty("user").GetProperty("displayName").GetString(), savedBody.GetProperty("realName").ValueKind, savedBody.GetProperty("gender").GetString(), savedBody.GetProperty("country").GetString(), savedBody.GetProperty("remark").GetString()));
        var (_, profile) = await SendAsync(server, HttpMethod.Get, "/api/profile", null, ann);
        Assert.Equal(new DateTimeOffset(1990, 5, 1, 0, 0, 0, TimeSpan.FromHours(8)), profile.GetProperty("birthday").GetDateTimeOffset());
        Assert.Equal(JsonValueKind.Null, profile.GetProperty("userNameChangeableAt").ValueKind);

        // 改用户名：密码不对、与现在相同、被占用、格式不对都拒绝；改后新名字能登录、旧名字不能，90 天一次
        Assert.Equal((HttpStatusCode.Forbidden, "IDENTITY_DENIED_003"), await StatusAsync(server, "/api/profile:change-username", """{"userName":"ann2","password":"wrong-one"}""", ann));
        Assert.Equal((HttpStatusCode.BadRequest, "IDENTITY_VALIDATION_017"), await StatusAsync(server, "/api/profile:change-username", """{"userName":"ann","password":"Pass@123456"}""", ann));
        Assert.Equal((HttpStatusCode.Conflict, "IDENTITY_CONFLICT_001"), await StatusAsync(server, "/api/profile:change-username", """{"userName":"bob","password":"Pass@123456"}""", ann));
        Assert.Equal(HttpStatusCode.BadRequest, (await SendAsync(server, HttpMethod.Post, "/api/profile:change-username", """{"userName":"Ann!","password":"Pass@123456"}""", ann)).Status);
        var (renamed, renamedBody) = await SendAsync(server, HttpMethod.Post, "/api/profile:change-username", """{"userName":"ann2","password":"Pass@123456"}""", ann);
        Assert.Equal((HttpStatusCode.OK, "ann2"), (renamed, renamedBody.GetProperty("user").GetProperty("userName").GetString()));
        var changedAt = renamedBody.GetProperty("userNameChangedAt").GetDateTimeOffset();
        Assert.Equal(changedAt.AddDays(90), renamedBody.GetProperty("userNameChangeableAt").GetDateTimeOffset());
        Assert.Equal(HttpStatusCode.OK, (await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/auth:login", """{"userName":"ann2","password":"Pass@123456"}"""))).Response.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/auth:login", """{"userName":"ann","password":"Pass@123456"}"""))).Response.StatusCode);
        Assert.Equal((HttpStatusCode.Conflict, "IDENTITY_CONFLICT_009"), await StatusAsync(server, "/api/profile:change-username", """{"userName":"ann3","password":"Pass@123456"}""", ann));

        // 头像：按文件头认，扩展名与内容类型骗不过；超过 2 MB 拒绝
        var (fake, fakeBody) = await AvatarAsync(server, ann, "me.png", "image/png", "not an image"u8.ToArray());
        Assert.Equal((HttpStatusCode.BadRequest, "IDENTITY_VALIDATION_016"), (fake, Code(fakeBody)));
        var huge = new byte[(2 * 1024 * 1024) + 1];
        _png.CopyTo(huge, 0);
        Assert.Equal(HttpStatusCode.BadRequest, (await AvatarAsync(server, ann, "big.png", "image/png", huge)).Status);

        // 传 PNG（扩展名写错也认）：成了本人的头像，存平台侧公开级别，租户一侧的人也能换签名地址，平台的文件列表里有、租户的没有
        var (first, firstBody) = await AvatarAsync(server, ann, "photo.gif", "application/octet-stream", _png);
        Assert.Equal(HttpStatusCode.OK, first);
        var firstId = firstBody.GetProperty("user").GetProperty("avatarFileId").GetString()!;
        Assert.Equal(HttpStatusCode.OK, (await SendAsync(server, HttpMethod.Get, $"/api/files/{firstId}:presign", null, ann)).Status);
        Assert.Equal(HttpStatusCode.OK, (await SendAsync(server, HttpMethod.Get, $"/api/files/{firstId}:presign", null, tenant)).Status);
        var (_, platformFile) = await SendAsync(server, HttpMethod.Get, $"/api/files/{firstId}", null, platform);
        Assert.Equal(("Public", "image/png", "avatar"), (platformFile.GetProperty("accessLevel").GetString(), platformFile.GetProperty("contentType").GetString(), platformFile.GetProperty("tags").GetString()));
        Assert.Equal(HttpStatusCode.NotFound, (await SendAsync(server, HttpMethod.Get, $"/api/files/{firstId}", null, tenant)).Status);

        // 换成 JPEG：旧的回收；去掉头像：新的也回收
        var (second, secondBody) = await AvatarAsync(server, ann, "photo.jpg", "image/jpeg", _jpeg);
        Assert.Equal(HttpStatusCode.OK, second);
        var secondId = secondBody.GetProperty("user").GetProperty("avatarFileId").GetString()!;
        Assert.NotEqual(firstId, secondId);
        Assert.Equal(HttpStatusCode.NotFound, (await SendAsync(server, HttpMethod.Get, $"/api/files/{firstId}:presign", null, ann)).Status);
        var (removed, removedBody) = await SendAsync(server, HttpMethod.Post, "/api/profile:remove-avatar", "{}", ann);
        Assert.Equal((HttpStatusCode.OK, JsonValueKind.Null), (removed, removedBody.GetProperty("user").GetProperty("avatarFileId").ValueKind));
        Assert.Equal(HttpStatusCode.NotFound, (await SendAsync(server, HttpMethod.Get, $"/api/files/{secondId}:presign", null, ann)).Status);
    }

    private static async Task<(HttpStatusCode, string)> StatusAsync(BasicAppTestServer server, string path, string json, string token)
    {
        var (status, body) = await SendAsync(server, HttpMethod.Post, path, json, token);
        return (status, Code(body));
    }
}
