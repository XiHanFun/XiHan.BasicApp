// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Settings.Domain.Services;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Settings.Operations.NumberingRules;

/// <summary>
/// 编号规则详情。
/// </summary>
/// <remarks>
/// <c>GET /api/numbering-rules/{id}</c>。自己的规则；租户另能看供租户使用的平台规则。
/// </remarks>
/// <param name="service">规则域服务。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = SettingsPermissions.NumberingRulesRead)]
public sealed class GetNumberingRule(NumberingRuleService service) : IOperation<GetNumberingRuleRequest, NumberingRuleResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "numbering-rules.get";

    /// <inheritdoc />
    public async ValueTask<Result<NumberingRuleResponse>> HandleAsync(GetNumberingRuleRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var found = await service.FindVisibleAsync(request.Id, SettingsScope.OwnerOf(context), context.CancellationToken).ConfigureAwait(false);
        return found.TryGetValue(out var rule)
            ? NumberingRuleResponse.From(rule)
            : Error.NotFound(SettingsErrors.NumberingRuleNotFound, SettingsMessages.NumberingRuleNotFound, MessageArguments.Of(("id", request.Id)));
    }
}
