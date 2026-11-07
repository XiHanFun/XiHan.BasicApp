// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Net;
using System.Text.Json;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.AI;
using XiHan.BasicApp.AI.Domain.Entities;
using XiHan.BasicApp.Integration.Tests.Infrastructure;
using XiHan.Framework.AI.Abstractions.Providers;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;
using XiHan.Framework.Security.Abstractions.Permissions;
using FrameworkAIErrors = XiHan.Framework.AI.Abstractions.AIErrors;

namespace XiHan.BasicApp.Integration.Tests;

/// <summary>
/// 验证 AI 模块的提供商与模块开关
/// </summary>
/// <remarks>
/// 提供商：编码唯一且不可改、API 密钥加密存储且不回显、留空即保留、缺省最多一个、停用的不能设缺省、测试连接探测聊天与嵌入；
/// 落库的 Provider 目录在写入的调用提交之后重载，新的调用即用新配置；只在平台一侧；关掉模块宿主照常启动、页面不下发
/// 模型端点是仿真的 OpenAI 兼容端点（照框架 OpenAI 一致性套件），不触网
/// </remarks>
public sealed class AIProvidersTests : IDisposable
{
    private const string Admin = "ai-admin";

    private readonly SqliteDatabase _database = new();

    private readonly FakeOpenAIHandler _model = new();

    public void Dispose()
    {
        _database.Dispose();
        _model.Dispose();
    }

    private async Task<BasicAppTestServer> StartAsync()
    {
        var server = await BasicAppTestServer.StartAsync(_database.ConnectionString, configure: _model.Attach);
        await server.SeedDefaultsAsync();
        await server.SeedAsync([], [.. AIPermissions.All.Select(static p => new PermissionGrant(GrantSubject.ForRole(Admin), p.Code))]);
        return server;
    }

    private static string Json(object value) => JsonSerializer.Serialize(value);

    private static Task<(HttpResponseMessage Response, JsonElement Body)> SendAsync(BasicAppTestServer server, HttpMethod method, string path, string? json, string token)
        => server.SendAsync(BasicAppTestServer.Request(method, path, json, token));

    private static Task<(HttpResponseMessage Response, JsonElement Body)> CreateAsync(BasicAppTestServer server, string token, string code, string chatModel = "chat-a", bool isDefault = false, string? embeddingModel = "embed-a", string apiKey = "sk-secret-value")
        => SendAsync(server, HttpMethod.Post, "/api/ai-providers", Json(new { code, name = code.ToUpperInvariant(), chatModel, embeddingModel, endpoint = FakeOpenAIHandler.Endpoint, apiKey, isDefault, vendor = "OpenAI" }), token);

    [Fact]
    public async Task AVendorOtherThanOpenAI_MustNameItsEndpoint()
    {
        await using var server = await StartAsync();
        var admin = TestTokens.Issue("root", null, Admin);

        // 厂商只用于分组：端点留空走 OpenAI 的官方地址，别家的密钥就发给了 OpenAI
        var (missing, missingBody) = await SendAsync(server, HttpMethod.Post, "/api/ai-providers", Json(new { code = "deepseek", name = "DeepSeek", chatModel = "m", vendor = "DeepSeek", apiKey = "sk-x" }), admin);
        Assert.Equal(HttpStatusCode.BadRequest, missing.StatusCode);
        Assert.True(missingBody.GetProperty("errors").TryGetProperty("endpoint", out _));

        // OpenAI 可以留空，大小写不敏感；别的厂商写了端点就行
        var (openai, openaiBody) = await SendAsync(server, HttpMethod.Post, "/api/ai-providers", Json(new { code = "openai", name = "OpenAI", chatModel = "m", vendor = "openai" }), admin);
        Assert.True(openai.StatusCode == HttpStatusCode.Created, openaiBody.ToString());
        var (named, namedBody) = await SendAsync(server, HttpMethod.Post, "/api/ai-providers", Json(new { code = "named", name = "Named", chatModel = "m", vendor = "DeepSeek", endpoint = "https://api.deepseek.com" }), admin);
        Assert.True(named.StatusCode == HttpStatusCode.Created, namedBody.ToString());

        // 修改同样：把厂商改掉而端点留空被拒
        var (moved, movedBody) = await SendAsync(server, HttpMethod.Put, $"/api/ai-providers/{openaiBody.GetProperty("id").GetString()}", Json(new { concurrencyStamp = openaiBody.GetProperty("concurrencyStamp").GetString(), name = "OpenAI", chatModel = "m", vendor = "DeepSeek" }), admin);
        Assert.Equal(HttpStatusCode.BadRequest, moved.StatusCode);
        Assert.True(movedBody.GetProperty("errors").TryGetProperty("endpoint", out _));
    }

