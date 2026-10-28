// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Auditing.Queries;
using XiHan.BasicApp.Contracts.Paging;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Auditing.Operations.ConditionHits;

/// <summary>
/// 条件命中记录列表：<c>POST /api/condition-hits:list</c>。只看当前一侧（平台主体看平台的，租户主体看本租户的）。
/// </summary>
/// <param name="queries">条件命中记录查询。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = AuditingPermissions.ConditionHitsRead)]
public sealed class ListConditionHits(ConditionHitQueries queries) : IOperation<PageRequest, PageResult<ConditionHitResponse>>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "condition-hits.list";

    /// <inheritdoc />
    public ValueTask<Result<PageResult<ConditionHitResponse>>> HandleAsync(PageRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        return queries.ListAsync(request, context);
    }
}
