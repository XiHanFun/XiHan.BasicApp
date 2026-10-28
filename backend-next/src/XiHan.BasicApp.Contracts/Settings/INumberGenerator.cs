// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Contracts.Settings;

/// <summary>
/// 业务编号发号器（Settings 包实现）：在独立事务里发号并提交，发出的号永久消耗——外层调用失败也不回收，可以有空洞，不会重用。
/// 同一侧对同一规则用同一个幂等键只发一次：参数相同即重放原结果，参数不同即冲突。
/// </summary>
public interface INumberGenerator
{
    /// <summary>
    /// 发一个或一批连续的号。
    /// </summary>
    /// <param name="request">发号请求。</param>
    /// <param name="context">调用上下文（决定当前租户与时钟取值）。</param>
    ValueTask<Result<NumberAllocation>> GenerateAsync(NumberRequest request, InvocationContext context);
}

/// <summary>
/// 发号请求。
/// </summary>
/// <param name="RuleCode">规则编码。</param>
/// <param name="IdempotencyKey">幂等键。</param>
/// <param name="Count">个数（1 到 1000）。</param>
/// <param name="Scope">找规则的范围。</param>
/// <param name="BusinessType">业务类型（记进发号记录）。</param>
/// <param name="BusinessId">业务单据主键（记进发号记录）。</param>
public sealed record NumberRequest(
    string RuleCode,
    string IdempotencyKey,
    int Count = 1,
    NumberingScope Scope = NumberingScope.Auto,
    string? BusinessType = null,
    string? BusinessId = null);

/// <summary>
/// 发号结果。
/// </summary>
/// <param name="RuleId">实际使用的规则。</param>
/// <param name="RuleCode">规则编码。</param>
/// <param name="Scope">规则所在的一侧（<see cref="NumberingScope.Tenant"/> 或 <see cref="NumberingScope.Platform"/>）。</param>
/// <param name="RequestTenantId">发起调用的一侧：租户 ID，平台为 <c>platform</c>。</param>
/// <param name="IdempotencyKey">幂等键。</param>
/// <param name="PeriodKey">周期键。</param>
/// <param name="StartValue">起始流水值（含）。</param>
/// <param name="EndValue">结束流水值（含）。</param>
/// <param name="Numbers">号码，按流水升序。</param>
/// <param name="GeneratedAt">发号时刻（UTC）。</param>
/// <param name="IsReplay">是否为同一幂等键的重放。</param>
public sealed record NumberAllocation(
    Guid RuleId,
    string RuleCode,
    NumberingScope Scope,
    string RequestTenantId,
    string IdempotencyKey,
    string PeriodKey,
    long StartValue,
    long EndValue,
    IReadOnlyList<string> Numbers,
    DateTimeOffset GeneratedAt,
    bool IsReplay);
