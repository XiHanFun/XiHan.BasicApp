// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text;
using System.Text.Json;
using XiHan.BasicApp.Contracts.Caching;
using XiHan.BasicApp.Maintenance.Operations.Jobs;
using XiHan.Framework.Caching.Abstractions.Caches;
using XiHan.Framework.Caching.Abstractions.Serialization;
using XiHan.Framework.Caching.Abstractions.Stores;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Maintenance.Operations.Caches;

/// <summary>
/// 列出开放的缓存键命名空间。
/// </summary>
/// <remarks>
/// <c>GET /api/caches:namespaces</c>（平台侧）。只有各包登记过的命名空间（<see cref="CacheNamespace"/>），框架自己的键不开放。
/// </remarks>
/// <param name="namespaces">各包登记的命名空间。</param>
[Operation(Id, Stability = Stability.Preview, ReadOnly = true, Expose = Expose.Http, Permission = MaintenancePermissions.CachesRead)]
public sealed class ListCacheNamespaces(IEnumerable<CacheNamespace> namespaces) : IOperation<CacheNamespacesRequest, CacheNamespacesResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "caches.namespaces";

    /// <inheritdoc />
    public ValueTask<Result<CacheNamespacesResponse>> HandleAsync(CacheNamespacesRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        if (JobViews.RequirePlatform(context) is { } denied)
        {
            return ValueTask.FromResult(Result.Failure<CacheNamespacesResponse>(denied));
        }

        var items = namespaces.OrderBy(static n => n.Prefix, StringComparer.Ordinal).Select(static n => new CacheNamespaceResponse(n.Package, n.Prefix, n.Editable)).ToList();
        return ValueTask.FromResult(Result.Success(new CacheNamespacesResponse(items)));
    }
}

/// <summary>
/// 按前缀分页列出缓存键。
/// </summary>
/// <remarks>
/// <c>GET /api/caches:keys</c>（平台侧）。前缀须落在开放的命名空间里；两层合起来列：先对象缓存、再字节存储，对象缓存列完而本页没满时接着从字节存储取，每个键标出所在的层。
/// 按续页标记往下翻（标记里带着所在的层，认不出即 <c>MAINTENANCE_VALIDATION_006</c>；Redis 是 SCAN 游标，同一个键可能出现在两页，同一页内去重）。
/// </remarks>
/// <param name="cache">共享缓存。</param>
/// <param name="namespaces">各包登记的命名空间。</param>
[Operation(Id, Stability = Stability.Preview, ReadOnly = true, Expose = Expose.Http, Permission = MaintenancePermissions.CachesRead)]
public sealed class ListCacheKeys(ISharedCache cache, IEnumerable<CacheNamespace> namespaces) : IOperation<CacheKeysRequest, CacheKeysResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "caches.keys";

    private const string ObjectCursor = "o:";
    private const string StoreCursor = "s:";

    /// <inheritdoc />
    public async ValueTask<Result<CacheKeysResponse>> HandleAsync(CacheKeysRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var checkedScope = CacheAccess.Check(context, cache, namespaces, request.Prefix);
        if (!checkedScope.TryGetValue(out var scope))
        {
            return checkedScope.Error!;
        }

        CacheLayer layer;
        string? cursor;
        if (request.Continuation is not { } continuation)
        {
            layer = scope.Objects is null ? CacheLayer.Store : CacheLayer.Objects;
            cursor = null;
        }
        else if (continuation.StartsWith(ObjectCursor, StringComparison.Ordinal) && scope.Objects is not null)
        {
            layer = CacheLayer.Objects;
            cursor = continuation[ObjectCursor.Length..];
        }
        else if (continuation.StartsWith(StoreCursor, StringComparison.Ordinal) && scope.Store is not null)
        {
            layer = CacheLayer.Store;

            // 对象缓存列完时给的是只有层的标记：字节存储从头列
            cursor = continuation.Length == StoreCursor.Length ? null : continuation[StoreCursor.Length..];
        }
        else
        {
            return Error.Validation(MaintenanceErrors.CacheContinuationInvalid, MaintenanceMessages.CacheContinuationInvalid);
        }

        var items = new List<CacheKeyItem>();
        var limit = request.Limit;
        if (layer == CacheLayer.Objects)
        {
            var page = await scope.Objects!.ListKeysAsync(new CacheKeyQuery { Prefix = request.Prefix, Limit = limit, Continuation = cursor }, context.CancellationToken).ConfigureAwait(false);
            items.AddRange(page.Keys.Select(static k => new CacheKeyItem(k.Value, CacheLayer.Objects)));
            if (page.Continuation is { } next)
            {
                return Page(items, ObjectCursor + next);
            }

            if (scope.Store is null)
            {
                return Page(items, null);
            }

            if (items.Count >= limit)
            {
                return Page(items, StoreCursor);
            }

            limit -= items.Count;
            cursor = null;
        }

        var stored = await scope.Store!.ListKeysAsync(new CacheKeyQuery { Prefix = request.Prefix, Limit = limit, Continuation = cursor }, context.CancellationToken).ConfigureAwait(false);
        items.AddRange(stored.Keys.Select(static k => new CacheKeyItem(k.Value, CacheLayer.Store)));
        return Page(items, stored.Continuation is { } more ? StoreCursor + more : null);
    }

    private static CacheKeysResponse Page(List<CacheKeyItem> items, string? continuation) => new([.. items.Distinct()], continuation);
}