    [Fact]
    public async Task Providers_FollowTheRulesOf4x_AndKeepKeysOutOfResponses()
    {
        await using var server = await StartAsync();
        var admin = TestTokens.Issue("root", null, Admin);

        // 新建：API 密钥加密存储、响应里只说有没有
        var (created, body) = await CreateAsync(server, admin, "openai-main", isDefault: true);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.True(body.GetProperty("hasApiKey").GetBoolean());
        Assert.DoesNotContain("sk-secret-value", body.GetRawText(), StringComparison.Ordinal);
        Assert.False(body.TryGetProperty("apiKey", out _));
        var main = body.GetProperty("id").GetString()!;
        var stored = await server.InvokeAsync(async ctx =>
        {
            var row = (await ctx.Services.GetRequiredService<IRepository<SysAiProvider, Guid>>().FindAsync(Guid.Parse(main), ctx.CancellationToken)).ValueOrThrow();
            Assert.NotEqual("sk-secret-value", row.ApiKey);
            Assert.DoesNotContain("sk-secret-value", row.ApiKey, StringComparison.Ordinal);
            return Result.Success();
        });
        Assert.True(stored.IsSuccess);

        // 编码唯一、按框架 Provider 名的文法（小写，方案 N4 D1）；端点只认绝对的 http 或 https 地址
        var (taken, takenBody) = await CreateAsync(server, admin, "openai-main");
        Assert.Equal(HttpStatusCode.Conflict, taken.StatusCode);
        Assert.Equal(AIErrors.ProviderCodeTaken, takenBody.GetProperty("code").GetString());
        var (upper, _) = await CreateAsync(server, admin, "OpenAI-Main");
        Assert.Equal(HttpStatusCode.BadRequest, upper.StatusCode);
        var (badEndpoint, badEndpointBody) = await SendAsync(server, HttpMethod.Post, "/api/ai-providers", Json(new { code = "relative", name = "Relative", chatModel = "m", vendor = "OpenAI", endpoint = "/v1" }), admin);
        Assert.Equal(HttpStatusCode.BadRequest, badEndpoint.StatusCode);
        Assert.True(badEndpointBody.GetProperty("errors").TryGetProperty("endpoint", out _));
        var (blankName, blankNameBody) = await SendAsync(server, HttpMethod.Post, "/api/ai-providers", Json(new { code = "blank", name = "   ", chatModel = "m", vendor = "OpenAI" }), admin);
        Assert.Equal(HttpStatusCode.BadRequest, blankName.StatusCode);
        Assert.True(blankNameBody.GetProperty("errors").TryGetProperty("name", out _));

        // 缺省最多一个：新的缺省让旧的取消
        var (_, deepseek) = await CreateAsync(server, admin, "deepseek", isDefault: true);
        var (_, mainAfter) = await SendAsync(server, HttpMethod.Get, $"/api/ai-providers/{main}", null, admin);
        Assert.False(mainAfter.GetProperty("isDefault").GetBoolean());
        Assert.True(deepseek.GetProperty("isDefault").GetBoolean());

        // 列表：关键字、状态多选、缺省排序（排序号再名称）、不支持的筛选
        var (_, list) = await SendAsync(server, HttpMethod.Post, "/api/ai-providers:list", """{"page":{"index":1,"size":20},"filters":[{"field":"status","operator":"in","value":"Active,Disabled"}]}""", admin);
        Assert.Equal(["deepseek", "openai-main"], list.GetProperty("items").EnumerateArray().Select(static i => i.GetProperty("code").GetString()));
        var (_, keyword) = await SendAsync(server, HttpMethod.Post, "/api/ai-providers:list", """{"page":{"index":1,"size":20},"keyword":"main"}""", admin);
        Assert.Equal(1, keyword.GetProperty("total").GetInt32());
        var (badFilter, badFilterBody) = await SendAsync(server, HttpMethod.Post, "/api/ai-providers:list", """{"page":{"index":1,"size":20},"filters":[{"field":"apiKey","operator":"eq","value":"x"}]}""", admin);
        Assert.Equal(HttpStatusCode.BadRequest, badFilter.StatusCode);
        Assert.Equal(AIErrors.QueryUnsupported, badFilterBody.GetProperty("code").GetString());

        // 修改：API 密钥留空即保留；过期的并发戳即冲突
        var (updated, updatedBody) = await SendAsync(server, HttpMethod.Put, $"/api/ai-providers/{main}", Json(new { concurrencyStamp = mainAfter.GetProperty("concurrencyStamp").GetString(), name = "Main", chatModel = "chat-b", vendor = "DeepSeek", endpoint = FakeOpenAIHandler.Endpoint, order = 5 }), admin);
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        Assert.True(updatedBody.GetProperty("hasApiKey").GetBoolean());
        Assert.Equal("chat-b", updatedBody.GetProperty("chatModel").GetString());
        var (stale, _) = await SendAsync(server, HttpMethod.Put, $"/api/ai-providers/{main}", Json(new { concurrencyStamp = mainAfter.GetProperty("concurrencyStamp").GetString(), name = "Main", chatModel = "chat-c", vendor = "OpenAI", endpoint = FakeOpenAIHandler.Endpoint }), admin);
        Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);

