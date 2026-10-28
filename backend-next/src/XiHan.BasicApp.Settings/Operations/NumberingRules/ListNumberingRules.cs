// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Paging;
using XiHan.BasicApp.Settings.Queries;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Settings.Operations.NumberingRules;

/// <summary>
/// 编号规则列表：<c>POST /api/numbering-rules:list</c>。平台主体看平台规则；租户主体看自己的规则与供租户使用的平台规则。
/// </summary>
/// <param name="queries">规则查询。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = SettingsPermissions.NumberingRulesRead)]
public sealed class ListNumberingRules(NumberingRuleQueries queries) : IOperation<PageRequest, PageResult<NumberingRuleResponse>>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "numbering-rules.list";

    /// <inheritdoc />
    public ValueTask<Result<PageResult<NumberingRuleResponse>>> HandleAsync(PageRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        return queries.ListAsync(request, context);
    }
}