/// <summary>
/// 查看一个缓存项。
/// </summary>
/// <remarks>
/// <c>GET /api/caches:inspect</c>（平台侧）。先找对象缓存：给出值的类型、剩余存活与滑动过期，值按 JSON 写出（写不出即不给文本），不能改写；
/// 再找字节存储：大小、剩余存活与滑动过期取自存储，值按 UTF-8 解出文本（不是合法 UTF-8 即不给文本、也不能改写）。
/// </remarks>
/// <param name="cache">共享缓存。</param>
/// <param name="namespaces">各包登记的命名空间。</param>
[Operation(Id, Stability = Stability.Preview, ReadOnly = true, Expose = Expose.Http, Permission = MaintenancePermissions.CachesRead)]
public sealed class InspectCacheEntry(ISharedCache cache, IEnumerable<CacheNamespace> namespaces) : IOperation<CacheKeyRequest, CacheEntryResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "caches.inspect";

    /// <inheritdoc />
    public async ValueTask<Result<CacheEntryResponse>> HandleAsync(CacheKeyRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var checkedScope = CacheAccess.Check(context, cache, namespaces, request.Key);
        if (!checkedScope.TryGetValue(out var scope))
        {
            return checkedScope.Error!;
        }

        var key = new CacheKey(request.Key);
        if (scope.Objects is { } objects && (await objects.InspectAsync(key, context.CancellationToken).ConfigureAwait(false)).TryGetValue(out var info))
        {
            var value = await cache.GetAsync<object>(key, context.CancellationToken).ConfigureAwait(false);
            var json = value.TryGetValue(out var item) ? CacheAccess.JsonOf(item) : null;
            return new CacheEntryResponse(request.Key, CacheLayer.Objects, info.TypeName, null, CacheAccess.Seconds(info.TimeToLive), CacheAccess.Seconds(info.SlidingExpiration), json, false);
        }

        if (scope.Store is not { } store)
        {
            return CacheAccess.NotFound(request.Key);
        }

        var entry = await store.InspectAsync(key, context.CancellationToken).ConfigureAwait(false);
        var bytes = await cache.Store.GetAsync(key, context.CancellationToken).ConfigureAwait(false);
        if (!entry.TryGetValue(out var stored) || !bytes.TryGetValue(out var payload))
        {
            return CacheAccess.NotFound(request.Key);
        }

        var text = CacheAccess.TextOf(payload);
        return new CacheEntryResponse(request.Key, CacheLayer.Store, null, stored.Size, CacheAccess.Seconds(stored.TimeToLive), CacheAccess.Seconds(stored.SlidingExpiration), text, scope.Namespace.Editable && text is not null);
    }
}

