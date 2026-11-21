// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Data.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SqlSugar;
using XiHan.BasicApp.Runtime.Persistence;
using XiHan.Framework.Persistence.SqlSugar;

namespace XiHan.BasicApp.Runtime.Schema;

/// <summary>
/// 共享的库是否正在被别的节点升级
/// </summary>
/// <remarks>
/// 每 <see cref="PollInterval"/> 读一次平台库的版本台账：有节点持着没过期的升级锁即进入维护模式，
/// 维护模式下除健康检查外的请求一律 503。台账还没建或读不到时不进入维护模式
/// 轮询由 <see cref="RuntimeFeature"/> 在内核启动核对通过之后开始、停止时结束，不是宿主的后台服务：核对没过时它根本不开始，宿主起不来也就没有要收尾的后台服务
/// </remarks>
/// <param name="scopes">作用域工厂</param>
/// <param name="options">结构升级配置</param>
/// <param name="clock">时间源</param>
/// <param name="logger">日志</param>
public sealed partial class MaintenanceModeState(IServiceScopeFactory scopes, SchemaOptions options, TimeProvider clock, ILogger<MaintenanceModeState> logger)
{
    /// <summary>
    /// 读台账的间隔
    /// </summary>
    public static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(10);

    private volatile bool _active;

    /// <summary>
    /// 是否处在维护模式
    /// </summary>
    public bool IsActive => _active;

    /// <summary>
    /// 轮询台账，直到停止
    /// </summary>
    /// <remarks>
    /// 一轮出了意外只记日志，维护模式保持上一轮的结论，下一轮再读；容器已经释放（宿主没停就被释放）时结束
    /// </remarks>
    /// <param name="stopping">停止信号</param>
    /// <returns>停止后结束的任务</returns>
    public async Task RunAsync(CancellationToken stopping)
    {
        try
        {
            using var timer = new PeriodicTimer(PollInterval, clock);
            do
            {
                try
                {
                    _active = await ReadAsync(stopping).ConfigureAwait(false);
                }
                catch (Exception exception) when (!stopping.IsCancellationRequested && exception is not ObjectDisposedException)
                {
                    LogReadFailed(logger, exception);
                }
            }
            while (await timer.WaitForNextTickAsync(stopping).ConfigureAwait(false));
        }
        catch (OperationCanceledException) when (stopping.IsCancellationRequested)
        {
            // 停止
        }
        catch (ObjectDisposedException)
        {
            // 容器已经释放：宿主没停就被释放，没有可读的了
        }
    }

    private async Task<bool> ReadAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await using var scope = scopes.CreateAsyncScope();
        var client = scope.ServiceProvider.GetRequiredService<SqlSugarUnitOfWork>().ConnectionFor(typeof(SysVersion));
        // 新库还没建表时读不到，下面接住了：不是错误，不进 SQL 错误日志
        using var expected = SqlDiagnostics.ExpectErrors();
        try
        {
            // 与升级锁同一口径：时间取节点时钟
            var expired = clock.GetUtcNow() - options.LockExpiry;
            return await client.Queryable<SysVersion>()
                .AnyAsync(v => v.Id == SysVersion.RowId && v.IsUpgrading && v.UpgradeStartedAt > expired, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is DbException or SqlSugarException)
        {
            return false;
        }
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Reading the schema version ledger for maintenance mode failed; trying again at the next poll.")]
    private static partial void LogReadFailed(ILogger logger, Exception exception);
}
