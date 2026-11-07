// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Net;
using System.Text.Json;
using XiHan.BasicApp.AI;
using XiHan.BasicApp.Integration.Tests.Infrastructure;
using XiHan.Framework.Security.Abstractions.Permissions;

namespace XiHan.BasicApp.Integration.Tests;

/// <summary>
/// 验证 AI 模块的提示词
/// </summary>
/// <remarks>
/// 编码唯一且不可改、正文原样保存、版本只是标签（同一编码只有一行）、列表不带正文；只在平台一侧
/// </remarks>
public sealed class AIPromptsTests : IDisposable
{
    private const string Admin = "ai-admin";
    private const string Reader = "ai-reader";

    private readonly SqliteDatabase _database = new();

    public void Dispose() => _database.Dispose();

    private async Task<BasicAppTestServer> StartAsync()
    {
        var server = await BasicAppTestServer.StartAsync(_database.ConnectionString);
        await server.SeedDefaultsAsync();
        await server.SeedAsync([], [
            .. AIPermissions.All.Select(static p => new PermissionGrant(GrantSubject.ForRole(Admin), p.Code)),
            new PermissionGrant(GrantSubject.ForRole(Reader), AIPermissions.AiPromptsRead),
        ]);
        return server;
    }

    private static string Json(object value) => JsonSerializer.Serialize(value);

    private static Task<(HttpResponseMessage Response, JsonElement Body)> SendAsync(BasicAppTestServer server, HttpMethod method, string path, string? json, string token)
        => server.SendAsync(BasicAppTestServer.Request(method, path, json, token));

    [Fact]
    public async Task Prompts_FollowTheRulesOf4x()
    {
        await using var server = await StartAsync();
        var admin = TestTokens.Issue("root", null, Admin);
        const string content = "  You are a helpful assistant.\n  Answer in {language}.\n";

        // 新建：正文原样保存（首尾空白与占位符都不动，没有变量渲染）
        var (created, body) = await SendAsync(server, HttpMethod.Post, "/api/ai-prompts", Json(new { code = "rag-answer", name = "RAG", content, category = "RAG", version = "v1", order = 5 }), admin);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.Equal(content, body.GetProperty("content").GetString());
        Assert.Equal("Active", body.GetProperty("status").GetString());
        var id = body.GetProperty("id").GetString()!;

        // 编码唯一，版本只是标签：同编码换个版本也是冲突
        var (taken, takenBody) = await SendAsync(server, HttpMethod.Post, "/api/ai-prompts", Json(new { code = "rag-answer", name = "Other", content = "x", version = "v2" }), admin);
        Assert.Equal(HttpStatusCode.Conflict, taken.StatusCode);
        Assert.Equal(AIErrors.PromptCodeTaken, takenBody.GetProperty("code").GetString());

        // 正文必填
        var (blank, blankBody) = await SendAsync(server, HttpMethod.Post, "/api/ai-prompts", Json(new { code = "blank", name = "Blank", content = "   " }), admin);
        Assert.Equal(HttpStatusCode.BadRequest, blank.StatusCode);
        Assert.True(blankBody.GetProperty("errors").TryGetProperty("content", out _));

        // 列表不带正文；关键字、状态多选、分类、缺省排序（排序号再名称）、不支持的筛选
        var (_, second) = await SendAsync(server, HttpMethod.Post, "/api/ai-prompts", Json(new { code = "code-review", name = "Code", content = "Review it.", category = "Code", order = 1 }), admin);
        var (_, list) = await SendAsync(server, HttpMethod.Post, "/api/ai-prompts:list", """{"page":{"index":1,"size":20},"filters":[{"field":"status","operator":"in","value":"Active,Disabled"}]}""", admin);
        Assert.Equal(["code-review", "rag-answer"], list.GetProperty("items").EnumerateArray().Select(static i => i.GetProperty("code").GetString()));
        Assert.All(list.GetProperty("items").EnumerateArray(), static i => Assert.False(i.TryGetProperty("content", out _)));
        var (_, keyword) = await SendAsync(server, HttpMethod.Post, "/api/ai-prompts:list", """{"page":{"index":1,"size":20},"keyword":"rag"}""", admin);
        Assert.Equal(1, keyword.GetProperty("total").GetInt32());
        var (_, category) = await SendAsync(server, HttpMethod.Post, "/api/ai-prompts:list", """{"page":{"index":1,"size":20},"filters":[{"field":"category","operator":"eq","value":"Code"}]}""", admin);
        Assert.Equal("code-review", category.GetProperty("items")[0].GetProperty("code").GetString());
        var (badFilter, badFilterBody) = await SendAsync(server, HttpMethod.Post, "/api/ai-prompts:list", """{"page":{"index":1,"size":20},"filters":[{"field":"content","operator":"contains","value":"x"}]}""", admin);
        Assert.Equal(HttpStatusCode.BadRequest, badFilter.StatusCode);
        Assert.Equal(AIErrors.QueryUnsupported, badFilterBody.GetProperty("code").GetString());

        // 详情带正文；修改不改编码，过期的并发戳即冲突
        var (_, detail) = await SendAsync(server, HttpMethod.Get, $"/api/ai-prompts/{id}", null, admin);
        Assert.Equal(content, detail.GetProperty("content").GetString());
        var (updated, updatedBody) = await SendAsync(server, HttpMethod.Put, $"/api/ai-prompts/{id}", Json(new { concurrencyStamp = detail.GetProperty("concurrencyStamp").GetString(), name = "RAG answer", content = "New.", version = "v2", order = 2 }), admin);
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        Assert.Equal("rag-answer", updatedBody.GetProperty("code").GetString());
        Assert.Equal("New.", updatedBody.GetProperty("content").GetString());
        Assert.Equal(JsonValueKind.Null, updatedBody.GetProperty("category").ValueKind);
        var (stale, _) = await SendAsync(server, HttpMethod.Put, $"/api/ai-prompts/{id}", Json(new { concurrencyStamp = detail.GetProperty("concurrencyStamp").GetString(), name = "x", content = "y" }), admin);
        Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);

