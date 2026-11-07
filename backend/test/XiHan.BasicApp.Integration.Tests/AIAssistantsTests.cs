// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Net;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.AI;
using XiHan.BasicApp.Integration.Tests.Infrastructure;
using XiHan.Framework.Security.Abstractions.Permissions;

namespace XiHan.BasicApp.Integration.Tests;

/// <summary>
/// 验证 AI 模块的助手配置
/// </summary>
/// <remarks>
/// 编码唯一且不可改、缺省最多一个、停用的不能设缺省；只在平台一侧。保存时核对引用（提示词、提供商、工具），被助手引用的提示词与提供商不能删（方案 D5）
/// </remarks>
public sealed class AIAssistantsTests : IDisposable
{
    private const string Admin = "ai-admin";
    private static readonly string[] _unknownTool = ["no_such_tool"];
    private static readonly string[] _repeatedTool = ["users_list", "users_list"];

    private readonly SqliteDatabase _database = new();
    private readonly FakeOpenAIHandler _model = new();

    public void Dispose()
    {
        _database.Dispose();
        _model.Dispose();
    }

    private async Task<BasicAppTestServer> StartAsync(bool vectorStore = false)
    {
        var server = await BasicAppTestServer.StartAsync(_database.ConnectionString, configure: builder =>
        {
            _model.Attach(builder);
            if (vectorStore)
            {
                builder.Services.AddInMemoryVectorStore();
            }
        });
        await server.SeedDefaultsAsync();
        await server.SeedAsync([], [.. AIPermissions.All.Select(static p => new PermissionGrant(GrantSubject.ForRole(Admin), p.Code))]);
        return server;
    }

    private static string Token() => TestTokens.Issue("root", null, Admin);

    private static Task<(HttpResponseMessage Response, JsonElement Body)> SendAsync(BasicAppTestServer server, HttpMethod method, string path, object? body)
        => server.SendAsync(BasicAppTestServer.Request(method, path, body is null ? null : JsonSerializer.Serialize(body), Token()));

    private static async Task<JsonElement> CreateAsync(BasicAppTestServer server, object body)
    {
        var (created, json) = await SendAsync(server, HttpMethod.Post, "/api/ai-assistants", body);
        Assert.True(created.StatusCode == HttpStatusCode.Created, json.GetRawText());
        return json;
    }

