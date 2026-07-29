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

    /// <inheritdoc />
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

    /// <inheritdoc />
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

    /// <inheritdoc />
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
