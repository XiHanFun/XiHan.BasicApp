// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Paging;
using XiHan.BasicApp.Settings.Domain.Services;
using XiHan.BasicApp.Settings.Queries;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Settings.Operations.NumberingRules;

/// <summary>
/// 规则的发号记录：<c>POST /api/numbering-rules/{id}:allocations</c>（声明只读，请求带分页对象仍走 POST）。按发号时刻倒序；
/// 平台看全部，租户看自己规则的全部、供租户使用的平台规则只看自己发起的。
/// </summary>
/// <param name="service">规则域服务。</param>
/// <param name="queries">发号记录查询。</param>
[Operation(Id, Stability = Stability.Preview, ReadOnly = true, Expose = Expose.Http, Permission = SettingsPermissions.NumberingRulesRead)]
public sealed class ListNumberingAllocations(NumberingRuleService service, NumberingAllocationQueries queries) : IOperation<NumberingAllocationsRequest, PageResult<NumberingAllocationResponse>>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "numbering-rules.allocations";

    /// <inheritdoc />
    public async ValueTask<Result<PageResult<NumberingAllocationResponse>>> HandleAsync(NumberingAllocationsRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var owner = SettingsScope.OwnerOf(context);
        if (!(await service.FindVisibleAsync(request.Id, owner, context.CancellationToken).ConfigureAwait(false)).TryGetValue(out var rule))
        {
            return Error.NotFound(SettingsErrors.NumberingRuleNotFound, SettingsMessages.NumberingRuleNotFound, MessageArguments.Of(("id", request.Id)));
        }

        var requester = SettingsScope.IsOwnedBy(rule, owner) ? null : owner;
        return await queries.ListAsync(rule.Id, requester, request.Keyword, request.Page ?? PageInfo.Default, context).ConfigureAwait(false);
    }
}
