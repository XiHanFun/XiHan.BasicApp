// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text;
using XiHan.BasicApp.Contracts.Caching;
using XiHan.BasicApp.Maintenance.Operations.Jobs;
using XiHan.Framework.Caching.Abstractions.Caches;
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
/// <c>GET /api/caches:keys</c>（平台侧）。前缀须落在开放的命名空间里；按续页标记往下翻（Redis 是 SCAN 游标，同一个键可能出现在两页，同一页内去重）。
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

        var page = await scope.Inspector.ListKeysAsync(new CacheKeyQuery { Prefix = request.Prefix, Limit = request.Limit, Continuation = request.Continuation }, context.CancellationToken).ConfigureAwait(false);
        return new CacheKeysResponse([.. page.Keys.Select(static k => k.Value).Distinct(StringComparer.Ordinal)], page.Continuation);
    }
}

/// <summary>
/// 查看一个缓存项。
/// </summary>
/// <remarks>
/// <c>GET /api/caches:inspect</c>（平台侧）。大小、剩余存活与滑动过期取自存储，值按 UTF-8 解出文本（不是合法 UTF-8 即不给文本、也不能改写）。
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
        var info = await scope.Inspector.InspectAsync(key, context.CancellationToken).ConfigureAwait(false);
        var value = await cache.Store.GetAsync(key, context.CancellationToken).ConfigureAwait(false);
        if (!info.TryGetValue(out var entry) || !value.TryGetValue(out var bytes))
        {
            return CacheAccess.NotFound(request.Key);
        }

        var text = CacheAccess.TextOf(bytes);
        return new CacheEntryResponse(request.Key, entry.Size, Seconds(entry.TimeToLive), Seconds(entry.SlidingExpiration), text, scope.Namespace.Editable && text is not null);
    }

    private static long? Seconds(TimeSpan? span) => span is { } value ? (long)Math.Ceiling(value.TotalSeconds) : null;
}

/// <summary>
/// 改写一个缓存项的值。
/// </summary>
/// <remarks>
/// <c>PUT /api/caches</c>（平台侧）。只能改可改写的命名空间里已存在、原值是文本的项；新值按 UTF-8 存，剩余存活或滑动过期沿用原项。
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
        var info = await scope.Inspector.InspectAsync(key, context.CancellationToken).ConfigureAwait(false);
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
        return new CacheEntryResponse(request.Key, value.Length, entry.TimeToLive is { } ttl ? (long)Math.Ceiling(ttl.TotalSeconds) : null, entry.SlidingExpiration is { } s ? (long)Math.Ceiling(s.TotalSeconds) : null, request.Value, true);
    }
}

/// <summary>
/// 批量删除缓存项。
/// </summary>
/// <remarks>
/// <c>DELETE /api/caches</c>（平台侧）。每个键须在开放的命名空间里（不在的逐项报 <c>MAINTENANCE_DENIED_002</c>），只读的命名空间也能删（删了缓存按需重建）；本来就没有的键不算删掉。
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

            if (await cache.Store.RemoveAsync(new CacheKey(key), context.CancellationToken).ConfigureAwait(false))
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
/// <c>POST /api/caches:delete-by-prefix</c>（平台侧）。前缀须落在开放的命名空间里（4.x 按模式删，6.0 按前缀，不支持通配）。
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

        return new CachePrefixDeleteResult(await scope.Inspector.RemoveByPrefixAsync(request.Prefix, context.CancellationToken).ConfigureAwait(false));
    }
}

/// <summary>
/// 缓存管理的公共校验。
/// </summary>
internal static class CacheAccess
{
    /// <summary>
    /// 平台侧、存储支持键管理、键或前缀落在开放的命名空间里。
    /// </summary>
    public static Result<(CacheNamespace Namespace, ICacheStoreInspector Inspector)> Check(InvocationContext context, ISharedCache cache, IEnumerable<CacheNamespace> namespaces, string keyOrPrefix)
    {
        if (JobViews.RequirePlatform(context) is { } denied)
        {
            return denied;
        }

        if (!CacheKey.IsValid(keyOrPrefix) || NamespaceOf(namespaces, keyOrPrefix) is not { } owner)
        {
            return Error.Denied(MaintenanceErrors.CacheNamespaceDenied, MaintenanceMessages.CacheNamespaceDenied, MessageArguments.Of(("key", keyOrPrefix)));
        }

        return cache.Store is ICacheStoreInspector inspector
            ? (owner, inspector)
            : Error.Unavailable(MaintenanceErrors.CacheInspectorUnavailable, MaintenanceMessages.CacheInspectorUnavailable);
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

    private static readonly UTF8Encoding _strict = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
}
