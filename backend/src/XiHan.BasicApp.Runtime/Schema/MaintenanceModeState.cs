// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Data.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
/// </remarks>
/// <param name="scopes">作用域工厂</param>
/// <param name="options">结构升级配置</param>
/// <param name="clock">时间源</param>
public sealed class MaintenanceModeState(IServiceScopeFactory scopes, SchemaOptions options, TimeProvider clock) : BackgroundService
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

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(PollInterval, clock);
        do
        {
            _active = await ReadAsync(stoppingToken).ConfigureAwait(false);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false));
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
}
