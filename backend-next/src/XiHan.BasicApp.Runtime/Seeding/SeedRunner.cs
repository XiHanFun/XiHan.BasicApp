// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Contracts.Seeding;
using XiHan.Framework.Kernel.Hosting;
using XiHan.Framework.Kernel.Pipeline;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Runtime.Seeding;

/// <summary>
/// 种子运行器：把已登记的 <see cref="ISeeder"/> 按 <c>Order</c> 排序，每个在一次独立调用（完整管道、自己的工作单元、无租户）里执行；任一失败即停止并返回该失败。
/// 由宿主的 <c>seed</c> 命令调用，要求应用处于服务状态。
/// </summary>
public sealed partial class SeedRunner
{
    private readonly App _app;
    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<SeedRunner> _logger;

    /// <summary>
    /// 构造运行器。
    /// </summary>
    public SeedRunner(App app, IServiceScopeFactory scopes, ILogger<SeedRunner> logger)
    {
        _app = app ?? throw new ArgumentNullException(nameof(app));
        _scopes = scopes ?? throw new ArgumentNullException(nameof(scopes));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 已登记的种子名与顺序（按执行顺序）。
    /// </summary>
    public async Task<IReadOnlyList<(string Name, int Order)>> DescribeAsync(CancellationToken cancellationToken)
    {
        // 种子是作用域服务（带仓储与工作单元，只能异步释放）：列出也要开一个异步作用域
        await using var scope = _scopes.CreateAsyncScope();
        cancellationToken.ThrowIfCancellationRequested();
        return scope.ServiceProvider.GetServices<ISeeder>().OrderBy(static s => s.Order).ThenBy(static s => s.Name, StringComparer.Ordinal).Select(static s => (s.Name, s.Order)).ToList();
    }

    /// <summary>
    /// 执行种子；<paramref name="only"/> 非空时只执行名字在其中的种子。
    /// </summary>
    public async Task<Result> RunAsync(IReadOnlyCollection<string>? only, CancellationToken cancellationToken)
    {
        foreach (var (name, order) in await DescribeAsync(cancellationToken).ConfigureAwait(false))
        {
            if (only is { Count: > 0 } && !only.Contains(name, StringComparer.Ordinal))
            {
                continue;
            }

            // 每个种子一个作用域：仓储与工作单元是作用域服务，种子之间互不共享事务
            await using var scope = _scopes.CreateAsyncScope();
            var seeder = scope.ServiceProvider.GetServices<ISeeder>().First(s => string.Equals(s.Name, name, StringComparison.Ordinal));
            var context = App.CreateContext(scope.ServiceProvider, cancellationToken);
            var outcome = await _app.InvokeAsync(context, async ctx => ctx.SetOutcome(await seeder.SeedAsync(ctx).ConfigureAwait(false))).ConfigureAwait(false);
            if (outcome.IsFailure)
            {
                LogFailed(_logger, name, order, outcome.Error!.Code);
                return outcome;
            }

            LogSucceeded(_logger, name, order);
        }

        return Result.Success();
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Seeder '{Name}' (order {Order}) completed.")]
    private static partial void LogSucceeded(ILogger logger, string name, int order);

    [LoggerMessage(Level = LogLevel.Error, Message = "Seeder '{Name}' (order {Order}) failed with '{Code}'; later seeders were not run.")]
    private static partial void LogFailed(ILogger logger, string name, int order, string code);
}