/// <summary>
/// 改写一个缓存项的值。
/// </summary>
/// <remarks>
/// <c>PUT /api/caches</c>（平台侧）。只能改可改写的命名空间里、字节存储中已存在且原值是文本的项（对象缓存的值不是文本，<c>MAINTENANCE_VALIDATION_003</c>）；新值按 UTF-8 存，剩余存活或滑动过期沿用原项。
/// </remarks>
/// <param name="cache">共享缓存。</param>
/// <param name="namespaces">各包登记的命名空间。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = MaintenancePermissions.CachesUpdate)]
public sealed class UpdateCacheEntry(ISharedCache cache, IEnumerable<CacheNamespace> namespaces) : IOperation<UpdateCacheRequest, CacheEntryResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "caches.update";

    /// <inheritdoc />
    public async ValueTask<Result<CacheEntryResponse>> HandleAsync(UpdateCacheRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var checkedScope = CacheAccess.Check(context, cache, namespaces, request.Key);
        if (!checkedScope.TryGetValue(out var scope))
        {
            return checkedScope.Error!;
        }

        if (!scope.Namespace.Editable)
        {
            return Error.Denied(MaintenanceErrors.CacheReadOnly, MaintenanceMessages.CacheReadOnly, MessageArguments.Of(("prefix", scope.Namespace.Prefix)));
        }

        var key = new CacheKey(request.Key);
        if (scope.Objects is { } objects && (await objects.InspectAsync(key, context.CancellationToken).ConfigureAwait(false)).HasValue)
        {
            return Error.Validation(MaintenanceErrors.CacheValueNotText, MaintenanceMessages.CacheValueNotText, MessageArguments.Of(("key", request.Key)));
        }

        if (scope.Store is not { } store)
        {
            return CacheAccess.NotFound(request.Key);
        }

        var info = await store.InspectAsync(key, context.CancellationToken).ConfigureAwait(false);
        var current = await cache.Store.GetAsync(key, context.CancellationToken).ConfigureAwait(false);
        if (!info.TryGetValue(out var entry) || !current.TryGetValue(out var bytes))
        {
            return CacheAccess.NotFound(request.Key);
        }

        if (CacheAccess.TextOf(bytes) is null)
        {
            return Error.Validation(MaintenanceErrors.CacheValueNotText, MaintenanceMessages.CacheValueNotText, MessageArguments.Of(("key", request.Key)));
        }

        var options = entry.SlidingExpiration is { } sliding ? CacheEntryOptions.Sliding(sliding)
            : entry.TimeToLive is { } remaining ? CacheEntryOptions.Expires(remaining)
            : CacheEntryOptions.Default;
        var value = Encoding.UTF8.GetBytes(request.Value);
        await cache.Store.SetAsync(key, value, options, context.CancellationToken).ConfigureAwait(false);
        return new CacheEntryResponse(request.Key, CacheLayer.Store, null, value.Length, CacheAccess.Seconds(entry.TimeToLive), CacheAccess.Seconds(entry.SlidingExpiration), request.Value, true);
    }
}

/// <summary>
/// 批量删除缓存项。
/// </summary>
/// <remarks>
/// <c>DELETE /api/caches</c>（平台侧）。每个键须在开放的命名空间里（不在的逐项报 <c>MAINTENANCE_DENIED_002</c>），只读的命名空间也能删（删了缓存按需重建）；
/// 对象缓存与字节存储里的都删，本来就没有的键不算删掉。
/// </remarks>
/// <param name="cache">共享缓存。</param>
/// <param name="namespaces">各包登记的命名空间。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = MaintenancePermissions.CachesDelete)]
public sealed class DeleteCacheEntries(ISharedCache cache, IEnumerable<CacheNamespace> namespaces) : IOperation<CacheKeysDeleteRequest, CacheDeleteResult>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "caches.delete";

    /// <inheritdoc />
    public async ValueTask<Result<CacheDeleteResult>> HandleAsync(CacheKeysDeleteRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        if (JobViews.RequirePlatform(context) is { } denied)
        {
            return denied;
        }

        var removed = new List<string>();
        var failed = new List<CacheDeleteFailure>();
        foreach (var key in request.Keys.Distinct(StringComparer.Ordinal))
        {
            if (!CacheKey.IsValid(key) || CacheAccess.NamespaceOf(namespaces, key) is null)
            {
                failed.Add(new CacheDeleteFailure(key, MaintenanceErrors.CacheNamespaceDenied));
                continue;
            }

            // 对象缓存的条目经缓存本身删除（ADR-0058），字节存储的经存储删除；序列化缓存两者是同一处，第二次删不到
            var cacheKey = new CacheKey(key);
            var fromObjects = cache is ICacheInspector && await cache.RemoveAsync(cacheKey, context.CancellationToken).ConfigureAwait(false);
            var fromStore = await cache.Store.RemoveAsync(cacheKey, context.CancellationToken).ConfigureAwait(false);
            if (fromObjects || fromStore)
            {
                removed.Add(key);
            }
        }

        return new CacheDeleteResult(removed, failed);
    }
}

