// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Settings;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Settings.Operations.NumberingRules;

/// <summary>
/// 按编号规则发号。
/// </summary>
/// <remarks>
/// <c>POST /api/numbering-rules:generate</c>。经 <see cref="INumberGenerator"/> 在独立事务里提交，号永久消耗；同一幂等键参数相同即重放（<c>isReplay</c>），不同即 <c>SETTINGS_CONFLICT_013</c>。
/// 规则停用 <c>SETTINGS_CONFLICT_010</c>，流水用尽 <c>SETTINGS_CONFLICT_011</c>，时钟落后 <c>SETTINGS_CONFLICT_012</c>，平台上下文用租户范围 <c>SETTINGS_VALIDATION_009</c>。
/// </remarks>
/// <param name="generator">发号器。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = SettingsPermissions.NumberingRulesGenerate)]
public sealed class GenerateNumbers(INumberGenerator generator) : IOperation<GenerateNumbersRequest, NumberAllocation>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "numbering-rules.generate";

    /// <inheritdoc />
    public ValueTask<Result<NumberAllocation>> HandleAsync(GenerateNumbersRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        return generator.GenerateAsync(new NumberRequest(request.Code.Trim(), request.IdempotencyKey.Trim(), request.Count, request.Scope, request.BusinessType, request.BusinessId), context);
    }
}
