// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Net;
using System.Text.Json;
using XiHan.BasicApp.Integration.Tests.Infrastructure;

namespace XiHan.BasicApp.Integration.Tests;

/// <summary>
/// 示例操作在 HTTP 上的契约：路由推导、认证与授权、字段级校验错误、租户隔离、分页。
/// </summary>
public sealed class UsersHttpTests : IDisposable
{
    private readonly SqliteDatabase _database = new();

    public void Dispose() => _database.Dispose();

    private async Task<BasicAppTestServer> StartAsync()
    {
        var server = await BasicAppTestServer.StartAsync(_database.ConnectionString);
        await server.SeedDefaultsAsync();
        return server;
    }

    [Fact]
    public async Task Create_RequiresAuthentication_ThenPermission_ThenTenant()
    {
        await using var server = await StartAsync();
        const string body = """{"userName":"alice.w","displayName":"Alice","email":"alice@example.com"}""";

        var (anonymous, anonymousBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/users", body));
        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
        Assert.Equal("application/problem+json", anonymous.Content.Headers.ContentType!.MediaType);
        Assert.Equal("SECURITY_DENIED_001", anonymousBody.GetProperty("code").GetString());

        var (viewer, viewerBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/users", body, TestTokens.Issue("bob", "acme", "viewer")));
        Assert.Equal(HttpStatusCode.Forbidden, viewer.StatusCode);
        Assert.Equal("SECURITY_DENIED_004", viewerBody.GetProperty("code").GetString());

        // 租户主体只能建本租户的用户：指定别的注册地是校验错误
        var (mismatch, mismatchBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/users", """{"userName":"alice.w","displayName":"Alice","homeTenantId":"globex"}""", TestTokens.Issue("alice", "acme", "admin")));
        Assert.Equal(HttpStatusCode.BadRequest, mismatch.StatusCode);
        Assert.Equal("IDENTITY_VALIDATION_001", mismatchBody.GetProperty("code").GetString());
        Assert.Equal("globex", mismatchBody.GetProperty("details").GetProperty("requested").GetString());

        // 平台主体没有租户声明：不指定注册地即平台账号，指定了就建到那个租户
        var (platform, platformBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/users", """{"userName":"root.ops","displayName":"Ops"}""", TestTokens.Issue("root", null, "admin")));
        Assert.Equal(HttpStatusCode.Created, platform.StatusCode);
        Assert.Equal("platform", platformBody.GetProperty("homeTenantId").GetString());
        var (delegated, delegatedBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/users", """{"userName":"globex.admin","displayName":"Globex Admin","homeTenantId":"globex"}""", TestTokens.Issue("root", null, "admin")));
        Assert.Equal(HttpStatusCode.Created, delegated.StatusCode);
        Assert.Equal("globex", delegatedBody.GetProperty("homeTenantId").GetString());

        var (created, createdBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/users", body, TestTokens.Issue("alice", "acme", "admin")));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.Equal("alice.w", createdBody.GetProperty("userName").GetString());
        Assert.Equal("acme", createdBody.GetProperty("homeTenantId").GetString());
        Assert.Equal("Active", createdBody.GetProperty("status").GetString());
        Assert.Equal(JsonValueKind.String, createdBody.GetProperty("createdAt").ValueKind);
        Assert.True(Guid.TryParse(createdBody.GetProperty("id").GetString(), out _));

        var (duplicate, duplicateBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/users", body, TestTokens.Issue("alice", "acme", "admin")));
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        Assert.Equal("IDENTITY_CONFLICT_001", duplicateBody.GetProperty("code").GetString());
        Assert.Equal("alice.w", duplicateBody.GetProperty("details").GetProperty("userName").GetString());

        // 同名在另一个租户可以创建
        var (other, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/users", body, TestTokens.Issue("carol", "globex", "admin")));
        Assert.Equal(HttpStatusCode.Created, other.StatusCode);
    }

    [Fact]
    public async Task Create_ValidationFailures_ReturnFieldErrors_LocalizedByAcceptLanguage()
    {
        await using var server = await StartAsync();
        var token = TestTokens.Issue("alice", "acme", "admin");

        // Schema 校验：长度、模式、必填；Validate 阶段拒绝，不进 Transaction / Handle
        var (schema, schemaBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/users", """{"userName":"A!","email":"x"}""", token, "zh-CN"));
        Assert.Equal(HttpStatusCode.BadRequest, schema.StatusCode);
        Assert.Equal("KERNEL_VALIDATION_003", schemaBody.GetProperty("code").GetString());
        Assert.Equal("zh-CN", schema.Content.Headers.ContentLanguage.Single());
        var errors = schemaBody.GetProperty("errors");
        Assert.True(errors.TryGetProperty("userName", out var userName) && userName.GetArrayLength() >= 1);
        Assert.True(errors.TryGetProperty("displayName", out var displayName) && displayName.GetArrayLength() >= 1);

        // 自校验：显示名只有空白、邮箱格式；文案按 Accept-Language 取自本包的消息目录
        var (self, selfBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/users", """{"userName":"alice.w","displayName":"   ","email":"not-an-email"}""", token, "zh-CN"));
        Assert.Equal(HttpStatusCode.BadRequest, self.StatusCode);
        Assert.Equal("KERNEL_VALIDATION_004", selfBody.GetProperty("code").GetString());
        Assert.Equal("显示名不能只有空白。", selfBody.GetProperty("errors").GetProperty("displayName")[0].GetString());
        Assert.Equal("邮箱地址格式不正确。", selfBody.GetProperty("errors").GetProperty("email")[0].GetString());

        var (english, englishBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/users", """{"userName":"alice.w","displayName":"   "}""", token, "en-US"));
        Assert.Equal(HttpStatusCode.BadRequest, english.StatusCode);
        Assert.Equal("The display name must not be blank.", englishBody.GetProperty("errors").GetProperty("displayName")[0].GetString());
    }

    [Fact]
    public async Task List_And_Get_AreScopedToTheCallersTenant()
    {
        await using var server = await StartAsync();
        var acme = TestTokens.Issue("alice", "acme", "admin");
        var globex = TestTokens.Issue("carol", "globex", "admin");
        var ids = new List<string>();
        foreach (var (token, name) in new[] { (acme, "user1"), (acme, "user2"), (globex, "user3") })
        {
            var (response, body) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/users", $$"""{"userName":"{{name}}","displayName":"User {{name}}"}""", token));
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            ids.Add(body.GetProperty("id").GetString()!);
        }

        // 带 PageRequest 的 list 落到 POST /api/users:list
        var (list, listBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/users:list", """{"page":{"index":1,"size":1},"sorts":[{"field":"userName"}]}""", acme));
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        Assert.Equal(2, listBody.GetProperty("total").GetInt64());
        Assert.Equal(1, listBody.GetProperty("items").GetArrayLength());
        Assert.Equal("user1", listBody.GetProperty("items")[0].GetProperty("userName").GetString());
        Assert.Equal(1, listBody.GetProperty("page").GetProperty("size").GetInt32());

        var (viewerList, viewerBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/users:list", """{"keyword":"user2"}""", TestTokens.Issue("bob", "acme", "viewer")));
        Assert.Equal(HttpStatusCode.OK, viewerList.StatusCode);
        Assert.Equal(1, viewerBody.GetProperty("total").GetInt64());

        var (filtered, filteredBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/users:list", """{"filters":[{"field":"status","operator":"eq","value":"Active"}]}""", globex));
        Assert.Equal(1, filteredBody.GetProperty("total").GetInt64());
        Assert.Equal("user3", filteredBody.GetProperty("items")[0].GetProperty("userName").GetString());

        var (unsupported, unsupportedBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/users:list", """{"filters":[{"field":"password","operator":"eq","value":"x"}]}""", acme));
        Assert.Equal(HttpStatusCode.BadRequest, unsupported.StatusCode);
        Assert.Equal("IDENTITY_VALIDATION_002", unsupportedBody.GetProperty("code").GetString());

        // 页大小上限 500 折进 Schema：越界在 Validate 阶段以字段路径 page.size 拒绝
        var (tooLarge, tooLargeBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/users:list", """{"page":{"index":1,"size":501}}""", acme));
        Assert.Equal(HttpStatusCode.BadRequest, tooLarge.StatusCode);
        Assert.Equal("KERNEL_VALIDATION_003", tooLargeBody.GetProperty("code").GetString());
        Assert.True(tooLargeBody.GetProperty("errors").TryGetProperty("page.size", out _));

        // get：路由值 {id}；别的租户的用户是 404
        var (get, getBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, $"/api/users/{ids[0]}", token: acme));
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);
        Assert.Equal("user1", getBody.GetProperty("userName").GetString());
        var (missing, missingBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, $"/api/users/{ids[0]}", token: globex));
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        Assert.Equal("IDENTITY_NOTFOUND_001", missingBody.GetProperty("code").GetString());
        Assert.Equal(ids[0], missingBody.GetProperty("details").GetProperty("id").GetString());

        // 平台主体（无租户）看全部（本用例建的 3 个；没有跑种子，没有 superadmin）
        var (all, allBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/users:list", "{}", TestTokens.Issue("root", null, "admin")));
        Assert.Equal(HttpStatusCode.OK, all.StatusCode);
        Assert.Equal(3, allBody.GetProperty("total").GetInt64());
    }

    [Fact]
    public async Task OpenApi_ServesTheSameDocumentAsContractsExport_WithPermissionExtensions()
    {
        await using var server = await StartAsync();
        var (response, document) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/openapi.json", acceptLanguage: "en-US"));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("6.0.0-preview.1", document.GetProperty("info").GetProperty("version").GetString());
        Assert.Equal("identity.users.create", document.GetProperty("paths").GetProperty("/api/users").GetProperty("post").GetProperty("x-xihan-permission").GetString());
        Assert.True(document.GetProperty("components").GetProperty("x-xihan-permissions").TryGetProperty("identity.users.read", out _));
        Assert.True(document.GetProperty("components").GetProperty("securitySchemes").TryGetProperty("bearer", out _));

        var served = await server.Client.GetByteArrayAsync("/openapi.json");
        var exported = Host.Contracts.ContractsExporter.Build(server.App);
        Assert.Equal(served, exported);
    }
}
