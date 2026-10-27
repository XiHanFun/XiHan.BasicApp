// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Net;
using System.Text.Json;
using XiHan.BasicApp.Integration.Tests.Infrastructure;

namespace XiHan.BasicApp.Integration.Tests;

/// <summary>
/// 同一份契约作为 MCP tool：<c>tools/list</c> 复用生成的 Schema 与消息目录描述，<c>tools/call</c> 经完整管道、失败按 <c>isError</c>。
/// </summary>
public sealed class UsersMcpTests : IDisposable
{
    private readonly SqliteDatabase _database = new();

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task Tools_AreListed_AndCallable_WithTheSameAuthorization()
    {
        await using var server = await BasicAppTestServer.StartAsync(_database.ConnectionString);
        await server.SeedDefaultsAsync();

        var (list, listBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/mcp", """{"jsonrpc":"2.0","id":1,"method":"tools/list"}""", acceptLanguage: "zh-CN"));
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        var tools = listBody.GetProperty("result").GetProperty("tools").EnumerateArray().ToDictionary(static t => t.GetProperty("name").GetString()!, static t => t);
        Assert.Equal(["users_create", "users_get", "users_list"], tools.Keys.Order(StringComparer.Ordinal));
        Assert.StartsWith("在当前租户创建用户", tools["users_create"].GetProperty("description").GetString(), StringComparison.Ordinal);
        Assert.Equal("object", tools["users_create"].GetProperty("inputSchema").GetProperty("type").GetString());
        Assert.True(tools["users_list"].GetProperty("annotations").GetProperty("readOnlyHint").GetBoolean());

        var call = """{"jsonrpc":"2.0","id":2,"method":"tools/call","params":{"name":"users_create","arguments":{"userName":"mcp.user","displayName":"Via MCP"}}}""";
        var (denied, deniedBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/mcp", call, TestTokens.Issue("bob", "acme", "viewer")));
        Assert.Equal(HttpStatusCode.OK, denied.StatusCode);
        Assert.True(deniedBody.GetProperty("result").GetProperty("isError").GetBoolean());
        Assert.StartsWith("SECURITY_DENIED_004:", deniedBody.GetProperty("result").GetProperty("content")[0].GetProperty("text").GetString(), StringComparison.Ordinal);

        var (created, createdBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/mcp", call, TestTokens.Issue("alice", "acme", "admin")));
        Assert.Equal(HttpStatusCode.OK, created.StatusCode);
        var result = createdBody.GetProperty("result");
        Assert.False(result.TryGetProperty("isError", out var isError) && isError.GetBoolean());
        Assert.Equal("mcp.user", result.GetProperty("structuredContent").GetProperty("userName").GetString());
        Assert.Equal("acme", result.GetProperty("structuredContent").GetProperty("homeTenantId").GetString());

        // 校验失败不是协议错误：isError + "<错误码>: <文案>"
        var invalid = """{"jsonrpc":"2.0","id":3,"method":"tools/call","params":{"name":"users_create","arguments":{"userName":"x"}}}""";
        var (bad, badBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/mcp", invalid, TestTokens.Issue("alice", "acme", "admin")));
        Assert.Equal(HttpStatusCode.OK, bad.StatusCode);
        Assert.True(badBody.GetProperty("result").GetProperty("isError").GetBoolean());
        Assert.StartsWith("KERNEL_VALIDATION_003:", badBody.GetProperty("result").GetProperty("content")[0].GetProperty("text").GetString(), StringComparison.Ordinal);

        var listCall = """{"jsonrpc":"2.0","id":4,"method":"tools/call","params":{"name":"users_list","arguments":{"page":{"index":1,"size":10}}}}""";
        var (listed, listedBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/mcp", listCall, TestTokens.Issue("alice", "acme", "admin")));
        Assert.Equal(HttpStatusCode.OK, listed.StatusCode);
        Assert.Equal(1, listedBody.GetProperty("result").GetProperty("structuredContent").GetProperty("total").GetInt64());
        Assert.Equal(JsonValueKind.Array, listedBody.GetProperty("result").GetProperty("structuredContent").GetProperty("items").ValueKind);
    }
}
