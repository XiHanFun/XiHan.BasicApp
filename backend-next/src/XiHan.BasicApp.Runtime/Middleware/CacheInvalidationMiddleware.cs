// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Runtime.Persistence;
using XiHan.Framework.Kernel.Pipeline;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Runtime.Middleware;

/// <summary>
/// 把调用里的缓存失效推到工作单元之后的中间件。
/// </summary>
/// <remarks>
/// Transaction 阶段开头、排在工作单元之外：调用里经 <see cref="CacheInvalidation"/> 记下的失效，在工作单元提交或回滚之后才执行，
/// 读穿缓存不会在提交之前被同一请求或并发的请求用旧数据回填。
/// </remarks>
public sealed class CacheInvalidationMiddleware : IPipelineMiddleware
{
    /// <inheritdoc />
    public async ValueTask InvokeAsync(InvocationContext context, PipelineStep nextStep)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(nextStep);
        await using (CacheInvalidation.Defer().ConfigureAwait(false))
        {
            await nextStep(context).ConfigureAwait(false);
        }
    }
}
