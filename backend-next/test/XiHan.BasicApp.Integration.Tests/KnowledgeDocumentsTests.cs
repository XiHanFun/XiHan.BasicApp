// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Net;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using SqlSugar;
using XiHan.BasicApp.AI;
using XiHan.BasicApp.AI.Domain.Entities;
using XiHan.BasicApp.Integration.Tests.Infrastructure;
using XiHan.BasicApp.Runtime.Schema;
using XiHan.BasicApp.Runtime.Persistence;
using XiHan.Framework.Kernel.Hosting;
using XiHan.Framework.MultiTenancy.Abstractions.Sharding;
using XiHan.Framework.Persistence.SqlSugar;
using XiHan.Framework.Primitives;
using XiHan.Framework.Security.Abstractions.Permissions;

namespace XiHan.BasicApp.Integration.Tests;

/// <summary>
/// 验证 AI 模块的知识库
/// </summary>
/// <remarks>
/// 摄入、检索与问答的引用编号对得上；平台与各租户互不可见（各自一个向量集合）；重建先写后删；删除带向量；
/// 索引失败以 <c>Failed</c> 保存并成功返回（没配向量库、模型不可达、维度不符）；被引用的提供商不能删
/// 模型端点是仿真的 OpenAI 兼容端点，向量库用内存连接器；设了 <c>XIHAN_TEST_QDRANT</c>（如 <c>http://localhost:6334</c>）另在真实 Qdrant 上跑一遍摄入与检索
/// </remarks>
public sealed class KnowledgeDocumentsTests : IDisposable
{
    private const string Admin = "ai-admin";
    private const string Reader = "ai-reader";
    private const string ProviderCode = "embedder";

    private readonly SqliteDatabase _database = new();

    private readonly FakeOpenAIHandler _model = new();

    public void Dispose()
    {
        _database.Dispose();
        _model.Dispose();
    }

    private async Task<BasicAppTestServer> StartAsync(bool vectorStore = true, IEnumerable<KeyValuePair<string, string?>>? settings = null, IReadOnlyDictionary<string, string>? acmeProperties = null)
    {
        var server = await BasicAppTestServer.StartAsync(
            _database.ConnectionString,
            settings: [new("XiHan:AI:Knowledge:Dimensions", FakeOpenAIHandler.DefaultDimensions.ToString(System.Globalization.CultureInfo.InvariantCulture)), .. settings ?? []],
            configure: builder =>
            {
                _model.Attach(builder);
                if (vectorStore && settings is null)
                {
                    builder.Services.AddInMemoryVectorStore();
                }
            });
        await server.SeedDefaultsAsync(acmeProperties);
        await server.SeedAsync([], [
            .. AIPermissions.All.Select(static p => new PermissionGrant(GrantSubject.ForRole(Admin), p.Code)),
            new PermissionGrant(GrantSubject.ForRole(Reader), AIPermissions.KnowledgeDocumentsRead),
        ]);
        var (provider, body) = await SendAsync(server, HttpMethod.Post, "/api/ai-providers", Json(new { code = ProviderCode, name = "Embedder", vendor = "OpenAI", chatModel = "chat-a", embeddingModel = "embed-a", endpoint = FakeOpenAIHandler.Endpoint, apiKey = "test-key", isDefault = true, maxOutputTokens = 256, temperature = 0.25 }), Platform());
        Assert.True(provider.StatusCode == HttpStatusCode.Created, body.GetRawText());
        return server;
    }

    private static string Platform() => TestTokens.Issue("root", null, Admin);

    private static string Tenant(string tenant) => TestTokens.Issue("root", tenant, Admin);

    private static string Json(object value) => JsonSerializer.Serialize(value);

    private static Task<(HttpResponseMessage Response, JsonElement Body)> SendAsync(BasicAppTestServer server, HttpMethod method, string path, string? json, string token)
        => server.SendAsync(BasicAppTestServer.Request(method, path, json, token));

