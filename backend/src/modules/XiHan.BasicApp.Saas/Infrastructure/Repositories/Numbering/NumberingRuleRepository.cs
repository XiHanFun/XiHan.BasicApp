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
}