        // 改了地址要重新填密钥：留空的旧密钥不能跟着发到新地址
        var (moved, movedBody) = await SendAsync(server, HttpMethod.Put, $"/api/ai-providers/{main}", Json(new { concurrencyStamp = updatedBody.GetProperty("concurrencyStamp").GetString(), name = "Main", chatModel = "chat-b", vendor = "DeepSeek", endpoint = "https://attacker.example/v1", order = 5 }), admin);
        Assert.Equal(HttpStatusCode.BadRequest, moved.StatusCode);
        Assert.Equal("AI_VALIDATION_107", movedBody.GetProperty("code").GetString());

        // 启停带并发戳；停用的不能设为缺省
        var (disabled, disabledBody) = await SendAsync(server, HttpMethod.Post, $"/api/ai-providers/{main}:set-status", Json(new { concurrencyStamp = updatedBody.GetProperty("concurrencyStamp").GetString(), status = "Disabled" }), admin);
        Assert.Equal(HttpStatusCode.OK, disabled.StatusCode);
        Assert.Equal("Disabled", disabledBody.GetProperty("status").GetString());
        var (refused, refusedBody) = await SendAsync(server, HttpMethod.Post, $"/api/ai-providers/{main}:set-default", null, admin);
        Assert.Equal(HttpStatusCode.Conflict, refused.StatusCode);
        Assert.Equal(AIErrors.ProviderDisabled, refusedBody.GetProperty("code").GetString());

        // 删除：批量，不存在的逐项报不存在
        var missing = Guid.NewGuid();
        var (deleted, deletedBody) = await SendAsync(server, HttpMethod.Delete, $"/api/ai-providers?ids={main}&ids={missing}", null, admin);
        Assert.Equal(HttpStatusCode.OK, deleted.StatusCode);
        Assert.Equal([main], deletedBody.GetProperty("succeeded").EnumerateArray().Select(static i => i.GetString()));
        Assert.Equal(AIErrors.ProviderNotFound, deletedBody.GetProperty("failed")[0].GetProperty("code").GetString());