    private static async Task<JsonElement> IngestAsync(BasicAppTestServer server, string token, string title, string text, string? source = null, string? provider = null)
    {
        var (response, body) = await SendAsync(server, HttpMethod.Post, "/api/knowledge-documents:ingest", Json(new { title, text, source, sourceType = source is null ? "PasteText" : "UploadFile", embeddingProviderCode = provider }), token);
        Assert.True(response.IsSuccessStatusCode, body.GetRawText());
        return body;
    }

    private static async Task<List<JsonElement>> SearchAsync(BasicAppTestServer server, string token, string question, int top = 20)
    {
        var (response, body) = await SendAsync(server, HttpMethod.Get, $"/api/knowledge-documents:search?question={Uri.EscapeDataString(question)}&top={top}", null, token);
        Assert.True(response.IsSuccessStatusCode, body.GetRawText());
        return [.. body.GetProperty("citations").EnumerateArray()];
    }

    private static async Task<Dictionary<string, string>> HealthAsync(BasicAppTestServer server)
    {
        var (_, body) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/health/ready"));
        return body.GetProperty("contributions").EnumerateArray().ToDictionary(static c => c.GetProperty("id").GetString()!, static c => c.GetProperty("state").GetString()!, StringComparer.Ordinal);
    }

    private static async Task<List<string>> ListTitlesAsync(BasicAppTestServer server, string token)
    {
        var (response, body) = await SendAsync(server, HttpMethod.Post, "/api/knowledge-documents:list", """{"page":{"index":1,"size":100}}""", token);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return [.. body.GetProperty("items").EnumerateArray().Select(static item => item.GetProperty("title").GetString()!)];
    }

    [Fact]
    public async Task Ingest_Search_Query_CitationsLineUpWithTheNumbersInTheAnswer()
    {
        await using var server = await StartAsync();
        var token = Platform();

        // 装了向量库，框架的知识库 Provider 自带健康项
        Assert.Contains("ai.vectordata.store", (await HealthAsync(server)).Keys);
        var document = await IngestAsync(server, token, "  Handbook  ", "  The refund window is thirty days.  ", source: "handbook.md");
        var id = document.GetProperty("id").GetString()!;
        Assert.Equal("Indexed", document.GetProperty("status").GetString());
        Assert.Equal(1, document.GetProperty("chunkCount").GetInt32());
        Assert.Equal("Handbook", document.GetProperty("title").GetString());
        Assert.Equal("The refund window is thirty days.", document.GetProperty("rawContent").GetString());
        Assert.Equal("UploadFile", document.GetProperty("sourceType").GetString());
        Assert.Equal(JsonValueKind.Null, document.GetProperty("errorCode").ValueKind);
        await IngestAsync(server, token, "Second", "Shipping takes five working days to arrive.");

        // 列表不带原文，详情带
        var (list, listBody) = await SendAsync(server, HttpMethod.Post, "/api/knowledge-documents:list", """{"page":{"index":1,"size":10},"keyword":"handbook"}""", token);
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        var item = Assert.Single(listBody.GetProperty("items").EnumerateArray());
        Assert.False(item.TryGetProperty("rawContent", out _));
        var (get, getBody) = await SendAsync(server, HttpMethod.Get, $"/api/knowledge-documents/{id}", null, token);
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);
        Assert.Equal("The refund window is thirty days.", getBody.GetProperty("rawContent").GetString());

        // 只检索：片段带文档、块序号、标题、来源、得分与正文
        var hits = await SearchAsync(server, token, "refund");
        Assert.Equal(2, hits.Count);
        var hit = Assert.Single(hits, h => h.GetProperty("documentId").GetString() == id);
        Assert.Equal(0, hit.GetProperty("index").GetInt32());
        Assert.Equal("Handbook", hit.GetProperty("title").GetString());
        Assert.Equal("handbook.md", hit.GetProperty("source").GetString());
        Assert.Equal("The refund window is thirty days.", hit.GetProperty("text").GetString());
        Assert.True(hit.GetProperty("score").GetDouble() > 0);

