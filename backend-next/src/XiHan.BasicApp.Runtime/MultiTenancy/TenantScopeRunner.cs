// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.Contracts.MultiTenancy;
using XiHan.Framework.Kernel.Hosting;
using XiHan.Framework.Kernel.Pipeline;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Runtime.MultiTenancy;

/// <summary>
/// <see cref="ITenantScopeRunner"/> 的实现：新开服务作用域，把租户放进 Baggage（框架的 <c>BaggageTenantResolver</c> 读它，中间件对照租户目录校验存在且启用），再经内核管道执行动作。
/// 嵌套在另一次调用里也可以（种子就是这样用的）：各自有自己的作用域、上下文与工作单元。
/// </summary>
/// <param name="app">内核应用。</param>
/// <param name="scopes">服务作用域工厂。</param>
public sealed class TenantScopeRunner(App app, IServiceScopeFactory scopes) : ITenantScopeRunner
{
    /// <inheritdoc />
    public async Task<Result> RunAsync(string tenantId, Func<InvocationContext, ValueTask<Result>> action, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentNullException.ThrowIfNull(action);
        await using var scope = scopes.CreateAsyncScope();
        var context = App.CreateContext(scope.ServiceProvider, cancellationToken, baggage: new Dictionary<string, string>(StringComparer.Ordinal) { [BaggageKeys.Tenant] = tenantId });
        return await app.InvokeAsync(context, async ctx => ctx.SetOutcome(await action(ctx).ConfigureAwait(false))).ConfigureAwait(false);
    }
}
