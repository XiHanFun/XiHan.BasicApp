// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Data.SqlSugar.Clients;

namespace XiHan.BasicApp.Saas.Infrastructure.Repositories;

/// <summary>
/// 业务编号永久分配记录 SqlSugar 仓储实现。
/// </summary>
/// <param name="clientResolver">SqlSugar 客户端解析器，根据当前租户上下文选择保存永久分配事实的目标数据库连接。</param>
/// <remarks>
/// 分配记录只追加、不删除，既用于幂等重放，也用于安全重置的防重复判断。
/// 查询始终显式包含规则所属租户，确保平台全局序列和租户私有序列不会共享错误的审计范围。
/// </remarks>
public sealed class NumberingAllocationRepository(ISqlSugarClientResolver clientResolver)
    : SaasRepository<SysNumberingAllocation>(clientResolver), INumberingAllocationRepository
{
    /// <summary>
    /// 按完整幂等唯一范围查询永久分配记录。
    /// </summary>
    /// <param name="ownerTenantId">规则所属租户；全局规则为 0。</param>
    /// <param name="ruleId">实际规则主键。</param>
    /// <param name="requestTenantId">原请求租户；平台或单体上下文为 0。</param>
    /// <param name="idempotencyKey">调用方幂等键。</param>
    /// <param name="cancellationToken">用于取消 SqlSugar 查询的取消令牌。</param>
    /// <returns>唯一匹配的分配记录；不存在时返回 <see langword="null"/>。</returns>
    /// <remarks>请求租户必须参与过滤，因为多个租户可以共享同一个全局规则和相同文本的幂等键。</remarks>
    /// <exception cref="ArgumentException"><paramref name="idempotencyKey"/> 为空或仅包含空白字符。</exception>
    /// <exception cref="OperationCanceledException">查询被 <paramref name="cancellationToken"/> 取消。</exception>
    public async Task<SysNumberingAllocation?> FindByIdempotencyKeyAsync(
        long ownerTenantId,
        long ruleId,
        long requestTenantId,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(idempotencyKey);
        cancellationToken.ThrowIfCancellationRequested();

        // 过滤字段与数据库唯一索引保持一致，查询和并发插入才能表达同一个幂等边界。
        return await CreateQueryable()
            .Where(allocation => allocation.TenantId == ownerTenantId
                && allocation.RuleId == ruleId
                && allocation.RequestTenantId == requestTenantId
                && allocation.IdempotencyKey == idempotencyKey)
            .FirstAsync(cancellationToken);
    }

    /// <summary>
    /// 查询指定规则和周期已经永久分配的最大结束流水值。
    /// </summary>
    /// <param name="ownerTenantId">规则所属租户；全局规则为 0。</param>
    /// <param name="ruleId">实际规则主键。</param>
    /// <param name="periodKey">规则时区下计算的周期键。</param>
    /// <param name="cancellationToken">用于取消 SqlSugar 聚合查询的取消令牌。</param>
    /// <returns>最大结束流水值；当前周期没有分配记录时返回 0。</returns>
    /// <remarks>使用数据库聚合而不是加载全部记录，避免安全重置随历史数据量增长产生线性内存开销。</remarks>
    /// <exception cref="ArgumentException"><paramref name="periodKey"/> 为空或仅包含空白字符。</exception>
    /// <exception cref="OperationCanceledException">查询被 <paramref name="cancellationToken"/> 取消。</exception>
    public async Task<long> GetMaximumEndValueAsync(
        long ownerTenantId,
        long ruleId,
        string periodKey,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(periodKey);
        cancellationToken.ThrowIfCancellationRequested();

        var maximum = await CreateQueryable()
            .Where(allocation => allocation.TenantId == ownerTenantId
                && allocation.RuleId == ruleId
                && allocation.PeriodKey == periodKey)
            .MaxAsync(allocation => (long?)allocation.EndValue, cancellationToken);
        return maximum ?? 0L;
    }
}