/// <summary>
/// 按前缀删除缓存项。
/// </summary>
/// <remarks>
/// <c>POST /api/caches:delete-by-prefix</c>（平台侧）。前缀须落在开放的命名空间里（4.x 按模式删，6.0 按前缀，不支持通配）；对象缓存与字节存储里的都删，删除数是两层之和。
/// </remarks>
/// <param name="cache">共享缓存。</param>
/// <param name="namespaces">各包登记的命名空间。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = MaintenancePermissions.CachesDelete)]
public sealed class DeleteCacheByPrefix(ISharedCache cache, IEnumerable<CacheNamespace> namespaces) : IOperation<CachePrefixRequest, CachePrefixDeleteResult>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "caches.delete-by-prefix";

    /// <inheritdoc />
    public async ValueTask<Result<CachePrefixDeleteResult>> HandleAsync(CachePrefixRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var checkedScope = CacheAccess.Check(context, cache, namespaces, request.Prefix);
        if (!checkedScope.TryGetValue(out var scope))
        {
            return checkedScope.Error!;
        }

        var removed = 0L;
        if (scope.Objects is { } objects)
        {
            removed += await objects.RemoveByPrefixAsync(request.Prefix, context.CancellationToken).ConfigureAwait(false);
        }

        if (scope.Store is { } store)
        {
            removed += await store.RemoveByPrefixAsync(request.Prefix, context.CancellationToken).ConfigureAwait(false);
        }

        return new CachePrefixDeleteResult(removed);
    }
}

/// <summary>
/// 缓存管理能看到的两层。
/// </summary>
/// <param name="Namespace">键或前缀所在的开放命名空间。</param>
/// <param name="Objects">对象缓存的键管理；序列化缓存没有。</param>
/// <param name="Store">字节存储的键管理；存储不支持时没有。</param>
internal sealed record CacheScope(CacheNamespace Namespace, ICacheInspector? Objects, ICacheStoreInspector? Store);

/// <summary>
/// 缓存管理的公共校验。
/// </summary>
internal static class CacheAccess
{
    /// <summary>
    /// 平台侧、至少一层支持键管理、键或前缀落在开放的命名空间里。
    /// </summary>
    public static Result<CacheScope> Check(InvocationContext context, ISharedCache cache, IEnumerable<CacheNamespace> namespaces, string keyOrPrefix)
    {
        if (JobViews.RequirePlatform(context) is { } denied)
        {
            return denied;
        }

        if (!CacheKey.IsValid(keyOrPrefix) || NamespaceOf(namespaces, keyOrPrefix) is not { } owner)
        {
            return Error.Denied(MaintenanceErrors.CacheNamespaceDenied, MaintenanceMessages.CacheNamespaceDenied, MessageArguments.Of(("key", keyOrPrefix)));
        }

        var objects = cache as ICacheInspector;
        var store = cache.Store as ICacheStoreInspector;
        return objects is null && store is null
            ? Error.Unavailable(MaintenanceErrors.CacheInspectorUnavailable, MaintenanceMessages.CacheInspectorUnavailable)
            : new CacheScope(owner, objects, store);
    }

    /// <summary>
    /// 键或前缀所在的开放命名空间。
    /// </summary>
    public static CacheNamespace? NamespaceOf(IEnumerable<CacheNamespace> namespaces, string keyOrPrefix)
        => namespaces.FirstOrDefault(n => keyOrPrefix.StartsWith(n.Prefix, StringComparison.Ordinal));

    /// <summary>
    /// 缓存项不存在。
    /// </summary>
    public static Error NotFound(string key) => Error.NotFound(MaintenanceErrors.CacheEntryNotFound, MaintenanceMessages.CacheEntryNotFound, MessageArguments.Of(("key", key)));

    /// <summary>
    /// 时长的整秒数。
    /// </summary>
    /// <remarks>
    /// 不足一秒按一秒，没有为空。
    /// </remarks>
    public static long? Seconds(TimeSpan? span) => span is { } value ? (long)Math.Ceiling(value.TotalSeconds) : null;

    /// <summary>
    /// 载荷按 UTF-8 解出的文本。
    /// </summary>
    /// <remarks>
    /// 不是合法 UTF-8 为空。
    /// </remarks>
    public static string? TextOf(ReadOnlyMemory<byte> bytes)
    {
        try
        {
            return _strict.GetString(bytes.Span);
        }
        catch (DecoderFallbackException)
        {
            return null;
        }
    }

    /// <summary>
    /// 对象缓存的值按 JSON 写出的文本。
    /// </summary>
    /// <remarks>
    /// 与序列化缓存（Redis）写进存储的同一种写法（反射、Web 缺省）；类型写不出 JSON（如有环引用）时为空。
    /// </remarks>
    public static string? JsonOf(object value)
    {
        try
        {
            return Encoding.UTF8.GetString(_json.Serialize(value).Span);
        }
        catch (Exception exception) when (exception is NotSupportedException or JsonException or InvalidOperationException)
        {
            return null;
        }
    }

    private static readonly UTF8Encoding _strict = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    private static readonly JsonCacheSerializer _json = JsonCacheSerializer.CreateReflectionBased();
}