        // 软删除后同编码可以重建
        var (recreated, _) = await CreateAsync(server, admin, "openai-main");
        Assert.Equal(HttpStatusCode.Created, recreated.StatusCode);
    }

    [Fact]
    public async Task Catalog_ServesTheStoredProviders_AndNewCallsUseTheNewSettings()
    {
        await using var server = await StartAsync();
        var admin = TestTokens.Issue("root", null, Admin);
        var catalog = server.Services.GetRequiredService<IAIProviderCatalog>();
        Assert.Empty(catalog.Providers);

        var (_, created) = await CreateAsync(server, admin, "openai-main", chatModel: "chat-a", isDefault: true);
        var id = created.GetProperty("id").GetString()!;

        // 写入的调用提交之后即进目录，缺省提供商就是标为缺省的那一个
        Assert.Equal(["openai-main"], catalog.Providers);
        var options = catalog.Find("openai-main").ValueOrThrow();
        Assert.Equal("chat-a", options.ChatModel);
        Assert.Equal("sk-secret-value", options.ApiKey);
        Assert.Equal(new Uri(FakeOpenAIHandler.Endpoint), options.Endpoint);
        await AskAsync(catalog, null);
        Assert.Equal("chat-a", _model.Requests[^1].Body["model"]!.GetValue<string>());

        // 改了模型，新的调用即用新配置（方案 N4 第一批的退出条件）
        var (_, current) = await SendAsync(server, HttpMethod.Get, $"/api/ai-providers/{id}", null, admin);
        var (updated, updatedBody) = await SendAsync(server, HttpMethod.Put, $"/api/ai-providers/{id}", Json(new { concurrencyStamp = current.GetProperty("concurrencyStamp").GetString(), name = "Main", chatModel = "chat-b", vendor = "OpenAI", endpoint = FakeOpenAIHandler.Endpoint, isDefault = true, timeoutSeconds = 30 }), admin);
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        Assert.Equal(TimeSpan.FromSeconds(30), catalog.Find("openai-main").ValueOrThrow().Timeout);
        await AskAsync(catalog, "openai-main");
        Assert.Equal("chat-b", _model.Requests[^1].Body["model"]!.GetValue<string>());

        // 停用的不在目录里：用到它即框架的 AI_NOTFOUND_001
        var (disabled, _) = await SendAsync(server, HttpMethod.Post, $"/api/ai-providers/{id}:set-status", Json(new { concurrencyStamp = updatedBody.GetProperty("concurrencyStamp").GetString(), status = "Disabled" }), admin);
        Assert.Equal(HttpStatusCode.OK, disabled.StatusCode);
        Assert.Empty(catalog.Providers);
        Assert.Equal(FrameworkAIErrors.ProviderNotFound, catalog.ResolveChatClient("openai-main").Error?.Code);
        Assert.Equal(FrameworkAIErrors.ProviderNotFound, catalog.ResolveChatClient().Error?.Code);

        // 解不开的密钥按未配置处理（fail-closed，不当明文用）
        var (_, other) = await CreateAsync(server, admin, "other");
        var tampered = await server.InvokeAsync(async ctx =>
        {
            var repository = ctx.Services.GetRequiredService<IRepository<SysAiProvider, Guid>>();
            var row = (await repository.FindAsync(Guid.Parse(other.GetProperty("id").GetString()!), ctx.CancellationToken)).ValueOrThrow();
            row.ApiKey = "not-a-protected-value";
            return await repository.UpdateAsync(row, ctx.CancellationToken);
        });
        Assert.True(tampered.IsSuccess);
        await server.Services.GetRequiredService<XiHan.BasicApp.AI.Infrastructure.AiProviderCatalog>().ReloadAsync(CancellationToken.None);
        Assert.False(catalog.Find("other").HasValue);
    }

    [Fact]
    public async Task Test_ProbesTheChatAndEmbeddingModels_AndReportsFailuresInTheResult()
    {
        await using var server = await StartAsync();
        var admin = TestTokens.Issue("root", null, Admin);
        var (_, created) = await CreateAsync(server, admin, "openai-main");
        var id = created.GetProperty("id").GetString()!;

        var (tested, testedBody) = await SendAsync(server, HttpMethod.Post, $"/api/ai-providers/{id}:test", null, admin);
        Assert.Equal(HttpStatusCode.OK, tested.StatusCode);
        Assert.True(testedBody.GetProperty("success").GetBoolean());
        Assert.True(testedBody.GetProperty("chat").GetProperty("success").GetBoolean());
        Assert.Equal("chat-a", testedBody.GetProperty("chat").GetProperty("model").GetString());
        Assert.Equal(8, testedBody.GetProperty("embedding").GetProperty("dimensions").GetInt32());
        var chatRequest = _model.Requests.Single(static r => r.Path.EndsWith("/chat/completions", StringComparison.Ordinal));
        Assert.Equal(16, chatRequest.Body["max_completion_tokens"]?.GetValue<int>() ?? chatRequest.Body["max_tokens"]!.GetValue<int>());

        // 不通不是失败的调用：结果为否、带提供商给的原因
        _model.Failing = true;
        var (failed, failedBody) = await SendAsync(server, HttpMethod.Post, $"/api/ai-providers/{id}:test", null, admin);
        Assert.Equal(HttpStatusCode.OK, failed.StatusCode);
        Assert.False(failedBody.GetProperty("success").GetBoolean());
        Assert.False(failedBody.GetProperty("chat").GetProperty("success").GetBoolean());
        Assert.False(string.IsNullOrWhiteSpace(failedBody.GetProperty("chat").GetProperty("message").GetString()));
        Assert.DoesNotContain("sk-secret-value", failedBody.GetRawText(), StringComparison.Ordinal);
        _model.Failing = false;

        // 没配嵌入模型即不测嵌入
        var (_, chatOnly) = await CreateAsync(server, admin, "chat-only", embeddingModel: null);
        var (_, chatOnlyBody) = await SendAsync(server, HttpMethod.Post, $"/api/ai-providers/{chatOnly.GetProperty("id").GetString()}:test", null, admin);
        Assert.True(chatOnlyBody.GetProperty("success").GetBoolean());
        Assert.Equal(JsonValueKind.Null, chatOnlyBody.GetProperty("embedding").ValueKind);

        // 解不开的密钥报错，不当明文用
        var tampered = await server.InvokeAsync(async ctx =>
        {
            var repository = ctx.Services.GetRequiredService<IRepository<SysAiProvider, Guid>>();
            var row = (await repository.FindAsync(Guid.Parse(id), ctx.CancellationToken)).ValueOrThrow();
            row.ApiKey = "not-a-protected-value";
            return await repository.UpdateAsync(row, ctx.CancellationToken);
        });
        Assert.True(tampered.IsSuccess);
        var (unreadable, unreadableBody) = await SendAsync(server, HttpMethod.Post, $"/api/ai-providers/{id}:test", null, admin);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, unreadable.StatusCode);
        Assert.Equal(AIErrors.ProviderSecretUnreadable, unreadableBody.GetProperty("code").GetString());
    }

    [Fact]
    public async Task WithRedis_ProvidersWrittenOnOneInstance_ReachTheOtherInstance()
    {
        var redis = Environment.GetEnvironmentVariable("XIHAN_TEST_REDIS");
        Assert.SkipWhen(string.IsNullOrWhiteSpace(redis), "XIHAN_TEST_REDIS is not set; this case only runs against a real server.");
        // 两个实例共用一个库、一个 Redis 键前缀：一个实例写入提供商，另一个实例经共享缓存的变更通知重载目录
        KeyValuePair<string, string?>[] settings = [new("XiHan:Bindings:cache:value", redis), new("XiHan:Bindings:queue:value", ""), new("XiHan:Caching:Redis:KeyPrefix", $"xihan-basicapp-test-{Guid.NewGuid():N}:")];
        await using var first = await BasicAppTestServer.StartAsync(_database.ConnectionString, settings: settings, configure: _model.Attach);
        await using var second = await BasicAppTestServer.StartAsync(_database.ConnectionString, settings: settings, configure: _model.Attach);
        await first.SeedDefaultsAsync();
        await first.SeedAsync([], [.. AIPermissions.All.Select(static p => new PermissionGrant(GrantSubject.ForRole(Admin), p.Code))]);
        var remote = second.Services.GetRequiredService<IAIProviderCatalog>();
        Assert.Empty(remote.Providers);

        var (created, _) = await CreateAsync(first, TestTokens.Issue("root", null, Admin), "shared", chatModel: "chat-shared");
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var deadline = DateTimeOffset.UtcNow.AddSeconds(10);
        while (!remote.Find("shared").HasValue && DateTimeOffset.UtcNow < deadline)
        {
            await Task.Delay(100);
        }

        Assert.Equal("chat-shared", remote.Find("shared").ValueOrThrow().ChatModel);
    }

    [Fact]
    public async Task Providers_AreOnlyOnThePlatformSide()
    {
        await using var server = await StartAsync();
        // 租户管理员持 *，平台侧的权限码按作用侧收窄：在授权阶段拒绝
        var tenantAdmin = TestTokens.Issue("alice", "acme", "admin");
        var (list, listBody) = await SendAsync(server, HttpMethod.Post, "/api/ai-providers:list", """{"page":{"index":1,"size":20}}""", tenantAdmin);
        Assert.Equal(HttpStatusCode.Forbidden, list.StatusCode);
        Assert.Equal("SECURITY_DENIED_004", listBody.GetProperty("code").GetString());
    }

    [Fact]
    public async Task AIModule_IsOnByDefault_WithItsPageAndPermissions()
    {
        await using var server = await BasicAppTestServer.StartAsync(_database.ConnectionString);
        await server.SeedDefaultsAsync();
        await server.RunSeedersAsync();
        var superadmin = (await AuthFlowTests.LoginAsync(server, "superadmin", "SuperAdmin@123")).GetProperty("accessToken").GetString()!;
        var (permissions, permissionsBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/api/auth:permissions", token: superadmin));
        Assert.Equal(HttpStatusCode.OK, permissions.StatusCode);
        Assert.Contains("ai.providers", permissionsBody.GetProperty("pages").EnumerateArray().Select(static p => p.GetString()));
        Assert.Contains(AIPermissions.AiProvidersExecute, permissionsBody.GetProperty("permissions").EnumerateArray().Select(static p => p.GetString()));
        var page = MenuTrees.Find(permissionsBody.GetProperty("menus"), "ai.providers");
        Assert.Equal("/ai/providers", page.GetProperty("route").GetString());
        Assert.Equal("ai/providers/index", page.GetProperty("component").GetString());
    }

    [Fact]
    public async Task AIModule_CanBeSwitchedOff()
    {
        await using var server = await BasicAppTestServer.StartAsync(_database.ConnectionString, settings: [new("BasicApp:Modules:AI:Enabled", "false")]);
        await server.SeedDefaultsAsync();
        await server.RunSeedersAsync();
        var superadmin = (await AuthFlowTests.LoginAsync(server, "superadmin", "SuperAdmin@123")).GetProperty("accessToken").GetString()!;
        var (permissions, permissionsBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/api/auth:permissions", token: superadmin));
        Assert.Equal(HttpStatusCode.OK, permissions.StatusCode);
        Assert.DoesNotContain("ai.providers", permissionsBody.GetProperty("pages").EnumerateArray().Select(static p => p.GetString()));
        Assert.DoesNotContain(permissionsBody.GetProperty("permissions").EnumerateArray().Select(static p => p.GetString()), static p => p!.StartsWith("ai.", StringComparison.Ordinal));
        var (list, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/ai-providers:list", """{"page":{"index":1,"size":20}}""", superadmin));
        Assert.Equal(HttpStatusCode.NotFound, list.StatusCode);
        var (docs, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, "/openapi.json"));
        Assert.Equal(HttpStatusCode.OK, docs.StatusCode);

        await Assert.ThrowsAsync<ArgumentException>(() => BasicAppTestServer.StartAsync(_database.ConnectionString, settings: [new("BasicApp:Modules:AI:Enabled", "maybe")]));
    }

    /// <summary>
    /// 经目录解析的聊天客户端问一句
    /// </summary>
    private static async Task AskAsync(IAIProviderCatalog catalog, string? name)
    {
        var client = catalog.ResolveChatClient(name).ValueOrThrow();
        var reply = await client.GetResponseAsync("ping", cancellationToken: CancellationToken.None);
        Assert.Equal("pong", reply.Text);
    }
}
