// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace XiHan.BasicApp.Runtime.Schema;

/// <summary>
/// 核对租户独立库是否可用
/// </summary>
/// <remarks>
/// 启动后把已就绪的独立库逐个核对一遍（关掉自动升级、由运维先跑 <c>schema upgrade</c> 时也知道哪些库落后），之后每 <see cref="PollInterval"/> 复查不可用的库：
/// 别的节点或维护页重试成功之后，本节点随之放行。启动时读不到租户目录就在下一轮接着核对全部，直到核对过一遍
/// </remarks>
/// <param name="tenantSchemas">租户独立库</param>
/// <param name="health">不可用的租户独立库</param>
/// <param name="clock">时间源</param>
/// <param name="logger">日志</param>
public sealed partial class TenantSchemaWatch(TenantSchemas tenantSchemas, SchemaHealth health, TimeProvider clock, ILogger<TenantSchemaWatch> logger) : BackgroundService
{
    /// <summary>
    /// 复查不可用的库的间隔
    /// </summary>
    public static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(60);

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var checkedAll = await CheckAllAsync(logUnreadable: true, stoppingToken).ConfigureAwait(false);
        using var timer = new PeriodicTimer(PollInterval, clock);
        while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
        {
            if (!checkedAll)
            {
                checkedAll = await CheckAllAsync(logUnreadable: false, stoppingToken).ConfigureAwait(false);
                continue;
            }

            foreach (var failure in health.Failures)
            {
                await CheckAsync(failure.Database, stoppingToken).ConfigureAwait(false);
            }
        }
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
}