    private static async Task ExpectAsync(BasicAppTestServer server, object body, HttpStatusCode status, string code)
    {
        var (response, json) = await SendAsync(server, HttpMethod.Post, "/api/ai-assistants", body);
        Assert.Equal(status, response.StatusCode);
        Assert.Equal(code, json.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Assistants_FollowTheRulesOf4x()
    {
        await using var server = await StartAsync();
        var token = await CreateAsync(server, new { code = "doc-helper", name = "  Doc helper ", greeting = "Hi", description = "Docs", avatar = "https://example.test/a.png", isDefault = true, order = 3 });
        Assert.Equal("Doc helper", token.GetProperty("name").GetString());
        Assert.True(token.GetProperty("enableKnowledge").GetBoolean());
        Assert.Equal(5, token.GetProperty("knowledgeTopK").GetInt32());
        Assert.Equal(10, token.GetProperty("historyRounds").GetInt32());
        Assert.Empty(token.GetProperty("tools").EnumerateArray());
        var first = token.GetProperty("id").GetString()!;

        // 编码唯一、不含空白；数值字段有范围
        await ExpectAsync(server, new { code = "doc-helper", name = "Other" }, HttpStatusCode.Conflict, AIErrors.AssistantCodeTaken);
        var (spaced, _) = await SendAsync(server, HttpMethod.Post, "/api/ai-assistants", new { code = "has space", name = "X" });
        Assert.Equal(HttpStatusCode.BadRequest, spaced.StatusCode);
        var (topK, _) = await SendAsync(server, HttpMethod.Post, "/api/ai-assistants", new { code = "x", name = "X", knowledgeTopK = 21 });
        Assert.Equal(HttpStatusCode.BadRequest, topK.StatusCode);
        var (rounds, _) = await SendAsync(server, HttpMethod.Post, "/api/ai-assistants", new { code = "x", name = "X", historyRounds = 51 });
        Assert.Equal(HttpStatusCode.BadRequest, rounds.StatusCode);

        // 缺省最多一个
        var second = await CreateAsync(server, new { code = "second", name = "Second", isDefault = true });
        var (_, firstNow) = await SendAsync(server, HttpMethod.Get, $"/api/ai-assistants/{first}", null);
        Assert.False(firstNow.GetProperty("isDefault").GetBoolean());
        var (setDefault, setDefaultBody) = await SendAsync(server, HttpMethod.Post, $"/api/ai-assistants/{first}:set-default", null);
        Assert.Equal(HttpStatusCode.OK, setDefault.StatusCode);
        Assert.True(setDefaultBody.GetProperty("isDefault").GetBoolean());

        // 修改：编码不可改（请求里没有这个字段），带上读到的并发戳
        var (updated, updatedBody) = await SendAsync(server, HttpMethod.Put, $"/api/ai-assistants/{first}", new { concurrencyStamp = setDefaultBody.GetProperty("concurrencyStamp").GetString(), name = "Renamed", description = "Docs", isDefault = true, enableKnowledge = false, historyRounds = 0 });
        Assert.True(updated.StatusCode == HttpStatusCode.OK, updatedBody.GetRawText());
        Assert.Equal("doc-helper", updatedBody.GetProperty("code").GetString());
        Assert.Equal("Renamed", updatedBody.GetProperty("name").GetString());
        var (stale, _) = await SendAsync(server, HttpMethod.Put, $"/api/ai-assistants/{first}", new { concurrencyStamp = setDefaultBody.GetProperty("concurrencyStamp").GetString(), name = "Stale" });
        Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);

        // 停用的不能设缺省；新建时也一样
        var (_, secondNow) = await SendAsync(server, HttpMethod.Get, $"/api/ai-assistants/{second.GetProperty("id").GetString()}", null);
        var (disabled, _) = await SendAsync(server, HttpMethod.Post, $"/api/ai-assistants/{second.GetProperty("id").GetString()}:set-status", new { concurrencyStamp = secondNow.GetProperty("concurrencyStamp").GetString(), status = "Disabled", remark = "paused" });
        Assert.Equal(HttpStatusCode.OK, disabled.StatusCode);
        var (cannot, cannotBody) = await SendAsync(server, HttpMethod.Post, $"/api/ai-assistants/{second.GetProperty("id").GetString()}:set-default", null);
        Assert.Equal(HttpStatusCode.Conflict, cannot.StatusCode);
        Assert.Equal(AIErrors.AssistantDisabled, cannotBody.GetProperty("code").GetString());
        await ExpectAsync(server, new { code = "paused", name = "Paused", isDefault = true, status = "Disabled" }, HttpStatusCode.Conflict, AIErrors.AssistantDisabled);

        // 列表与筛选
        var (list, listBody) = await SendAsync(server, HttpMethod.Post, "/api/ai-assistants:list", new { page = new { index = 1, size = 10 }, keyword = "second", filters = new[] { new { field = "status", @operator = "in", value = "Disabled" } } });
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        Assert.Equal("second", Assert.Single(listBody.GetProperty("items").EnumerateArray()).GetProperty("code").GetString());

        // 关键字也匹配简介；按挂知识库与缺省排序
        var (byDescription, byDescriptionBody) = await SendAsync(server, HttpMethod.Post, "/api/ai-assistants:list", new { page = new { index = 1, size = 10 }, keyword = "Docs" });
        Assert.Equal(HttpStatusCode.OK, byDescription.StatusCode);
        Assert.Equal("doc-helper", Assert.Single(byDescriptionBody.GetProperty("items").EnumerateArray()).GetProperty("code").GetString());
        var (sorted, sortedBody) = await SendAsync(server, HttpMethod.Post, "/api/ai-assistants:list", new { page = new { index = 1, size = 10 }, sorts = new object[] { new { field = "isDefault", descending = true }, new { field = "enableKnowledge", descending = false } } });
        Assert.True(sorted.StatusCode == HttpStatusCode.OK, sortedBody.GetRawText());
        Assert.Equal("doc-helper", sortedBody.GetProperty("items")[0].GetProperty("code").GetString());

        // 删除：逐项结果，同编码可以重建
        var missing = Guid.CreateVersion7();
        var (deleted, deletedBody) = await SendAsync(server, HttpMethod.Delete, $"/api/ai-assistants?ids={first}&ids={missing}", null);
        Assert.Equal(HttpStatusCode.OK, deleted.StatusCode);
        Assert.Equal(first, Assert.Single(deletedBody.GetProperty("succeeded").EnumerateArray()).GetString());
        Assert.Equal(AIErrors.AssistantNotFound, Assert.Single(deletedBody.GetProperty("failed").EnumerateArray()).GetProperty("code").GetString());
        _ = await CreateAsync(server, new { code = "doc-helper", name = "Again" });

        // 只在平台一侧
        var (tenant, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/ai-assistants:list", """{"page":{"index":1,"size":10}}""", TestTokens.Issue("alice", "acme", "admin")));
        Assert.Equal(HttpStatusCode.Forbidden, tenant.StatusCode);
    }

    [Fact]
    public async Task References_AreCheckedOnSave_AndReferencedItemsCannotBeDeleted()
    {
        await using var server = await StartAsync();
        await ExpectAsync(server, new { code = "a", name = "A", providerCode = "missing" }, HttpStatusCode.BadRequest, AIErrors.AssistantProviderUnavailable);
        await ExpectAsync(server, new { code = "a", name = "A", knowledgeProviderCode = "missing" }, HttpStatusCode.BadRequest, AIErrors.AssistantProviderUnavailable);
        await ExpectAsync(server, new { code = "a", name = "A", promptCode = "missing" }, HttpStatusCode.BadRequest, AIErrors.AssistantPromptUnavailable);
        await ExpectAsync(server, new { code = "a", name = "A", tools = _unknownTool }, HttpStatusCode.BadRequest, AIErrors.AssistantToolUnavailable);

        var (provider, providerBody) = await SendAsync(server, HttpMethod.Post, "/api/ai-providers", new { code = "model", name = "Model", vendor = "OpenAI", chatModel = "chat-a", embeddingModel = "embed-a", endpoint = FakeOpenAIHandler.Endpoint, apiKey = "test-key" });
        Assert.True(provider.StatusCode == HttpStatusCode.Created, providerBody.GetRawText());
        var (prompt, promptBody) = await SendAsync(server, HttpMethod.Post, "/api/ai-prompts", new { code = "answer", name = "Answer", content = "Answer." });
        Assert.True(prompt.StatusCode == HttpStatusCode.Created, promptBody.GetRawText());

        // 停用的提示词也不能引用
        var (_, promptNow) = await SendAsync(server, HttpMethod.Get, $"/api/ai-prompts/{promptBody.GetProperty("id").GetString()}", null);
        var (off, _) = await SendAsync(server, HttpMethod.Post, $"/api/ai-prompts/{promptBody.GetProperty("id").GetString()}:set-status", new { concurrencyStamp = promptNow.GetProperty("concurrencyStamp").GetString(), status = "Disabled" });
        Assert.Equal(HttpStatusCode.OK, off.StatusCode);
        await ExpectAsync(server, new { code = "a", name = "A", promptCode = "answer" }, HttpStatusCode.BadRequest, AIErrors.AssistantPromptUnavailable);
        var (_, promptDisabled) = await SendAsync(server, HttpMethod.Get, $"/api/ai-prompts/{promptBody.GetProperty("id").GetString()}", null);
        var (on, _) = await SendAsync(server, HttpMethod.Post, $"/api/ai-prompts/{promptBody.GetProperty("id").GetString()}:set-status", new { concurrencyStamp = promptDisabled.GetProperty("concurrencyStamp").GetString(), status = "Active" });
        Assert.Equal(HttpStatusCode.OK, on.StatusCode);

        var assistant = await CreateAsync(server, new { code = "a", name = "A", promptCode = "answer", providerCode = "model", knowledgeProviderCode = "model", tools = _repeatedTool });
        Assert.Equal(["users_list"], assistant.GetProperty("tools").EnumerateArray().Select(static t => t.GetString()));

        // 被引用的不能删
        var (promptDelete, promptDeleteBody) = await SendAsync(server, HttpMethod.Delete, $"/api/ai-prompts?ids={promptBody.GetProperty("id").GetString()}", null);
        Assert.Equal(HttpStatusCode.OK, promptDelete.StatusCode);
        Assert.Equal(AIErrors.PromptInUse, Assert.Single(promptDeleteBody.GetProperty("failed").EnumerateArray()).GetProperty("code").GetString());
        var (providerDelete, providerDeleteBody) = await SendAsync(server, HttpMethod.Delete, $"/api/ai-providers?ids={providerBody.GetProperty("id").GetString()}", null);
        Assert.Equal(HttpStatusCode.OK, providerDelete.StatusCode);
        Assert.Equal(AIErrors.ProviderInUse, Assert.Single(providerDeleteBody.GetProperty("failed").EnumerateArray()).GetProperty("code").GetString());

        // 助手删了就能删
        var (removed, _) = await SendAsync(server, HttpMethod.Delete, $"/api/ai-assistants?ids={assistant.GetProperty("id").GetString()}", null);
        Assert.Equal(HttpStatusCode.OK, removed.StatusCode);
        var (_, promptAfter) = await SendAsync(server, HttpMethod.Delete, $"/api/ai-prompts?ids={promptBody.GetProperty("id").GetString()}", null);
        Assert.Single(promptAfter.GetProperty("succeeded").EnumerateArray());
        var (_, providerAfter) = await SendAsync(server, HttpMethod.Delete, $"/api/ai-providers?ids={providerBody.GetProperty("id").GetString()}", null);
        Assert.Single(providerAfter.GetProperty("succeeded").EnumerateArray());
    }

    [Fact]
    public async Task Tools_ListsTheCatalog_AndTellsWhetherAVectorStoreExists()
    {
        await using (var without = await StartAsync())
        {
            var (listed, body) = await SendAsync(without, HttpMethod.Get, "/api/ai-assistants:tools", null);
            Assert.True(listed.StatusCode == HttpStatusCode.OK, body.GetRawText());
            Assert.False(body.GetProperty("knowledgeAvailable").GetBoolean());
            var names = body.GetProperty("items").EnumerateArray().Select(static t => t.GetProperty("name").GetString()!).ToList();
            Assert.Contains("users_list", names);
            Assert.Contains("knowledge_documents_search", names);
            Assert.Equal(names.Order(StringComparer.Ordinal), names);
        }

        await using var with = await StartAsync(vectorStore: true);
        var (_, withBody) = await SendAsync(with, HttpMethod.Get, "/api/ai-assistants:tools", null);
        Assert.True(withBody.GetProperty("knowledgeAvailable").GetBoolean());
    }
}
