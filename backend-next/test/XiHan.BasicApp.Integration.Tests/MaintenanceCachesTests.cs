// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.Integration.Tests.Infrastructure;
using XiHan.Framework.Caching.Abstractions.Caches;
using XiHan.Framework.Caching.Abstractions.Stores;

namespace XiHan.BasicApp.Integration.Tests;

/// <summary>
/// 验证缓存管理。
/// </summary>
/// <remarks>
/// 只开放各包登记的命名空间；按前缀分页列键（进程内的对象缓存与字节存储两层合起来、标出所在的层）、查看大小或类型与过期、按 UTF-8 或 JSON 看文本；
/// 可改写的命名空间里改字节存储的值沿用过期，鉴权相关的只读、对象缓存的不能改；按键与按前缀删除两层；命名空间之外的键一律拒绝。
/// </remarks>
public sealed class MaintenanceCachesTests : IDisposable
{
    private readonly SqliteDatabase _database = new();

    /// <summary>
    /// 写进对象缓存的值。
    /// </summary>
    /// <param name="Name">名称。</param>
    /// <param name="Count">个数。</param>
    private sealed record Snapshot(string Name, int Count);

    public void Dispose() => _database.Dispose();

    private static async Task<(HttpStatusCode Status, JsonElement Body)> SendAsync(BasicAppTestServer server, HttpMethod method, string path, object? body, string? token)
    {
        var (response, json) = await server.SendAsync(BasicAppTestServer.Request(method, path, body is null ? null : JsonSerializer.Serialize(body), token));
        return (response.StatusCode, json);
    }

