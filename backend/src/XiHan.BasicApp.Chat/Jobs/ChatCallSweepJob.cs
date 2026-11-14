// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Chat.Domain.Services;
using XiHan.BasicApp.Contracts;
using XiHan.BasicApp.Runtime.MultiTenancy;
using XiHan.Framework.Kernel.Hosting;
using XiHan.Framework.Kernel.Pipeline;
using XiHan.Framework.MultiTenancy.Abstractions;
using XiHan.Framework.Primitives;
using XiHan.Framework.Scheduling.Abstractions.Jobs;
using XiHan.Framework.Scheduling.Abstractions.Schedules;

namespace XiHan.BasicApp.Chat.Jobs;

/// <summary>
/// 聊天通话的收尾
/// </summary>
/// <remarks>
/// 内建作业（每 5 分钟）。平台与目录里的每个租户各开一次内部调用（租户经 Baggage，独立库的租户收的是它自己的库），
/// 把响铃过点没人管的记未接听，结束发起人已离线的响铃与有人离线的通话；通话记录与推送随那次调用提交之后发出
/// 正常情况下这些由两端的计时器与离线消息先一步做掉，这里兜的是端崩了、消息丢了的情形
/// 停用的租户跳过（没人连得上，等它启用后再收）；一侧失败只记日志、计入失败，不影响其他
/// </remarks>
/// <param name="sides">要收尾的各侧</param>
/// <param name="app">内核应用</param>
/// <param name="scopes">作用域工厂</param>
/// <param name="logger">日志</param>
public sealed partial class ChatCallSweepJob(TenantSides sides, App app, IServiceScopeFactory scopes, ILogger<ChatCallSweepJob> logger) : IJob
{
    /// <summary>
    /// 作业名
    /// </summary>
    public const string Name = "chat.calls.sweep";

    /// <summary>
    /// 调度：每 5 分钟
    /// </summary>
    public static JobSchedule Schedule { get; } = JobSchedule.FromCron("*/5 * * * *");

    /// <inheritdoc />
    public async ValueTask<Result> RunAsync(JobRun run, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(run);
        ArgumentNullException.ThrowIfNull(context);
        var cancellationToken = context.CancellationToken;
        var listedSides = await sides.ListAsync(cancellationToken).ConfigureAwait(false);
        if (!listedSides.TryGetValue(out var sideIds))
        {
            return listedSides.Error!;
        }

        var swept = 0;
        var failures = 0;
        foreach (var side in sideIds)
        {
            var result = await SweepAsync(side, cancellationToken).ConfigureAwait(false);
            if (result.TryGetValue(out var count))
            {
                swept += count;
            }
            else if (!string.Equals(result.Error!.Code, MultiTenancyErrors.Disabled, StringComparison.Ordinal))
            {
                failures++;
                LogSweepFailed(logger, side, result.Error.Code);
            }
        }

        if (swept > 0 || failures > 0)
        {
            LogSwept(logger, swept, failures);
        }

        return TenantSides.Outcome(Name, failures);
    }

    /// <summary>
    /// 以一侧开一次内部调用收尾
    /// </summary>
    private async Task<Result<int>> SweepAsync(string side, CancellationToken cancellationToken)
    {
        await using var scope = scopes.CreateAsyncScope();
        var baggage = string.Equals(side, TenantIds.Platform, StringComparison.Ordinal)
            ? null
            : new Dictionary<string, string>(StringComparer.Ordinal) { [BaggageKeys.Tenant] = side };
        var inner = App.CreateContext(scope.ServiceProvider, new InvocationContextOptions { Baggage = baggage }, cancellationToken);
        var swept = 0;
        var outcome = await app.InvokeAsync(inner, async ctx =>
        {
            var result = await ctx.Services.GetRequiredService<ChatCallService>().SweepAsync(ctx, side).ConfigureAwait(false);
            if (result.TryGetValue(out var count))
            {
                swept = count;
                ctx.SetOutcome(Result.Success());
            }
            else
            {
                ctx.SetOutcome(result.Error!);
            }
        }).ConfigureAwait(false);
        return outcome.IsSuccess ? swept : outcome.Error!;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Chat call sweep ended {Swept} calls with {Failures} failures.")]
    private static partial void LogSwept(ILogger logger, int swept, int failures);

    [LoggerMessage(Level = LogLevel.Error, Message = "Chat call sweep failed for side '{Side}' ({Code}).")]
    private static partial void LogSweepFailed(ILogger logger, string side, string code);
}
