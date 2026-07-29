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
}
