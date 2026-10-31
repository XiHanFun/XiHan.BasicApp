// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using XiHan.Framework.Caching.Abstractions.Stores;

namespace XiHan.BasicApp.Maintenance.Operations.Caches;

/// <summary>
/// <c>caches.namespaces</c> 的请求（没有参数）。
/// </summary>
public sealed record CacheNamespacesRequest;

/// <summary>
/// 一个开放的缓存键命名空间。
/// </summary>
/// <param name="Package">登记它的包。</param>
/// <param name="Prefix">键前缀，如 <c>access:</c>。</param>
/// <param name="Editable">能否改写值（鉴权相关的只许删）。</param>
public sealed record CacheNamespaceResponse(string Package, string Prefix, bool Editable);

/// <summary>
/// 开放的缓存键命名空间。
/// </summary>
/// <param name="Items">命名空间，按前缀排序。</param>
public sealed record CacheNamespacesResponse(IReadOnlyList<CacheNamespaceResponse> Items);

/// <summary>
/// 缓存条目所在的层。
/// </summary>
/// <remarks>
/// 进程内缓存把对象引用存在自己的表里（对象缓存），锁、幂等等组件与序列化缓存（Redis）的条目在字节存储里；两层合起来是全部条目。
/// </remarks>
public enum CacheLayer
{
    /// <summary>
    /// 字节存储。
    /// </summary>
    Store = 0,

    /// <summary>
    /// 进程内的对象缓存。
    /// </summary>
    Objects = 1,
}

/// <summary>
/// <c>caches.keys</c> 的请求。
/// </summary>
/// <param name="Prefix">键前缀：某个开放命名空间的前缀，或更长的前缀。</param>
/// <param name="Limit">本页最多几个键。</param>
/// <param name="Continuation">上一页给的续页标记（原样传回）；第一页为空。</param>
public sealed record CacheKeysRequest(
    [Required, StringLength(CacheKey.MaxLength, MinimumLength = 1)] string Prefix,
    [Range(1, 1000)] int Limit = 100,
    [StringLength(1024)] string? Continuation = null);

/// <summary>
/// 列出的一个缓存键。
/// </summary>
/// <param name="Key">键。</param>
/// <param name="Layer">所在的层。</param>
public sealed record CacheKeyItem(string Key, CacheLayer Layer);

/// <summary>
/// 一页缓存键。
/// </summary>
/// <param name="Items">键：先对象缓存、后字节存储，各按存储返回的顺序（同一页内去重）。</param>
/// <param name="Continuation">下一页的续页标记；没有下一页为空。</param>
public sealed record CacheKeysResponse(IReadOnlyList<CacheKeyItem> Items, string? Continuation);

/// <summary>
/// 按键取一个缓存项的请求。
/// </summary>
/// <param name="Key">完整的键，须在开放的命名空间里。</param>
public sealed record CacheKeyRequest([Required, StringLength(CacheKey.MaxLength, MinimumLength = 1)] string Key);

/// <summary>
/// 一个缓存项。
/// </summary>
/// <param name="Key">键。</param>
/// <param name="Layer">所在的层。</param>
/// <param name="TypeName">对象缓存里值的类型全名；字节存储的为空。</param>
/// <param name="Size">载荷字节数；对象缓存的为空。</param>
/// <param name="TimeToLiveSeconds">剩余存活秒数；不过期为空。</param>
/// <param name="SlidingSeconds">滑动过期的秒数；没有为空。</param>
/// <param name="Text">字节存储的载荷按 UTF-8 解出的文本，对象缓存的值按 JSON 写出；不是合法 UTF-8 或写不出 JSON 时为空。</param>
/// <param name="Editable">能否改写（字节存储里、命名空间允许且载荷是文本）。</param>
public sealed record CacheEntryResponse(string Key, CacheLayer Layer, string? TypeName, long? Size, long? TimeToLiveSeconds, long? SlidingSeconds, string? Text, bool Editable);

/// <summary>
/// <c>caches.update</c> 的请求。
/// </summary>
/// <param name="Key">键，须在可改写的命名空间里且已存在。</param>
/// <param name="Value">新值（按 UTF-8 存），过期设置沿用原项。</param>
public sealed record UpdateCacheRequest(
    [Required, StringLength(CacheKey.MaxLength, MinimumLength = 1)] string Key,
    [Required, StringLength(1_048_576)] string Value);

/// <summary>
/// <c>caches.delete</c> 的请求。
/// </summary>
/// <param name="Keys">要删的键，每个都须在开放的命名空间里。</param>
public sealed record CacheKeysDeleteRequest([Required, MinLength(1), MaxLength(100)] IReadOnlyList<string> Keys);

/// <summary>
/// 批量删除的结果。
/// </summary>
/// <param name="Removed">删掉的键（本来就不存在的不在内）。</param>
/// <param name="Failed">不在开放命名空间里的键与原因码。</param>
public sealed record CacheDeleteResult(IReadOnlyList<string> Removed, IReadOnlyList<CacheDeleteFailure> Failed);

/// <summary>
/// 删除失败的一个键。
/// </summary>
/// <param name="Key">键。</param>
/// <param name="Code">原因码。</param>
public sealed record CacheDeleteFailure(string Key, string Code);

/// <summary>
/// <c>caches.delete-by-prefix</c> 的请求。
/// </summary>
/// <param name="Prefix">前缀：某个开放命名空间的前缀，或更长的前缀。</param>
public sealed record CachePrefixRequest([Required, StringLength(CacheKey.MaxLength, MinimumLength = 1)] string Prefix);

/// <summary>
/// 按前缀删除的结果。
/// </summary>
/// <param name="Removed">删掉的键数。</param>
public sealed record CachePrefixDeleteResult(long Removed);
