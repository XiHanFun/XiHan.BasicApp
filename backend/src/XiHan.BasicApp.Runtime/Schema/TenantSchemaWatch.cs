// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Logging;
using XiHan.Framework.Kernel.Hosting;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Runtime.Schema;

/// <summary>
/// 核对租户独立库是否可用
/// </summary>
/// <remarks>
/// 内核进入服务状态后把已就绪的独立库逐个核对一遍（关掉自动升级、由运维先跑 <c>schema upgrade</c> 时也知道哪些库落后），之后每 <see cref="PollInterval"/> 复查不可用的库：
/// 别的节点或维护页重试成功之后，本节点随之放行。服务状态之前不读租户目录：开发与测试按实体建表（InitTables）在内核启动里，那时目录表可能还没有
/// 启动升级在宿主启动之前做完，它记下的失败这一遍照样按各库的台账核对，台账上还是失败或落后的保持不可用。读不到租户目录就在下一轮接着核对全部，直到核对过一遍
/// 轮询由 <see cref="RuntimeFeature"/> 在内核启动核对通过之后开始、停止时结束，不是宿主的后台服务：核对没过时它根本不开始，宿主起不来也就没有要收尾的后台服务
/// </remarks>
/// <param name="tenantSchemas">租户独立库</param>
/// <param name="health">不可用的租户独立库</param>
/// <param name="app">内核</param>
/// <param name="clock">时间源</param>
/// <param name="logger">日志</param>
public sealed partial class TenantSchemaWatch(TenantSchemas tenantSchemas, SchemaHealth health, App app, TimeProvider clock, ILogger<TenantSchemaWatch> logger)
{
    /// <summary>
    /// 复查不可用的库的间隔
    /// </summary>
    public static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(60);

    private static readonly TimeSpan _servingProbe = TimeSpan.FromMilliseconds(200);

    private readonly TaskCompletionSource _firstPass = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>
    /// 启动后的第一遍核对
    /// </summary>
    /// <remarks>
    /// 读到租户目录、已就绪的独立库都核对过一遍即完成；宿主停止时还没核对完即取消
    /// </remarks>
    public Task FirstPass => _firstPass.Task;

    /// <summary>
    /// 等内核进入服务状态后核对，之后定时复查，直到停止
    /// </summary>
    /// <remarks>
    /// 一轮出了意外只记日志，下一轮重新核对全部；容器已经释放（宿主没停就被释放）时结束
    /// </remarks>
    /// <param name="stopping">停止信号</param>
    /// <returns>停止后结束的任务</returns>
    public async Task RunAsync(CancellationToken stopping)
    {
        try
        {
            while (!app.State.IsServing())
            {
                await Task.Delay(_servingProbe, clock, stopping).ConfigureAwait(false);
            }

            var checkedAll = await RoundAsync(() => CheckAllAsync(logUnreadable: true, stopping), stopping).ConfigureAwait(false);
            using var timer = new PeriodicTimer(PollInterval, clock);
            while (await timer.WaitForNextTickAsync(stopping).ConfigureAwait(false))
            {
                checkedAll = checkedAll
                    ? await RoundAsync(() => RecheckFailuresAsync(stopping), stopping).ConfigureAwait(false)
                    : await RoundAsync(() => CheckAllAsync(logUnreadable: false, stopping), stopping).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (stopping.IsCancellationRequested)
        {
            // 停止
        }
        catch (ObjectDisposedException)
        {
            // 容器已经释放：宿主没停就被释放，没有可核对的了
        }
        finally
        {
            _firstPass.TrySetCanceled(stopping);
        }
    }

    /// <summary>
    /// 执行一轮核对
    /// </summary>
    /// <remarks>
    /// 出了意外记日志并返回 false，下一轮重新核对全部
    /// </remarks>
    private async Task<bool> RoundAsync(Func<Task<bool>> round, CancellationToken stopping)
    {
        try
        {
            return await round().ConfigureAwait(false);
        }
        catch (Exception exception) when (!stopping.IsCancellationRequested && exception is not ObjectDisposedException)
        {
            LogRoundFailed(logger, exception);
            return false;
        }
    }

    private async Task<bool> RecheckFailuresAsync(CancellationToken cancellationToken)
    {
        foreach (var failure in health.Failures)
        {
            await CheckAsync(failure.Database, cancellationToken).ConfigureAwait(false);
        }

        return true;
    }

    private async Task<bool> CheckAllAsync(bool logUnreadable, CancellationToken cancellationToken)
    {
        var ready = await tenantSchemas.ReadyTenantsAsync(cancellationToken).ConfigureAwait(false);
        if (!ready.TryGetValue(out var tenantIds))
        {
            if (logUnreadable)
            {
                LogDirectoryUnreadable(logger, ready.Error!.Code, ready.Error.Message);
            }

            return false;
        }

        foreach (var tenantId in tenantIds)
        {
            await CheckAsync(tenantId, cancellationToken).ConfigureAwait(false);
        }

        _firstPass.TrySetResult();
        return true;
    }

    private async Task CheckAsync(string tenantId, CancellationToken cancellationToken)
    {
        var verdict = await tenantSchemas.CheckAsync(tenantId, cancellationToken).ConfigureAwait(false);
        if (verdict.TryGetValue(out var failure))
        {
            if (!health.IsUnavailable(tenantId))
            {
                LogUnavailable(logger, tenantId, failure.Code, failure.Message);
            }

            health.Fail(failure);
            return;
        }

        if (health.IsUnavailable(tenantId))
        {
            health.Recover(tenantId);
            LogRecovered(logger, tenantId);
        }
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Database of tenant '{TenantId}' is not at the current schema version ({Code}): {Message}; the tenant is unavailable until it is upgraded.")]
    private static partial void LogUnavailable(ILogger logger, string tenantId, string code, string message);

    [LoggerMessage(Level = LogLevel.Information, Message = "Database of tenant '{TenantId}' is at the current schema version again; the tenant is available.")]
    private static partial void LogRecovered(ILogger logger, string tenantId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Tenant databases were not checked: the tenant directory could not be read ({Code}): {Message}; checking again every poll until it is read.")]
    private static partial void LogDirectoryUnreadable(ILogger logger, string code, string message);

    [LoggerMessage(Level = LogLevel.Error, Message = "Checking tenant databases failed; checking all of them again at the next poll.")]
    private static partial void LogRoundFailed(ILogger logger, Exception exception);
}