        // 不要答案：不调聊天模型
        var (bare, bareBody) = await SendAsync(server, HttpMethod.Get, "/api/knowledge-documents:query?question=refund&answer=false", null, token);
        Assert.Equal(HttpStatusCode.OK, bare.StatusCode);
        Assert.Equal(JsonValueKind.Null, bareBody.GetProperty("answer").ValueKind);
        Assert.Equal(2, bareBody.GetProperty("citations").GetArrayLength());
        Assert.DoesNotContain(_model.Requests, static r => r.Path.EndsWith("/chat/completions", StringComparison.Ordinal));

        // 要答案：提示里的 [n] 就是第 n 个片段，最大输出与温度取提供商的配置
        _model.Reply = "Thirty days [1].";
        var (asked, askedBody) = await SendAsync(server, HttpMethod.Get, "/api/knowledge-documents:query?question=refund&top=2", null, token);
        Assert.Equal(HttpStatusCode.OK, asked.StatusCode);
        Assert.Equal("Thirty days [1].", askedBody.GetProperty("answer").GetString());
        var citations = askedBody.GetProperty("citations").EnumerateArray().ToList();
        var request = Assert.Single(_model.Requests, static r => r.Path.EndsWith("/chat/completions", StringComparison.Ordinal)).Body;
        var prompt = request["messages"]!.AsArray().Last()!["content"]!.ToJsonString();
        for (var index = 0; index < citations.Count; index++)
        {
            Assert.Contains($"[{index + 1}] Source: {citations[index].GetProperty("title").GetString()}", prompt, StringComparison.Ordinal);
        }

        Assert.Equal(256, request["max_completion_tokens"]?.GetValue<int>() ?? request["max_tokens"]!.GetValue<int>());
        Assert.Equal(0.25, request["temperature"]!.GetValue<double>(), 3);

        // 没有命中不调模型
        var (none, noneBody) = await SendAsync(server, HttpMethod.Get, "/api/knowledge-documents:query?question=refund", null, Tenant("acme"));
        Assert.Equal(HttpStatusCode.OK, none.StatusCode);
        Assert.Equal(JsonValueKind.Null, noneBody.GetProperty("answer").ValueKind);
        Assert.Equal(0, noneBody.GetProperty("citations").GetArrayLength());