        // 启停带并发戳；给了备注即替换
        var (disabled, disabledBody) = await SendAsync(server, HttpMethod.Post, $"/api/ai-prompts/{id}:set-status", Json(new { concurrencyStamp = updatedBody.GetProperty("concurrencyStamp").GetString(), status = "Disabled", remark = "paused" }), admin);
        Assert.Equal(HttpStatusCode.OK, disabled.StatusCode);
        Assert.Equal("Disabled", disabledBody.GetProperty("status").GetString());
        Assert.Equal("paused", disabledBody.GetProperty("remark").GetString());

        // 删除：批量，不存在的逐项报不存在；软删除后同编码可以重建
        var missing = Guid.NewGuid();
        var (deleted, deletedBody) = await SendAsync(server, HttpMethod.Delete, $"/api/ai-prompts?ids={id}&ids={missing}", null, admin);
        Assert.Equal(HttpStatusCode.OK, deleted.StatusCode);
        Assert.Equal([id], deletedBody.GetProperty("succeeded").EnumerateArray().Select(static i => i.GetString()));
        Assert.Equal(AIErrors.PromptNotFound, deletedBody.GetProperty("failed")[0].GetProperty("code").GetString());
        var (gone, goneBody) = await SendAsync(server, HttpMethod.Get, $"/api/ai-prompts/{id}", null, admin);
        Assert.Equal(HttpStatusCode.NotFound, gone.StatusCode);
        Assert.Equal(AIErrors.PromptNotFound, goneBody.GetProperty("code").GetString());
        var (recreated, _) = await SendAsync(server, HttpMethod.Post, "/api/ai-prompts", Json(new { code = "rag-answer", name = "RAG", content = "Again." }), admin);
        Assert.Equal(HttpStatusCode.Created, recreated.StatusCode);
        Assert.Equal(JsonValueKind.String, second.GetProperty("id").ValueKind);
    }

    [Fact]
    public async Task Prompts_AreOnlyOnThePlatformSide_AndRequireTheirPermissions()
    {
        await using var server = await StartAsync();
        // 只读角色能看不能写
        var reader = TestTokens.Issue("reader", null, Reader);
        var (read, _) = await SendAsync(server, HttpMethod.Post, "/api/ai-prompts:list", """{"page":{"index":1,"size":20}}""", reader);
        Assert.Equal(HttpStatusCode.OK, read.StatusCode);
        var (write, writeBody) = await SendAsync(server, HttpMethod.Post, "/api/ai-prompts", Json(new { code = "x", name = "x", content = "x" }), reader);
        Assert.Equal(HttpStatusCode.Forbidden, write.StatusCode);
        Assert.StartsWith("SECURITY_DENIED", writeBody.GetProperty("code").GetString(), StringComparison.Ordinal);

        // 租户管理员持 *，平台侧的权限码按作用侧收窄：在授权阶段拒绝
        var tenantAdmin = TestTokens.Issue("alice", "acme", "admin");
        var (list, listBody) = await SendAsync(server, HttpMethod.Post, "/api/ai-prompts:list", """{"page":{"index":1,"size":20}}""", tenantAdmin);
        Assert.Equal(HttpStatusCode.Forbidden, list.StatusCode);
        Assert.Equal("SECURITY_DENIED_004", listBody.GetProperty("code").GetString());
    }
}
