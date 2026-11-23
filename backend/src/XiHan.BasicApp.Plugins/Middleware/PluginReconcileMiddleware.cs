// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.Plugins.Infrastructure;
using XiHan.Framework.Kernel.Pipeline;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Plugins.Middleware;

/// <summary>
/// Transaction 阶段开头的中间件，插件改动提交之后让各节点对齐
/// </summary>
/// <remarks>
/// 排在工作单元之外：调用成功、改动已经提交，且调用标记了 <see cref="PluginChanges"/> 才对齐；本节点对齐完再广播给其他节点（<see cref="PluginBroadcast"/>），然后才应答。加载失败记在各插件上，不改变调用的结果
/// </remarks>
public sealed class PluginReconcileMiddleware : IPipelineMiddleware
{
    /// <inheritdoc />
    public async ValueTask InvokeAsync(InvocationContext context, PipelineStep nextStep)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(nextStep);
        await nextStep(context).ConfigureAwait(false);
        var succeeded = !context.FindOutcome().Match(static r => r.IsFailure, static () => false);
        if (succeeded && context.Services.GetRequiredService<PluginChanges>().Requested)
        {
            // 改动已经提交：调用方断开也要对齐完，不跟着调用取消
            await context.Services.GetRequiredService<PluginReconciler>().ReconcileAsync(CancellationToken.None).ConfigureAwait(false);
            await context.Services.GetRequiredService<PluginBroadcast>().AnnounceAsync().ConfigureAwait(false);
        }
    }
}
