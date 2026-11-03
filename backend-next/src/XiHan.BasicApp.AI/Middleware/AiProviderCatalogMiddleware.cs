// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.AI.Domain.Services;
using XiHan.BasicApp.AI.Infrastructure;
using XiHan.Framework.Caching.Abstractions.Caches;
using XiHan.Framework.Caching.Abstractions.Stores;
using XiHan.Framework.Kernel.Pipeline;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.AI.Middleware;

/// <summary>
/// 提供商写入之后重载 Provider 目录
/// </summary>
/// <remarks>
/// 排在 Transaction 阶段开头、工作单元之外：调用成功、工作单元提交之后，本实例当场重载快照（新的调用即用新配置），
/// 再改写共享缓存的戳键，配了 Redis 时别的实例经变更通知重载。失败或回滚的调用不重载
/// </remarks>
public sealed class AiProviderCatalogMiddleware : IPipelineMiddleware
{
    /// <summary>
    /// 戳键的存活时间
    /// </summary>
    /// <remarks>
    /// 戳键只为触发变更通知，值本身没人读
    /// </remarks>
    private static readonly TimeSpan _stampLifetime = TimeSpan.FromDays(1);

    /// <inheritdoc />
    public async ValueTask InvokeAsync(InvocationContext context, PipelineStep nextStep)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(nextStep);
        await nextStep(context).ConfigureAwait(false);
        if (context.Services.GetService<AiProviderChanges>() is not { Changed: true } || context.GetOutcome().Match(static r => r.IsFailure, static () => false))
        {
            return;
        }

        // 工作单元已经结束，调用的取消令牌可能已触发，重载仍要做完
        await context.Services.GetRequiredService<AiProviderCatalog>().ReloadAsync(CancellationToken.None).ConfigureAwait(false);
        await context.Services.GetRequiredService<ISharedCache>().SetAsync(AiProviderCatalog.StampKey, Guid.NewGuid().ToString("N"), CacheEntryOptions.Expires(_stampLifetime), CancellationToken.None).ConfigureAwait(false);
    }
}
