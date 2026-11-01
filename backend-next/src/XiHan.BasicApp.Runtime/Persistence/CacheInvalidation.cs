// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.Framework.Caching.Abstractions.Caches;
using XiHan.Framework.Caching.Abstractions.Stores;

namespace XiHan.BasicApp.Runtime.Persistence;

/// <summary>
/// 读穿缓存的失效，在调用里推到工作单元结束之后。
/// </summary>
/// <remarks>
/// 读穿缓存未命中时经 <see cref="StoreScope"/> 在独立工作单元里读库，看不到调用里还没提交的写入。
/// 写入方若在提交之前失效，同一请求或并发的请求在失效与提交之间再读一次，就把提交前的旧数据写回缓存，直到缓存过期。
/// 所以调用里的失效只记下，由 <see cref="Middleware.CacheInvalidationMiddleware"/> 在工作单元提交或回滚之后执行；
/// 回滚了照样失效（多失效一次只是多读一次库）。不在调用里（管道之外的种子、作业）即刻失效。
/// </remarks>
public static class CacheInvalidation
{
    private static readonly AsyncLocal<Batch?> _current = new();

    /// <summary>
    /// 失效一个缓存键。
    /// </summary>
    /// <remarks>
    /// 在调用里只记下，调用的工作单元结束之后才从缓存删除；不在调用里立即删除。
    /// </remarks>
    /// <param name="cache">共享缓存。</param>
    /// <param name="key">缓存键。</param>
    /// <param name="cancellationToken">取消令牌（只用于立即删除）。</param>
    public static async ValueTask RemoveAsync(ISharedCache cache, CacheKey key, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(cache);
        if (_current.Value is { } batch)
        {
            batch.Add(cache, key);
            return;
        }

        await cache.RemoveAsync(key, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// 开一段推迟失效的范围。
    /// </summary>
    /// <remarks>
    /// 范围里经 <see cref="RemoveAsync"/> 的失效都记下；释放时先还原外层范围，再逐个删除记下的键。
    /// 嵌套的调用各有自己的工作单元，各自的范围在各自结束时执行，不并入外层。
    /// </remarks>
    /// <returns>范围句柄。</returns>
    public static IAsyncDisposable Defer()
    {
        var batch = new Batch(_current.Value);
        _current.Value = batch;
        return batch;
    }

    /// <summary>
    /// 一段范围里记下的失效。
    /// </summary>
    /// <param name="outer">外层范围。</param>
    private sealed class Batch(Batch? outer) : IAsyncDisposable
    {
        private readonly Lock _gate = new();

        private readonly List<(ISharedCache Cache, CacheKey Key)> _pending = [];

        /// <summary>
        /// 记下一个要失效的键。
        /// </summary>
        public void Add(ISharedCache cache, CacheKey key)
        {
            lock (_gate)
            {
                _pending.Add((cache, key));
            }
        }

        /// <inheritdoc />
        public ValueTask DisposeAsync()
        {
            // 同步还原：异步方法里改的异步本地值传不回调用方
            _current.Value = outer;
            return FlushAsync();
        }

        private async ValueTask FlushAsync()
        {
            (ISharedCache Cache, CacheKey Key)[] pending;
            lock (_gate)
            {
                pending = [.. _pending.Distinct()];
                _pending.Clear();
            }

            // 工作单元已经结束，调用的取消令牌可能已触发，失效仍要做完；删除失败照常抛出
            foreach (var (cache, key) in pending)
            {
                await cache.RemoveAsync(key, CancellationToken.None).ConfigureAwait(false);
            }
        }
    }
}
