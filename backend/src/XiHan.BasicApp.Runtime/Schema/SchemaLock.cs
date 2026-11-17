// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Logging;
using SqlSugar;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Runtime.Schema;

/// <summary>
/// 一个库的升级锁
/// </summary>
/// <remarks>
/// 锁就是库里台账行（<see cref="SysVersion"/>）的 <see cref="SysVersion.IsUpgrading"/>：条件更新是原子的，只有一个节点改得动。时间取节点时钟，各节点的偏差远小于锁的失效时间
/// 平台库的锁在升级期间让别的节点进入维护模式；租户独立库各用自己的那一行，重试一个租户库不影响别的调用
/// </remarks>
internal static partial class SchemaLock
{
    private static readonly TimeSpan _retryDelay = TimeSpan.FromSeconds(5);

    /// <summary>
    /// 抢锁
    /// </summary>
    /// <param name="client">库的连接</param>
    /// <param name="node">持锁的节点</param>
    /// <param name="expiry">锁过了这么久即视为持锁节点已退出</param>
    /// <param name="wait">别的节点持着锁时最多等多久；为零即不等</param>
    /// <param name="clock">时间源</param>
    /// <param name="logger">日志</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>抢到即成功；等不到即 <see cref="RuntimeErrors.SchemaUpgradeLocked"/></returns>
    public static async Task<Result> AcquireAsync(ISqlSugarClient client, string node, TimeSpan expiry, TimeSpan wait, TimeProvider clock, ILogger logger, CancellationToken cancellationToken)
    {
        var deadline = clock.GetUtcNow() + wait;
        while (true)
        {
            var now = clock.GetUtcNow();
            var expired = now - expiry;
            var taken = await client.Updateable<SysVersion>()
                .SetColumns(v => new SysVersion { IsUpgrading = true, UpgradeNode = node, UpgradeStartedAt = now })
                .Where(v => v.Id == SysVersion.RowId && (!v.IsUpgrading || v.UpgradeStartedAt == null || v.UpgradeStartedAt < expired))
                .ExecuteCommandAsync(cancellationToken).ConfigureAwait(false);
            if (taken > 0)
            {
                return Result.Success();
            }

            if (clock.GetUtcNow() >= deadline)
            {
                return Error.Literal(RuntimeErrors.SchemaUpgradeLocked, wait == TimeSpan.Zero
                    ? "Another node is upgrading this database."
                    : $"Another node is still upgrading the database schema after {wait.TotalMinutes} minutes.");
            }

            LogWaiting(logger);
            await Task.Delay(_retryDelay, clock, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// 释放这个节点持着的锁
    /// </summary>
    /// <param name="client">库的连接</param>
    /// <param name="node">持锁的节点</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>这个节点确实持着锁、已释放为 <see langword="true"/></returns>
    public static async Task<bool> ReleaseAsync(ISqlSugarClient client, string node, CancellationToken cancellationToken)
        => await client.Updateable<SysVersion>()
            .SetColumns(static v => new SysVersion { IsUpgrading = false })
            .Where(v => v.Id == SysVersion.RowId && v.IsUpgrading && v.UpgradeNode == node)
            .ExecuteCommandAsync(cancellationToken).ConfigureAwait(false) > 0;

    [LoggerMessage(Level = LogLevel.Information, Message = "Another node is upgrading the database schema; waiting.")]
    private static partial void LogWaiting(ILogger logger);
}
