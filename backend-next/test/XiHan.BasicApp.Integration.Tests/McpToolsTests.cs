// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Net;
using System.Text.Json;
using XiHan.BasicApp.Integration.Tests.Infrastructure;

namespace XiHan.BasicApp.Integration.Tests;

/// <summary>
/// 操作表里带 <c>Expose.Mcp</c> 的只读目录类操作作为 MCP tool：<c>tools/list</c> 只列它们，复用生成的 Schema 与消息目录描述，行为提示为只读；
/// <c>tools/call</c> 经完整管道、按与 HTTP 相同的授权判定，失败按 <c>isError</c>。
/// </summary>
public sealed class McpToolsTests : IDisposable
{
    private readonly SqliteDatabase _database = new();

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task DirectoryTools_AreListed_AndCallable_WithTheSameAuthorization()
    {
        await using var server = await BasicAppTestServer.StartAsync(_database.ConnectionString);
        await server.SeedDefaultsAsync();

        var (list, listBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/mcp", """{"jsonrpc":"2.0","id":1,"method":"tools/list"}""", acceptLanguage: "zh-CN"));
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        var tools = listBody.GetProperty("result").GetProperty("tools").EnumerateArray().ToDictionary(static t => t.GetProperty("name").GetString()!, static t => t);
        // 写操作（users_create 之类）不在 MCP 面上
        Assert.Equal(["departments_get", "memberships_list", "permissions_list", "positions_get", "positions_list", "users_get", "users_list"], tools.Keys.Order(StringComparer.Ordinal));
        Assert.StartsWith("分页列出用户", tools["users_list"].GetProperty("description").GetString(), StringComparison.Ordinal);
        Assert.Equal("object", tools["users_get"].GetProperty("inputSchema").GetProperty("type").GetString());
        foreach (var (name, tool) in tools)
        {
            var annotations = tool.GetProperty("annotations");
            Assert.True(annotations.GetProperty("readOnlyHint").GetBoolean(), $"{name} must be announced read-only");
            Assert.False(annotations.GetProperty("destructiveHint").GetBoolean(), $"{name} must not be announced destructive");
        }

        var (created, createdBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/users", """{"userName":"mcp.user","displayName":"Via HTTP"}""", TestTokens.Issue("alice", "acme", "admin")));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var id = createdBody.GetProperty("id").GetString()!;
        var viewer = TestTokens.Issue("bob", "acme", "viewer");

        var listCall = """{"jsonrpc":"2.0","id":2,"method":"tools/call","params":{"name":"users_list","arguments":{"page":{"index":1,"size":10}}}}""";
        var (denied, deniedBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/mcp", listCall, TestTokens.Issue("carol", "acme", "guest")));
        Assert.Equal(HttpStatusCode.OK, denied.StatusCode);
        Assert.True(deniedBody.GetProperty("result").GetProperty("isError").GetBoolean());
        Assert.StartsWith("SECURITY_DENIED_004:", deniedBody.GetProperty("result").GetProperty("content")[0].GetProperty("text").GetString(), StringComparison.Ordinal);

        var (listed, listedBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/mcp", listCall, viewer));
        Assert.Equal(HttpStatusCode.OK, listed.StatusCode);
        Assert.False(listedBody.GetProperty("result").TryGetProperty("isError", out var listError) && listError.GetBoolean());
        Assert.Equal(1, listedBody.GetProperty("result").GetProperty("structuredContent").GetProperty("total").GetInt64());
        Assert.Equal(JsonValueKind.Array, listedBody.GetProperty("result").GetProperty("structuredContent").GetProperty("items").ValueKind);

        var getCall = $$"""{"jsonrpc":"2.0","id":3,"method":"tools/call","params":{"name":"users_get","arguments":{"id":"{{id}}"} } }""";
        var (got, gotBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/mcp", getCall, viewer));
        Assert.Equal(HttpStatusCode.OK, got.StatusCode);
        var result = gotBody.GetProperty("result");
        Assert.False(result.TryGetProperty("isError", out var getError) && getError.GetBoolean());
        Assert.Equal("mcp.user", result.GetProperty("structuredContent").GetProperty("userName").GetString());
        Assert.Equal("acme", result.GetProperty("structuredContent").GetProperty("homeTenantId").GetString());

        // 业务失败与校验失败都不是协议错误：isError + "<错误码>: <文案>"
        var missingCall = $$"""{"jsonrpc":"2.0","id":4,"method":"tools/call","params":{"name":"users_get","arguments":{"id":"{{Guid.CreateVersion7()}}"} } }""";
        var (missing, missingBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/mcp", missingCall, viewer));
        Assert.Equal(HttpStatusCode.OK, missing.StatusCode);
        Assert.True(missingBody.GetProperty("result").GetProperty("isError").GetBoolean());
        Assert.StartsWith("IDENTITY_NOTFOUND_001:", missingBody.GetProperty("result").GetProperty("content")[0].GetProperty("text").GetString(), StringComparison.Ordinal);

        var invalidCall = """{"jsonrpc":"2.0","id":5,"method":"tools/call","params":{"name":"users_list","arguments":{"page":{"index":1,"size":501}}}}""";
        var (bad, badBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/mcp", invalidCall, viewer));
        Assert.Equal(HttpStatusCode.OK, bad.StatusCode);
        Assert.True(badBody.GetProperty("result").GetProperty("isError").GetBoolean());
        Assert.StartsWith("KERNEL_VALIDATION_003:", badBody.GetProperty("result").GetProperty("content")[0].GetProperty("text").GetString(), StringComparison.Ordinal);
    }
}