    [Fact]
    public async Task Caches_AreBrowsedRewrittenAndDeleted_WithinTheApplicationNamespaces()
    {
        var (server, platform, _) = await SettingsTestHost.StartAsync(_database.ConnectionString);
        await using var running = server;
        var cache = server.Services.GetRequiredService<ISharedCache>();
        var store = cache.Store;
        await cache.SetAsync(new CacheKey("notification:test:obj"), new Snapshot("demo", 3), CacheEntryOptions.Expires(TimeSpan.FromHours(1)), CancellationToken.None);
        await cache.SetAsync(new CacheKey("notification:test:obj2"), new Snapshot("other", 1), CacheEntryOptions.Default, CancellationToken.None);
        await store.SetAsync(new CacheKey("notification:test:a"), Encoding.UTF8.GetBytes("hello"), CacheEntryOptions.Expires(TimeSpan.FromHours(1)), CancellationToken.None);
        await store.SetAsync(new CacheKey("notification:test:b"), Encoding.UTF8.GetBytes("world"), CacheEntryOptions.Default, CancellationToken.None);
        await store.SetAsync(new CacheKey("notification:test:bin"), new byte[] { 0xff, 0xfe, 0x00 }, CacheEntryOptions.Default, CancellationToken.None);
        await store.SetAsync(new CacheKey("access:test:x"), Encoding.UTF8.GetBytes("rule"), CacheEntryOptions.Default, CancellationToken.None);
        await store.SetAsync(new CacheKey("lock:test"), Encoding.UTF8.GetBytes("framework"), CacheEntryOptions.Default, CancellationToken.None);

        // 命名空间：各包登记的前缀，鉴权相关的只读
        var (_, namespaces) = await SendAsync(server, HttpMethod.Get, "/api/caches:namespaces", null, platform);
        var registered = namespaces.GetProperty("items").EnumerateArray().ToDictionary(static n => n.GetProperty("prefix").GetString()!, static n => n.GetProperty("editable").GetBoolean());
        Assert.False(registered["access:"]);
        Assert.True(registered["notification:"]);
        Assert.DoesNotContain("lock:", registered.Keys);

        // 列键：两层合起来分页（先对象缓存、再字节存储），每个键标出所在的层；续页标记认不出与命名空间之外的前缀拒绝
        var (_, firstPage) = await SendAsync(server, HttpMethod.Get, "/api/caches:keys?prefix=notification:test:&limit=3", null, platform);
        var keys = firstPage.GetProperty("items").EnumerateArray().Select(static k => (k.GetProperty("key").GetString()!, k.GetProperty("layer").GetString()!)).ToList();
        Assert.Equal([("notification:test:obj", "Objects"), ("notification:test:obj2", "Objects")], keys.Take(2));
        var continuation = firstPage.GetProperty("continuation").GetString();
        while (continuation is not null)
        {
            var (_, next) = await SendAsync(server, HttpMethod.Get, $"/api/caches:keys?prefix=notification:test:&limit=3&continuation={Uri.EscapeDataString(continuation)}", null, platform);
            keys.AddRange(next.GetProperty("items").EnumerateArray().Select(static k => (k.GetProperty("key").GetString()!, k.GetProperty("layer").GetString()!)));
            continuation = next.GetProperty("continuation").ValueKind == JsonValueKind.String ? next.GetProperty("continuation").GetString() : null;
        }

        Assert.Equal(
            [("notification:test:a", "Store"), ("notification:test:b", "Store"), ("notification:test:bin", "Store"), ("notification:test:obj", "Objects"), ("notification:test:obj2", "Objects")],
            keys.Distinct().OrderBy(static k => k.Item1, StringComparer.Ordinal));
        var (badCursor, badCursorBody) = await SendAsync(server, HttpMethod.Get, "/api/caches:keys?prefix=notification:test:&continuation=x:1", null, platform);
        Assert.Equal((HttpStatusCode.BadRequest, "MAINTENANCE_VALIDATION_006"), (badCursor, badCursorBody.GetProperty("code").GetString()));
        var (outside, outsideBody) = await SendAsync(server, HttpMethod.Get, "/api/caches:keys?prefix=lock:", null, platform);
        Assert.Equal((HttpStatusCode.Forbidden, "MAINTENANCE_DENIED_002"), (outside, outsideBody.GetProperty("code").GetString()));

        // 查看：大小、剩余存活、文本；不是 UTF-8 的不给文本也不能改
        var (_, entry) = await SendAsync(server, HttpMethod.Get, "/api/caches:inspect?key=notification:test:a", null, platform);
        Assert.Equal(("hello", 5L, true), (entry.GetProperty("text").GetString(), entry.GetProperty("size").GetInt64(), entry.GetProperty("editable").GetBoolean()));
        Assert.InRange(entry.GetProperty("timeToLiveSeconds").GetInt64(), 1, 3600);
        Assert.Equal(("Store", JsonValueKind.Null), (entry.GetProperty("layer").GetString(), entry.GetProperty("typeName").ValueKind));
        var (_, objectEntry) = await SendAsync(server, HttpMethod.Get, "/api/caches:inspect?key=notification:test:obj", null, platform);
        Assert.Equal(("Objects", JsonValueKind.Null, false), (objectEntry.GetProperty("layer").GetString(), objectEntry.GetProperty("size").ValueKind, objectEntry.GetProperty("editable").GetBoolean()));
        Assert.EndsWith("+Snapshot", objectEntry.GetProperty("typeName").GetString(), StringComparison.Ordinal);
        Assert.Equal("""{"name":"demo","count":3}""", objectEntry.GetProperty("text").GetString());
        Assert.InRange(objectEntry.GetProperty("timeToLiveSeconds").GetInt64(), 1, 3600);
        var (_, binary) = await SendAsync(server, HttpMethod.Get, "/api/caches:inspect?key=notification:test:bin", null, platform);
        Assert.Equal((JsonValueKind.Null, false), (binary.GetProperty("text").ValueKind, binary.GetProperty("editable").GetBoolean()));
        Assert.Equal("MAINTENANCE_NOTFOUND_003", (await SendAsync(server, HttpMethod.Get, "/api/caches:inspect?key=notification:test:none", null, platform)).Body.GetProperty("code").GetString());

        // 改写：沿用过期；只读命名空间、非文本、命名空间之外的键各自拒绝
        var (updated, updatedBody) = await SendAsync(server, HttpMethod.Put, "/api/caches", new { key = "notification:test:a", value = "hi" }, platform);
        Assert.Equal(HttpStatusCode.OK, updated);
        Assert.Equal("hi", updatedBody.GetProperty("text").GetString());
        Assert.True((await store.GetAsync(new CacheKey("notification:test:a"), CancellationToken.None)).TryGetValue(out var stored));
        Assert.Equal("hi", Encoding.UTF8.GetString(stored.Span));
        Assert.Equal("MAINTENANCE_DENIED_003", (await SendAsync(server, HttpMethod.Put, "/api/caches", new { key = "access:test:x", value = "y" }, platform)).Body.GetProperty("code").GetString());
        Assert.Equal("MAINTENANCE_VALIDATION_003", (await SendAsync(server, HttpMethod.Put, "/api/caches", new { key = "notification:test:bin", value = "y" }, platform)).Body.GetProperty("code").GetString());
        Assert.Equal("MAINTENANCE_VALIDATION_003", (await SendAsync(server, HttpMethod.Put, "/api/caches", new { key = "notification:test:obj", value = "y" }, platform)).Body.GetProperty("code").GetString());
        Assert.Equal("MAINTENANCE_DENIED_002", (await SendAsync(server, HttpMethod.Put, "/api/caches", new { key = "lock:test", value = "y" }, platform)).Body.GetProperty("code").GetString());

        // 删除：按键（只读命名空间也能删，之外的逐项拒绝），按前缀
        var (deleted, deletedBody) = await SendAsync(server, HttpMethod.Delete, "/api/caches?keys=access:test:x&keys=lock:test&keys=notification:test:none&keys=notification:test:obj", null, platform);
        Assert.Equal(HttpStatusCode.OK, deleted);
        Assert.Equal(["access:test:x", "notification:test:obj"], deletedBody.GetProperty("removed").EnumerateArray().Select(static k => k.GetString()).ToList());
        Assert.False((await cache.GetAsync<Snapshot>(new CacheKey("notification:test:obj"), CancellationToken.None)).HasValue);
        Assert.Equal("MAINTENANCE_DENIED_002", deletedBody.GetProperty("failed")[0].GetProperty("code").GetString());
        Assert.True((await store.GetAsync(new CacheKey("lock:test"), CancellationToken.None)).HasValue);
        var (byPrefix, byPrefixBody) = await SendAsync(server, HttpMethod.Post, "/api/caches:delete-by-prefix", new { prefix = "notification:test:" }, platform);
        Assert.Equal(HttpStatusCode.OK, byPrefix);
        Assert.Equal(4, byPrefixBody.GetProperty("removed").GetInt64());
        Assert.False((await cache.GetAsync<Snapshot>(new CacheKey("notification:test:obj2"), CancellationToken.None)).HasValue);
        Assert.Equal("MAINTENANCE_DENIED_002", (await SendAsync(server, HttpMethod.Post, "/api/caches:delete-by-prefix", new { prefix = "lock:" }, platform)).Body.GetProperty("code").GetString());

        // 没登录
        Assert.Equal(HttpStatusCode.Unauthorized, (await SendAsync(server, HttpMethod.Get, "/api/caches:namespaces", null, null)).Status);
    }
}
