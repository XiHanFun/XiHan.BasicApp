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
/// <param name="GeneratedTime">首次成功分配的 UTC 时间。</param>
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
    DateTimeOffset GeneratedTime,
    bool IsIdempotentReplay);

/// <summary>
/// 业务编号生成器公共 DI 契约。
/// </summary>
/// <remarks>
/// <para>
/// 调用方应通过构造函数注入本接口。不提供静态 Helper 或全局 Service Locator，以保持租户上下文、事务和测试边界明确。
/// 请求模型不接受租户 ID；实现必须从 <c>ICurrentTenant</c> 捕获原请求租户，并在解析到全局规则后切换平台数据库。
/// </para>
/// <para>
/// <b>事务语义</b>：发号必须在数据库事务内完成。调用链上已有事务型工作单元时，发号加入该事务；
/// 没有时发号自行开启并提交事务。由此产生三条调用方义务：
/// </para>
/// <list type="number">
///   <item>
///     <b>编号与调用方事务同生共死。</b>在自己的事务里取号后回滚，该号会被回收并可能发给另一个业务实体。
///     如果编号在事务提交前就已外泄（写日志、打印、下发给外部系统），请改为在开启业务事务之前取号。
///   </item>
///   <item>
///     <b>规则行会被锁定到调用方事务提交为止。</b>同一条规则的并发发号在数据库行锁上排队，
///     因此调用方事务越长，该规则的整体吞吐越低。请把发号放在业务事务中尽量靠近提交的位置，
///     不要在取号之后再调用外部接口。
///   </item>
///   <item>
///     <b>不要在已经修改过同一条规则行的事务里发号。</b>那会让发号语句等待调用方自己尚未提交的行锁。
///   </item>
/// </list>
/// <para>
/// <b>编号空洞是预期行为而非异常</b>。以下情形都会产生空洞：调用方事务回滚、推进成功后写入分配记录失败、
/// 相同幂等键并发竞争落败、管理员安全重置时主动跳号。编号保证唯一与单调分配，不保证连续。
/// </para>
/// <para>
/// <b>相同幂等键并发</b>：幂等预检是无锁查询，真正的幂等边界是分配记录的唯一索引。
/// 两个并发请求携带同一幂等键时，落败方会因唯一索引冲突抛出数据库异常并回滚，
/// 由调用方以相同幂等键重试即可读到胜出方的结果。这条路径不做就地恢复，
/// 因为 PostgreSQL 在语句失败后会把事务置为中止状态，事务内无法再执行任何查询。
/// </para>
/// <para>
/// <b>库隔离租户调用平台全局规则</b>：全局规则行位于平台库，租户业务数据位于租户库，
/// 因此在租户自己的事务内取全局编号会让一个工作单元跨两条连接、两个事务顺序提交，框架不提供两阶段提交。
/// 若第二条事务提交失败，先提交的一方无法回滚。需要严格保证编号唯一性的库隔离租户，
/// 应改用租户私有规则，或在开启业务事务之前取号。共享库（行隔离）部署不受此限制。
/// </para>
/// <para>
/// 处于非事务型工作单元内时，发号既无法加入事务也无法自行开启事务，
/// 此时实现会立即抛出 <see cref="InvalidOperationException"/> 而不是静默退化——静默退化会发出重复编号。
/// </para>
/// </remarks>
public interface INumberGenerator
{
    /// <summary>
    /// 生成一个业务编号。
    /// </summary>
    /// <param name="request">单号请求；规则编码和幂等键必填，业务关联字段可为空。</param>
    /// <param name="cancellationToken">用于取消数据库查询和事务提交的取消令牌。</param>
    /// <returns>包含唯一编号、实际解析作用域和永久分配审计信息的结果。</returns>
    /// <remarks>
    /// 相同实际规则、请求租户、幂等键和请求指纹会重放首次分配结果；相同幂等键但参数不同会被拒绝。
    /// 成功分配在同一事务内推进规则流水并插入永久分配记录，两者要么一起可见要么一起消失。
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="request"/> 为 <see langword="null"/>。</exception>
    /// <exception cref="XiHan.Framework.Core.Exceptions.UserFriendlyException">规则不可用、幂等冲突、流水耗尽或节点时钟偏差导致周期回退。</exception>
    /// <exception cref="OperationCanceledException">操作被 <paramref name="cancellationToken"/> 取消。</exception>
    Task<NumberGenerationResult> GenerateAsync(NumberGenerateRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// 在同一事务中批量生成连续业务编号。
    /// </summary>
    /// <param name="request">批量请求；数量范围为 1 至 1000，规则编码和幂等键必填。</param>
    /// <param name="cancellationToken">用于取消数据库查询和事务提交的取消令牌。</param>
    /// <returns>包含连续编号、实际解析作用域和永久分配审计信息的结果。</returns>
    /// <remarks>
    /// 整批编号共享一个连续流水区间和一条永久分配记录。任何分配步骤失败都会回滚整批流水推进，
    /// 但调用方事务在发号成功后失败仍可能形成允许存在的业务空洞。
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="request"/> 为 <see langword="null"/>。</exception>
    /// <exception cref="XiHan.Framework.Core.Exceptions.UserFriendlyException">请求无效、规则不可用、幂等冲突、流水耗尽或节点时钟偏差导致周期回退。</exception>
    /// <exception cref="OperationCanceledException">操作被 <paramref name="cancellationToken"/> 取消。</exception>
    Task<NumberGenerationResult> GenerateBatchAsync(NumberBatchGenerateRequest request, CancellationToken cancellationToken = default);
}
