// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Concurrent;

namespace XiHan.BasicApp.Saas.Application.Services;

/// <summary>
/// 进程内业务编号规则键锁提供器。
/// </summary>
/// <remarks>
/// 锁键包含规则所属租户和规则主键，避免独立租户库中相同主键互相阻塞。字典生命周期跟随进程且规则集合有界；
/// 此锁仅降低单节点竞争，跨节点正确性仍由数据库 RowVersion 乐观锁和唯一索引保证。
/// </remarks>
public sealed class NumberingLockProvider
{
    /// <summary>
    /// 进程生命周期内的规则键与信号量映射；并发字典负责线程安全，<see cref="Lazy{T}"/> 保证每个键只采用一个锁实例。
    /// </summary>
    /// <remarks>
    /// 字典不主动移除条目，依据是规则集合通常有界且移除仍在等待的信号量会破坏互斥；若未来规则规模无界，应改为带引用计数的安全回收策略。
    /// </remarks>
    private readonly ConcurrentDictionary<NumberingLockKey, Lazy<SemaphoreSlim>> _locks = new();

    /// <summary>
    /// 获取指定规则的共享异步信号量。
    /// </summary>
    /// <param name="ownerTenantId">规则所属租户。</param>
    /// <param name="ruleId">规则主键。</param>
    /// <returns>同一规则键始终返回同一个信号量实例。</returns>
    /// <remarks>
    /// 调用方负责执行 <see cref="SemaphoreSlim.WaitAsync(CancellationToken)"/> 并在 <c>finally</c> 中调用
    /// <see cref="SemaphoreSlim.Release()"/>。此锁是性能优化，不替代数据库并发控制。
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="ownerTenantId"/> 小于 0，或 <paramref name="ruleId"/> 不为正数。</exception>
    public SemaphoreSlim Get(long ownerTenantId, long ruleId)
    {
        if (ownerTenantId < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(ownerTenantId));
        }

        if (ruleId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(ruleId));
        }

        var key = new NumberingLockKey(ownerTenantId, ruleId);
        // ConcurrentDictionary 的值工厂可能并发执行；用 Lazy 包裹可确保只有最终入字典的实例创建 SemaphoreSlim。
        return _locks.GetOrAdd(
            key,
            static _ => new Lazy<SemaphoreSlim>(
                static () => new SemaphoreSlim(1, 1),
                LazyThreadSafetyMode.ExecutionAndPublication)).Value;
    }

    /// <summary>
    /// 规则键值对象。
    /// </summary>
    /// <param name="OwnerTenantId">规则所属租户。</param>
    /// <param name="RuleId">规则主键。</param>
    private readonly record struct NumberingLockKey(long OwnerTenantId, long RuleId);
}
