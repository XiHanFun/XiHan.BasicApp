// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Domain.Repositories;

/// <summary>
/// 业务编号规则仓储契约。
/// </summary>
/// <remarks>
/// 业务编号同时支持全局规则和租户私有规则，因此专用查询必须显式接收规则所属租户，
/// 不能只依赖框架的默认多租户过滤器推断作用域。
/// </remarks>
public interface INumberingRuleRepository : ISaasRepository<SysNumberingRule>
{
    /// <summary>
    /// 在明确的规则所属租户中按编码查找规则。
    /// </summary>
    /// <param name="ownerTenantId">规则所属租户；0 表示平台全局规则。</param>
    /// <param name="ruleCode">规则编码。</param>
    /// <param name="enabledOnly">为 <see langword="true"/> 时仅返回启用规则；为 <see langword="false"/> 时包含停用规则。</param>
    /// <param name="cancellationToken">用于取消数据库查询的取消令牌。</param>
    /// <returns>精确作用域内匹配的规则；不存在时返回 <see langword="null"/>。</returns>
    /// <exception cref="ArgumentException"><paramref name="ruleCode"/> 为 <see langword="null"/>、空字符串或仅包含空白字符。</exception>
    /// <exception cref="OperationCanceledException">查询被 <paramref name="cancellationToken"/> 取消。</exception>
    Task<SysNumberingRule?> FindByCodeInScopeAsync(long ownerTenantId, string ruleCode, bool enabledOnly, CancellationToken cancellationToken = default);

    /// <summary>
    /// 在明确的规则所属租户中按主键查找规则。
    /// </summary>
    /// <param name="ownerTenantId">规则所属租户；0 表示平台全局规则。</param>
    /// <param name="id">规则主键。</param>
    /// <param name="cancellationToken">用于取消数据库查询的取消令牌。</param>
    /// <returns>精确作用域内匹配的规则；主键无效或规则不存在时返回 <see langword="null"/>。</returns>
    /// <exception cref="OperationCanceledException">查询被 <paramref name="cancellationToken"/> 取消。</exception>
    Task<SysNumberingRule?> FindByIdInScopeAsync(long ownerTenantId, long id, CancellationToken cancellationToken = default);

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
    Task<bool> TryRollOverPeriodAsync(long ownerTenantId, long ruleId, string periodKey, long periodOrdinal, CancellationToken cancellationToken = default);

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
    Task<bool> TryAdvanceSequenceAsync(long ownerTenantId, long ruleId, string periodKey, int count, long maximum, CancellationToken cancellationToken = default);

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
    Task<NumberingSequenceState?> ReadCurrentValueAsync(long ownerTenantId, long ruleId, CancellationToken cancellationToken = default);

}
