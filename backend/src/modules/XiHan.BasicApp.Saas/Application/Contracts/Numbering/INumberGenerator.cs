// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.Saas.Application.Contracts;

/// <summary>
/// 单号生成请求。
/// </summary>
/// <param name="RuleCode">规则编码。</param>
/// <param name="Scope">规则作用域解析方式。</param>
/// <param name="IdempotencyKey">调用方幂等键。</param>
/// <param name="BusinessType">可选业务类型。</param>
/// <param name="BusinessId">可选业务标识。</param>
public sealed record NumberGenerateRequest(
    string RuleCode,
    NumberingScope Scope,
    string IdempotencyKey,
    string? BusinessType = null,
    string? BusinessId = null);

/// <summary>
/// 批量编号生成请求。
/// </summary>
/// <param name="RuleCode">规则编码。</param>
/// <param name="Scope">规则作用域解析方式。</param>
/// <param name="IdempotencyKey">调用方幂等键。</param>
/// <param name="Count">生成数量，范围为 1 至 1000。</param>
/// <param name="BusinessType">可选业务类型。</param>
/// <param name="BusinessId">可选业务标识。</param>
public sealed record NumberBatchGenerateRequest(
    string RuleCode,
    NumberingScope Scope,
    string IdempotencyKey,
    int Count,
    string? BusinessType = null,
    string? BusinessId = null);

/// <summary>
/// 编号生成结果。
/// </summary>
/// <param name="RuleId">实际规则主键。</param>
/// <param name="RuleCode">实际规则编码。</param>
/// <param name="ResolvedScope">实际解析到的作用域。</param>
/// <param name="RequestTenantId">原始请求租户；平台或单体上下文为 0。</param>
/// <param name="IdempotencyKey">幂等键。</param>
/// <param name="PeriodKey">发号周期键。</param>
/// <param name="StartValue">起始流水值。</param>
/// <param name="EndValue">结束流水值。</param>
/// <param name="Numbers">生成的编号。</param>
/// <param name="GeneratedAtUtc">首次成功分配的 UTC 时间。</param>
/// <param name="IsIdempotentReplay">是否由既有幂等记录重放。</param>
public sealed record NumberGenerationResult(
    long RuleId,
    string RuleCode,
    NumberingScope ResolvedScope,
    long RequestTenantId,
    string IdempotencyKey,
    string PeriodKey,
    long StartValue,
    long EndValue,
    IReadOnlyList<string> Numbers,
    DateTimeOffset GeneratedAtUtc,
    bool IsIdempotentReplay);

/// <summary>
/// 业务编号生成器公共 DI 契约。
/// </summary>
/// <remarks>
/// 调用方应通过构造函数注入本接口。首版不提供静态 Helper 或全局 Service Locator，以保持租户上下文、事务和测试边界明确。
/// 请求模型不接受租户 ID；实现必须从 <c>ICurrentTenant</c> 捕获原请求租户，并在解析到全局规则后切换平台数据库。
/// </remarks>
public interface INumberGenerator
{
    /// <summary>
    /// 生成一个业务编号。
    /// </summary>
    /// <param name="request">单号请求；规则编码和幂等键必填，业务关联字段可为空。</param>
    /// <param name="cancellationToken">用于取消进程内锁等待、数据库查询、事务提交和冲突退避的取消令牌。</param>
    /// <returns>包含唯一编号、实际解析作用域和永久分配审计信息的结果。</returns>
    /// <remarks>
    /// 相同实际规则、请求租户、幂等键和请求指纹会重放首次分配结果；相同幂等键但参数不同会被拒绝。
    /// 成功分配在独立事务中原子更新规则行版本并插入永久分配记录。
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="request"/> 为 <see langword="null"/>。</exception>
    /// <exception cref="XiHan.Framework.Core.Exceptions.UserFriendlyException">规则不可用、幂等冲突、流水耗尽或并发重试耗尽。</exception>
    /// <exception cref="OperationCanceledException">操作被 <paramref name="cancellationToken"/> 取消。</exception>
    Task<NumberGenerationResult> GenerateAsync(NumberGenerateRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// 在同一事务中批量生成连续业务编号。
    /// </summary>
    /// <param name="request">批量请求；数量范围为 1 至 1000，规则编码和幂等键必填。</param>
    /// <param name="cancellationToken">用于取消进程内锁等待、数据库查询、事务提交和冲突退避的取消令牌。</param>
    /// <returns>包含连续编号、实际解析作用域和永久分配审计信息的结果。</returns>
    /// <remarks>
    /// 整批编号共享一个连续流水区间和一条永久分配记录。任何分配步骤失败都会回滚整批流水推进，
    /// 但调用方事务在发号成功后失败仍可能形成允许存在的业务空洞。
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="request"/> 为 <see langword="null"/>。</exception>
    /// <exception cref="XiHan.Framework.Core.Exceptions.UserFriendlyException">请求无效、规则不可用、幂等冲突、流水耗尽或并发重试耗尽。</exception>
    /// <exception cref="OperationCanceledException">操作被 <paramref name="cancellationToken"/> 取消。</exception>
    Task<NumberGenerationResult> GenerateBatchAsync(NumberBatchGenerateRequest request, CancellationToken cancellationToken = default);
}
