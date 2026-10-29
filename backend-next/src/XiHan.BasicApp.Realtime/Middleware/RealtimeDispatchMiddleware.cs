// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.Realtime.Infrastructure;
using XiHan.Framework.Kernel.Pipeline;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Realtime.Middleware;

/// <summary>
/// Transaction 阶段开头的中间件，调用成功之后才发出推送。
/// </summary>
/// <remarks>
/// 排在工作单元之外。调用里的推送先暂存，工作单元提交、调用成功之后才发出，失败、回滚或异常即丢弃（4.x 在提交前推送，回滚了客户端也已收到）。
/// </remarks>
public sealed class RealtimeDispatchMiddleware : IPipelineMiddleware
{
    /// <inheritdoc />
    public async ValueTask InvokeAsync(InvocationContext context, PipelineStep nextStep)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(nextStep);
        var outbox = context.Services.GetRequiredService<RealtimeOutbox>();
        outbox.Begin();
        var succeeded = false;
        try
        {
            await nextStep(context).ConfigureAwait(false);
            succeeded = !context.GetOutcome().Match(static r => r.IsFailure, static () => false);
        }
        finally
        {
            await outbox.EndAsync(succeeded).ConfigureAwait(false);
        }
    }
}
