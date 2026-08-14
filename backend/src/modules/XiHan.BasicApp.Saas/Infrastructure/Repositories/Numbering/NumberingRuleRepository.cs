// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Data.SqlSugar.Clients;

namespace XiHan.BasicApp.Saas.Infrastructure.Repositories;

/// <summary>
/// 业务编号规则 SqlSugar 仓储实现。
/// </summary>
/// <param name="clientResolver">SqlSugar 客户端解析器，根据当前租户上下文选择平台库、共享租户库或独立租户库连接。</param>
/// <remarks>
/// 使用 <see cref="SaasRepository{TEntity}"/> 提供的当前数据库连接，但每个专用查询仍显式过滤
/// <c>TenantId</c>，防止默认多租户过滤同时可见全局数据时误命中错误作用域。
/// </remarks>
public sealed class NumberingRuleRepository(ISqlSugarClientResolver clientResolver)
    : SaasRepository<SysNumberingRule>(clientResolver), INumberingRuleRepository
{
    /// <summary>
    /// 在明确规则所属租户内按编码查询规则。
    /// </summary>
    /// <param name="ownerTenantId">规则所属租户；0 表示平台全局规则。</param>
    /// <param name="ruleCode">精确匹配的规则编码。</param>
    /// <param name="enabledOnly">是否只接受启用状态的规则。</param>
    /// <param name="cancellationToken">用于取消 SqlSugar 查询的取消令牌。</param>
    /// <returns>作用域和编码均匹配的首条规则；不存在时返回 <see langword="null"/>。</returns>
    /// <exception cref="ArgumentException"><paramref name="ruleCode"/> 为空或仅包含空白字符。</exception>
    /// <exception cref="OperationCanceledException">查询被 <paramref name="cancellationToken"/> 取消。</exception>
    public async Task<SysNumberingRule?> FindByCodeInScopeAsync(
        long ownerTenantId,
        string ruleCode,
        bool enabledOnly,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ruleCode);
        cancellationToken.ThrowIfCancellationRequested();

        // 框架读过滤会合并当前租户与全局数据；显式 TenantId 条件保证作用域解析不会误命中另一层规则。
        var query = CreateQueryable()
            .Where(rule => rule.TenantId == ownerTenantId && rule.RuleCode == ruleCode);
        if (enabledOnly)
        {
            query = query.Where(rule => rule.Status == EnableStatus.Enabled);
        }

        return await query.FirstAsync(cancellationToken);
    }

    /// <summary>
    /// 在明确规则所属租户内按主键查询规则。
    /// </summary>
    /// <param name="ownerTenantId">规则所属租户；0 表示平台全局规则。</param>
    /// <param name="id">规则主键。</param>
    /// <param name="cancellationToken">用于取消 SqlSugar 查询的取消令牌。</param>
    /// <returns>作用域和主键均匹配的规则；主键无效或不存在时返回 <see langword="null"/>。</returns>
    /// <exception cref="OperationCanceledException">查询被 <paramref name="cancellationToken"/> 取消。</exception>
    public async Task<SysNumberingRule?> FindByIdInScopeAsync(long ownerTenantId, long id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            return null;
        }

        cancellationToken.ThrowIfCancellationRequested();
        // 主键在独立租户库中可能重复，仍需组合 TenantId 保证共享库和独立库使用同一安全查询语义。
        return await CreateQueryable()
            .Where(rule => rule.TenantId == ownerTenantId && rule.BasicId == id)
            .FirstAsync(cancellationToken);
    }

    /// <summary>
    /// 把规则的流水基线单调推进到新周期。
    /// </summary>
    /// <param name="ownerTenantId">规则所属租户；0 表示平台全局规则。</param>
    /// <param name="ruleId">规则主键。</param>
    /// <param name="periodKey">新周期键。</param>
    /// <param name="periodOrdinal">与 <paramref name="periodKey"/> 对应的周期序号。</param>
    /// <param name="cancellationToken">用于取消数据库操作的取消令牌。</param>
    /// <returns>规则确实被翻转到新周期时为 <see langword="true"/>；已在该周期或周期更新时为 <see langword="false"/>。</returns>
    /// <remarks>
    /// 只接受比库中更大的周期序号，因此时钟落后的节点无法把规则拉回旧周期重发编号。
    /// 返回 <see langword="false"/> 不代表失败：绝大多数发号都落在当前周期内，本语句本就应当命中 0 行。
    /// </remarks>
    /// <exception cref="ArgumentException"><paramref name="periodKey"/> 为空或仅包含空白字符。</exception>
    /// <exception cref="InvalidOperationException">当前连接上没有活动事务。</exception>
    /// <exception cref="OperationCanceledException">操作被 <paramref name="cancellationToken"/> 取消。</exception>
    public async Task<bool> TryRollOverPeriodAsync(
        long ownerTenantId,
        long ruleId,
        string periodKey,
        long periodOrdinal,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(periodKey);
        cancellationToken.ThrowIfCancellationRequested();
        EnsureInDatabaseTransaction();

        // 只接受更大的周期序号：时钟落后的节点无法把规则拉回旧周期，从而不会重发该周期已经发出的编号。
        // 谓词显式写全所属租户、软删和启停，语义不随环境租户上下文或框架过滤器默认开关漂移。
        return await UpdateAsync(
            rule => new SysNumberingRule
            {
                CurrentValue = 0,
                CurrentPeriod = periodKey,
                CurrentPeriodOrdinal = periodOrdinal,
                RowVersion = rule.RowVersion + 1
            },
            rule => rule.BasicId == ruleId
                && rule.TenantId == ownerTenantId
                && !rule.IsDeleted
                && rule.Status == EnableStatus.Enabled
                && rule.CurrentPeriodOrdinal < periodOrdinal,
            cancellationToken);
    }

    /// <summary>
    /// 在当前周期内原子推进流水，并占用一段连续区间。
    /// </summary>
    /// <param name="ownerTenantId">规则所属租户；0 表示平台全局规则。</param>
    /// <param name="ruleId">规则主键。</param>
    /// <param name="periodKey">调用方计算出的当前周期键；与库中不一致时不推进。</param>
    /// <param name="count">本次占用的连续流水数量。</param>
    /// <param name="maximum">流水位数允许的最大值。</param>
    /// <param name="cancellationToken">用于取消数据库操作的取消令牌。</param>
    /// <returns>成功占用区间时为 <see langword="true"/>；任一守卫拒绝时为 <see langword="false"/>。</returns>
    /// <remarks>
    /// <para>语句自身完成「读取当前值并累加」，不经过应用层读改写，因此不存在丢失更新。</para>
    /// <para>推进的同时显式自增 <c>RowVersion</c>，使管理端加载后的整实体回写会因乐观锁失败而拒绝，不会把流水打回旧值。</para>
    /// <para>命中的行会被数据库持有排他锁直到事务提交，<see cref="ReadCurrentValueAsync"/> 因此能读到本次推进后的确切值。</para>
    /// </remarks>
    /// <exception cref="ArgumentException"><paramref name="periodKey"/> 为空或仅包含空白字符。</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> 不为正数，或 <paramref name="maximum"/> 为负数。</exception>
    /// <exception cref="InvalidOperationException">当前连接上没有活动事务。</exception>
    /// <exception cref="OperationCanceledException">操作被 <paramref name="cancellationToken"/> 取消。</exception>
    public async Task<bool> TryAdvanceSequenceAsync(
        long ownerTenantId,
        long ruleId,
        string periodKey,
        int count,
        long maximum,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(periodKey);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(count);
        ArgumentOutOfRangeException.ThrowIfNegative(maximum);
        cancellationToken.ThrowIfCancellationRequested();
        EnsureInDatabaseTransaction();

        // 先做减法再比较，避免 currentValue + count 在 18 位边界上整数溢出。
        var ceiling = maximum - count;

        // 累加在数据库内完成，应用层不参与读改写，因此不存在丢失更新；
        // 同时显式自增 RowVersion，让管理端「加载整实体 → 改字段 → 乐观锁回写」无法把流水打回旧值。
        return await UpdateAsync(
            rule => new SysNumberingRule
            {
                CurrentValue = rule.CurrentValue + count,
                HasAllocated = true,
                RowVersion = rule.RowVersion + 1
            },
            rule => rule.BasicId == ruleId
                && rule.TenantId == ownerTenantId
                && !rule.IsDeleted
                && rule.Status == EnableStatus.Enabled
                && rule.CurrentPeriod == periodKey
                && rule.CurrentValue <= ceiling,
            cancellationToken);
    }

    /// <summary>
    /// 读取规则当前的流水值与周期基线。
    /// </summary>
    /// <param name="ownerTenantId">规则所属租户；0 表示平台全局规则。</param>
    /// <param name="ruleId">规则主键。</param>
    /// <param name="cancellationToken">用于取消数据库查询的取消令牌。</param>
    /// <returns>当前流水状态；规则不存在时返回 <see langword="null"/>。</returns>
    /// <remarks>
    /// 只在事务内使用：推进成功后读回本事务自己的写入以确定占用区间，或在推进被拒绝后读取实际状态以定位原因。
    /// 事务外调用没有意义，因为返回值随时可能被其他节点改变，故与写入语句采用同一条事务断言。
    /// </remarks>
    /// <exception cref="InvalidOperationException">当前连接上没有活动事务。</exception>
    /// <exception cref="OperationCanceledException">查询被 <paramref name="cancellationToken"/> 取消。</exception>
    public async Task<NumberingSequenceState?> ReadCurrentValueAsync(
        long ownerTenantId,
        long ruleId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        EnsureInDatabaseTransaction();

        var rule = await CreateQueryable()
            .Where(rule => rule.TenantId == ownerTenantId && rule.BasicId == ruleId)
            .FirstAsync(cancellationToken);
        return rule is null
            ? null
            : new NumberingSequenceState(
                rule.CurrentValue,
                rule.CurrentPeriod,
                rule.CurrentPeriodOrdinal,
                rule.Status,
                rule.SerialLength);
    }

    /// <summary>
    /// 断言当前连接上确实存在活动事务。
    /// </summary>
    /// <remarks>
    /// 发号的正确性建立在「推进语句取得的排他行锁一直持有到提交」之上：
    /// 没有事务时锁随语句立即释放，读回值会被其他节点的推进覆盖，进而把同一段区间发给两个调用方。
    /// 这里核验的是连接上的事实而不是工作单元的选项，因为工作单元只是推断，
    /// 真正决定语句行为的是执行它的那条连接（见框架 <c>SqlSugarTransactionApi</c> 的连接钉住语义）。
    /// </remarks>
    /// <exception cref="InvalidOperationException">当前连接上没有活动事务。</exception>
    private void EnsureInDatabaseTransaction()
    {
        if (DbClient.Ado.IsNoTran())
        {
            throw new InvalidOperationException(
                "业务编号流水推进必须在数据库事务内执行，当前连接上没有活动事务。" +
                "请确认调用链上存在事务型工作单元；发号器会在没有环境工作单元时自行开启事务。");
        }
    }
}