        // 问题长度与条数有上限，全是空白被拒
        var (tooLong, _) = await SendAsync(server, HttpMethod.Get, $"/api/knowledge-documents:search?question={new string('x', 2001)}", null, token);
        Assert.Equal(HttpStatusCode.BadRequest, tooLong.StatusCode);
        var (tooMany, _) = await SendAsync(server, HttpMethod.Get, "/api/knowledge-documents:search?question=a&top=21", null, token);
        Assert.Equal(HttpStatusCode.BadRequest, tooMany.StatusCode);
        var (blank, _) = await SendAsync(server, HttpMethod.Post, "/api/knowledge-documents:ingest", Json(new { title = "   ", text = "x" }), token);
        Assert.Equal(HttpStatusCode.BadRequest, blank.StatusCode);
    }

    [Fact]
    public async Task PlatformAndTenants_CannotSeeEachOthersKnowledge()
    {
        await using var server = await StartAsync();
        var platform = await IngestAsync(server, Platform(), "Platform doc", "Platform only text.");
        var acme = await IngestAsync(server, Tenant("acme"), "Acme doc", "Acme only text.");
        var globex = await IngestAsync(server, Tenant("globex"), "Globex doc", "Globex only text.");
        Assert.All(new[] { platform, acme, globex }, static d => Assert.Equal("Indexed", d.GetProperty("status").GetString()));

        // 行：各看各的
        Assert.Equal(["Platform doc"], await ListTitlesAsync(server, Platform()));
        Assert.Equal(["Acme doc"], await ListTitlesAsync(server, Tenant("acme")));
        Assert.Equal(["Globex doc"], await ListTitlesAsync(server, Tenant("globex")));

        // 向量：检索只在调用方一侧的集合里
        Assert.Equal(["Platform doc"], (await SearchAsync(server, Platform(), "text")).Select(static h => h.GetProperty("title").GetString()!));
        Assert.Equal(["Acme doc"], (await SearchAsync(server, Tenant("acme"), "text")).Select(static h => h.GetProperty("title").GetString()!));
        Assert.Equal(["Globex doc"], (await SearchAsync(server, Tenant("globex"), "text")).Select(static h => h.GetProperty("title").GetString()!));

        // 别的一侧的文档读不到、重建不了、删不掉
        var other = globex.GetProperty("id").GetString()!;
        var (get, getBody) = await SendAsync(server, HttpMethod.Get, $"/api/knowledge-documents/{other}", null, Tenant("acme"));
        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
        Assert.Equal(AIErrors.KnowledgeDocumentNotFound, getBody.GetProperty("code").GetString());
        var (reindex, _) = await SendAsync(server, HttpMethod.Post, $"/api/knowledge-documents/{other}:reindex", null, Tenant("acme"));
        Assert.Equal(HttpStatusCode.NotFound, reindex.StatusCode);
        var (delete, deleteBody) = await SendAsync(server, HttpMethod.Delete, $"/api/knowledge-documents?ids={other}", null, Tenant("acme"));
        Assert.Equal(HttpStatusCode.OK, delete.StatusCode);
        Assert.Equal(AIErrors.KnowledgeDocumentNotFound, Assert.Single(deleteBody.GetProperty("failed").EnumerateArray()).GetProperty("code").GetString());
        Assert.Equal(["Globex doc"], (await SearchAsync(server, Tenant("globex"), "text")).Select(static h => h.GetProperty("title").GetString()!));

        // 只读的角色摄入不了，也检索不了
        var reader = TestTokens.Issue("reader", "acme", Reader);
        Assert.Equal(["Acme doc"], await ListTitlesAsync(server, reader));
        var (ingest, _) = await SendAsync(server, HttpMethod.Post, "/api/knowledge-documents:ingest", Json(new { title = "t", text = "x" }), reader);
        Assert.Equal(HttpStatusCode.Forbidden, ingest.StatusCode);
        var (search, _) = await SendAsync(server, HttpMethod.Get, "/api/knowledge-documents:search?question=a", null, reader);
        Assert.Equal(HttpStatusCode.Forbidden, search.StatusCode);
    }

    [Fact]
    public async Task Reindex_TrimsExtraChunks_AndDelete_RemovesVectors()
    {
        await using var server = await StartAsync();
        var token = Platform();
        var longText = string.Join(' ', Enumerable.Repeat("The quick brown fox jumps over the lazy dog.", 80));
        var document = await IngestAsync(server, token, "Long", longText);
        var id = document.GetProperty("id").GetString()!;
        var chunks = document.GetProperty("chunkCount").GetInt32();
        Assert.True(chunks > 1);
        Assert.Equal(chunks, (await SearchAsync(server, token, "fox")).Count);

        // 原文变短之后重建：多出来的块被裁掉
        await SettingsTestHost.InvokeAsync(server, null, async ctx =>
        {
            await ctx.Services.GetRequiredService<SqlSugarUnitOfWork>().ConnectionFor(typeof(SysKnowledgeDocument)).Updateable<SysKnowledgeDocument>()
                .SetColumns(d => d.RawContent == "Short now.")
                .Where(d => d.Id == Guid.Parse(id))
                .ExecuteCommandAsync(ctx.CancellationToken);
            return 0;
        });
        var (reindexed, body) = await SendAsync(server, HttpMethod.Post, $"/api/knowledge-documents/{id}:reindex", null, token);
        Assert.Equal(HttpStatusCode.OK, reindexed.StatusCode);
        Assert.Equal(1, body.GetProperty("chunkCount").GetInt32());
        var hit = Assert.Single(await SearchAsync(server, token, "fox"));
        Assert.Equal("Short now.", hit.GetProperty("text").GetString());

        // 删除带向量
        var missing = Guid.CreateVersion7();
        var (deleted, deletedBody) = await SendAsync(server, HttpMethod.Delete, $"/api/knowledge-documents?ids={id}&ids={missing}", null, token);
        Assert.Equal(HttpStatusCode.OK, deleted.StatusCode);
        Assert.Equal(id, Assert.Single(deletedBody.GetProperty("succeeded").EnumerateArray()).GetString());
        Assert.Equal(AIErrors.KnowledgeDocumentNotFound, Assert.Single(deletedBody.GetProperty("failed").EnumerateArray()).GetProperty("code").GetString());
        Assert.Empty(await SearchAsync(server, token, "fox"));
        Assert.Empty(await ListTitlesAsync(server, token));
    }

    [Fact]
    public async Task IndexFailures_AreSavedOnTheDocument_AndReindexRecovers()
    {
        await using var server = await StartAsync();
        var token = Platform();

        // 模型不可达：文档以失败保存、成功返回，带码与调用方区域的原因
        _model.Failing = true;
        var failed = await IngestAsync(server, token, "Down", "Model is down.");
        Assert.Equal("Failed", failed.GetProperty("status").GetString());
        Assert.Equal(0, failed.GetProperty("chunkCount").GetInt32());
        Assert.StartsWith("AI_", failed.GetProperty("errorCode").GetString(), StringComparison.Ordinal);
        Assert.False(string.IsNullOrEmpty(failed.GetProperty("errorMessage").GetString()));
        var id = failed.GetProperty("id").GetString()!;

        // 恢复之后重建：状态与原因都清掉
        _model.Failing = false;
        var (recovered, body) = await SendAsync(server, HttpMethod.Post, $"/api/knowledge-documents/{id}:reindex", null, token);
        Assert.Equal(HttpStatusCode.OK, recovered.StatusCode);
        Assert.Equal("Indexed", body.GetProperty("status").GetString());
        Assert.Equal(1, body.GetProperty("chunkCount").GetInt32());
        Assert.Equal(JsonValueKind.Null, body.GetProperty("errorCode").ValueKind);
        Assert.Equal(JsonValueKind.Null, body.GetProperty("errorMessage").ValueKind);

        // 重建失败：旧块照常可检索，块数不变
        _model.Failing = true;
        var (broken, brokenBody) = await SendAsync(server, HttpMethod.Post, $"/api/knowledge-documents/{id}:reindex", null, token);
        Assert.Equal(HttpStatusCode.OK, broken.StatusCode);
        Assert.Equal("Failed", brokenBody.GetProperty("status").GetString());
        Assert.Equal(1, brokenBody.GetProperty("chunkCount").GetInt32());
        _model.Failing = false;
        Assert.Single(await SearchAsync(server, token, "down"));

        // 维度不符：框架的 AI_VALIDATION_002 记在文档上
        _model.Dimensions = FakeOpenAIHandler.DefaultDimensions + 1;
        var mismatch = await IngestAsync(server, token, "Mismatch", "Wrong dimensions.");
        Assert.Equal("Failed", mismatch.GetProperty("status").GetString());
        Assert.Equal("AI_VALIDATION_002", mismatch.GetProperty("errorCode").GetString());
        var (searched, searchedBody) = await SendAsync(server, HttpMethod.Get, "/api/knowledge-documents:search?question=down", null, token);
        Assert.Equal(HttpStatusCode.BadRequest, searched.StatusCode);
        Assert.Equal("AI_VALIDATION_002", searchedBody.GetProperty("code").GetString());
    }

    [Fact]
    public async Task WithoutAVectorStore_DocumentsAreSavedAsFailed_AndSearchIsUnavailable()
    {
        await using var server = await StartAsync(vectorStore: false);
        var token = Platform();
        Assert.DoesNotContain("ai.vectordata.store", (await HealthAsync(server)).Keys);
        var document = await IngestAsync(server, token, "No store", "There is no vector store.");
        Assert.Equal("Failed", document.GetProperty("status").GetString());
        Assert.Equal("AI_UNAVAILABLE_002", document.GetProperty("errorCode").GetString());

        var (search, searchBody) = await SendAsync(server, HttpMethod.Get, "/api/knowledge-documents:search?question=a", null, token);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, search.StatusCode);
        Assert.Equal("AI_UNAVAILABLE_002", searchBody.GetProperty("code").GetString());
        var (query, queryBody) = await SendAsync(server, HttpMethod.Get, "/api/knowledge-documents:query?question=a", null, token);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, query.StatusCode);
        Assert.Equal("AI_UNAVAILABLE_002", queryBody.GetProperty("code").GetString());

        // 没写入过向量的文档照样删得掉
        var id = document.GetProperty("id").GetString()!;
        var (deleted, deletedBody) = await SendAsync(server, HttpMethod.Delete, $"/api/knowledge-documents?ids={id}", null, token);
        Assert.Equal(HttpStatusCode.OK, deleted.StatusCode);
        Assert.Equal(id, Assert.Single(deletedBody.GetProperty("succeeded").EnumerateArray()).GetString());
    }

    [Fact]
    public async Task ProviderInUse_CannotBeDeleted_AndUnknownProvidersAreRejected()
    {
        await using var server = await StartAsync();
        var platform = Platform();
        var (unknown, unknownBody) = await SendAsync(server, HttpMethod.Post, "/api/knowledge-documents:ingest", Json(new { title = "t", text = "x", embeddingProviderCode = "missing" }), platform);
        Assert.Equal(HttpStatusCode.BadRequest, unknown.StatusCode);
        Assert.Equal(AIErrors.KnowledgeProviderUnavailable, unknownBody.GetProperty("code").GetString());

        // 引用可以来自任何一侧：这里是租户的文档，提供商在平台上删
        var document = await IngestAsync(server, Tenant("acme"), "Pinned", "Uses the embedder by name.", provider: ProviderCode);
        Assert.Equal("Indexed", document.GetProperty("status").GetString());
        var (_, list) = await SendAsync(server, HttpMethod.Post, "/api/ai-providers:list", """{"page":{"index":1,"size":10}}""", platform);
        var providerId = Assert.Single(list.GetProperty("items").EnumerateArray()).GetProperty("id").GetString()!;
        var (rejected, rejectedBody) = await SendAsync(server, HttpMethod.Delete, $"/api/ai-providers?ids={providerId}", null, platform);
        Assert.Equal(HttpStatusCode.OK, rejected.StatusCode);
        Assert.Equal(AIErrors.ProviderInUse, Assert.Single(rejectedBody.GetProperty("failed").EnumerateArray()).GetProperty("code").GetString());

        var (removed, _) = await SendAsync(server, HttpMethod.Delete, $"/api/knowledge-documents?ids={document.GetProperty("id").GetString()}", null, Tenant("acme"));
        Assert.Equal(HttpStatusCode.OK, removed.StatusCode);
        var (accepted, acceptedBody) = await SendAsync(server, HttpMethod.Delete, $"/api/ai-providers?ids={providerId}", null, platform);
        Assert.Equal(HttpStatusCode.OK, accepted.StatusCode);
        Assert.Equal(providerId, Assert.Single(acceptedBody.GetProperty("succeeded").EnumerateArray()).GetString());
    }

    [Fact]
    public async Task IsolatedTenant_KeepsItsDocumentsInItsOwnDatabase()
    {
        using var shard = new SqliteDatabase();
        using (var client = new SqlSugarClient(SqlSugarConventions.Apply(Configure(shard.ConnectionString))))
        {
            client.CodeFirst.InitTables<SysKnowledgeDocument>();
        }

        await using var server = await StartAsync(acmeProperties: new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [TenantShardKeys.ConnectionString] = shard.ConnectionString,
            [TenantShardKeys.DatabaseProvider] = "sqlite",
            [TenantSchemas.ReadyProperty] = "true",
        });
        var token = Tenant("acme");

        // 摄入的两次写入（待处理、已索引）都在独立的工作单元里，落进租户自己的库
        var document = await IngestAsync(server, token, "Isolated", "Isolated tenant text.", provider: ProviderCode);
        Assert.Equal("Indexed", document.GetProperty("status").GetString());
        Assert.Equal(1, CountIn(shard.ConnectionString));
        Assert.Equal(0, CountIn(_database.ConnectionString));
        Assert.Equal(["Isolated"], await ListTitlesAsync(server, token));
        Assert.Empty(await ListTitlesAsync(server, Platform()));

        // 引用的核对查到租户自己的库：提供商还不能删
        var (_, list) = await SendAsync(server, HttpMethod.Post, "/api/ai-providers:list", """{"page":{"index":1,"size":10}}""", Platform());
        var providerId = Assert.Single(list.GetProperty("items").EnumerateArray()).GetProperty("id").GetString()!;
        var (rejected, rejectedBody) = await SendAsync(server, HttpMethod.Delete, $"/api/ai-providers?ids={providerId}", null, Platform());
        Assert.Equal(HttpStatusCode.OK, rejected.StatusCode);
        Assert.Equal(AIErrors.ProviderInUse, Assert.Single(rejectedBody.GetProperty("failed").EnumerateArray()).GetProperty("code").GetString());
    }

    private static int CountIn(string connectionString)
    {
        using var client = new SqlSugarClient(SqlSugarConventions.Apply(Configure(connectionString)));
        return client.Queryable<SysKnowledgeDocument>().Where(static d => !d.IsDeleted).Count();
    }

    private static ConnectionConfig Configure(string connectionString)
    {
        var config = new ConnectionConfig();
        SqlSugarRuntime.Configure(Maybe.Some(new BindingValue("db", "connection:relational", connectionString, new Dictionary<string, string>(StringComparer.Ordinal) { ["provider"] = "sqlite" })), config);
        return config;
    }

    [Fact]
    public async Task OnRealQdrant_IngestAndSearch_StayOnTheirOwnSide()
    {
        var endpoint = Environment.GetEnvironmentVariable("XIHAN_TEST_QDRANT");
        Assert.SkipWhen(string.IsNullOrWhiteSpace(endpoint), "XIHAN_TEST_QDRANT is not set.");
        var uri = new Uri(endpoint!);
        await using var server = await StartAsync(settings:
        [
            new("BasicApp:AI:Qdrant:Host", uri.Host),
            new("BasicApp:AI:Qdrant:Port", uri.Port.ToString(System.Globalization.CultureInfo.InvariantCulture)),
            new("BasicApp:AI:Qdrant:Https", (uri.Scheme == Uri.UriSchemeHttps).ToString()),
        ]);

        Assert.Contains("ai.vectordata.store", (await HealthAsync(server)).Keys);

        // 集合名是固定的：用一次性的标记区分这一轮的文档，结束时删掉
        var marker = Guid.CreateVersion7().ToString("N");
        var document = await IngestAsync(server, Platform(), marker, $"Qdrant text {marker}.");
        Assert.True(document.GetProperty("status").GetString() == "Indexed", document.GetRawText());
        var other = await IngestAsync(server, Tenant("acme"), marker + "-acme", $"Acme text {marker}.");
        Assert.True(other.GetProperty("status").GetString() == "Indexed", other.GetRawText());

        var hits = await SearchAsync(server, Platform(), marker);
        Assert.Contains(hits, h => h.GetProperty("title").GetString() == marker);
        Assert.DoesNotContain(hits, h => h.GetProperty("title").GetString() == marker + "-acme");
        var tenantHits = await SearchAsync(server, Tenant("acme"), marker);
        Assert.Contains(tenantHits, h => h.GetProperty("title").GetString() == marker + "-acme");
        Assert.DoesNotContain(tenantHits, h => h.GetProperty("title").GetString() == marker);

        var (deleted, _) = await SendAsync(server, HttpMethod.Delete, $"/api/knowledge-documents?ids={document.GetProperty("id").GetString()}", null, Platform());
        Assert.Equal(HttpStatusCode.OK, deleted.StatusCode);
        Assert.DoesNotContain(await SearchAsync(server, Platform(), marker), h => h.GetProperty("title").GetString() == marker);
        await SendAsync(server, HttpMethod.Delete, $"/api/knowledge-documents?ids={other.GetProperty("id").GetString()}", null, Tenant("acme"));
    }
}
