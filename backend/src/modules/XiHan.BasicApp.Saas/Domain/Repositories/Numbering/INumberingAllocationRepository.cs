// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Domain.Repositories;

/// <summary>
/// 业务编号分配记录仓储契约。
/// </summary>
/// <remarks>
/// 分配记录为永久审计数据。幂等查询同时包含规则所属租户、实际规则、原请求租户和幂等键，
/// 以保证多个租户共享全局序列时仍具有彼此隔离的幂等命名空间。
/// </remarks>
public interface INumberingAllocationRepository : ISaasRepository<SysNumberingAllocation>
{
    /// <summary>
    /// 查询指定规则、请求租户和幂等键对应的永久分配记录。
    /// </summary>
    /// <param name="ownerTenantId">规则所属租户。</param>
    /// <param name="ruleId">规则主键。</param>
    /// <param name="requestTenantId">原始请求租户。</param>
    /// <param name="idempotencyKey">幂等键。</param>
    /// <param name="cancellationToken">用于取消数据库查询的取消令牌。</param>
    /// <returns>完整幂等范围内唯一的分配记录；不存在时返回 <see langword="null"/>。</returns>
    /// <exception cref="ArgumentException"><paramref name="idempotencyKey"/> 为 <see langword="null"/>、空字符串或仅包含空白字符。</exception>
    /// <exception cref="OperationCanceledException">查询被 <paramref name="cancellationToken"/> 取消。</exception>
    Task<SysNumberingAllocation?> FindByIdempotencyKeyAsync(
        long ownerTenantId,
        long ruleId,
        long requestTenantId,
        string idempotencyKey,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 取得规则在指定周期内已实际分配的最大流水值。
    /// </summary>
    /// <param name="ownerTenantId">规则所属租户。</param>
    /// <param name="ruleId">规则主键。</param>
    /// <param name="periodKey">周期键。</param>
    /// <param name="cancellationToken">用于取消聚合查询的取消令牌。</param>
    /// <returns>指定周期内永久记录的最大结束流水值；没有记录时返回 0。</returns>
    /// <remarks>安全重置使用该值判断回退是否可能与历史编号重复，不能用规则当前值替代。</remarks>
    /// <exception cref="ArgumentException"><paramref name="periodKey"/> 为 <see langword="null"/>、空字符串或仅包含空白字符。</exception>
    /// <exception cref="OperationCanceledException">查询被 <paramref name="cancellationToken"/> 取消。</exception>
    Task<long> GetMaximumEndValueAsync(long ownerTenantId, long ruleId, string periodKey, CancellationToken cancellationToken = default);
}
