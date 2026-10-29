// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Auditing.Queries;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Auditing.Operations.ConditionHits;

/// <summary>
/// 条件命中记录详情。
/// </summary>
/// <remarks>
/// <c>GET /api/condition-hits/{id}</c>。不在当前一侧即找不到。
/// </remarks>
/// <param name="queries">条件命中记录查询。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = AuditingPermissions.ConditionHitsRead)]
public sealed class GetConditionHit(ConditionHitQueries queries) : IOperation<GetConditionHitRequest, ConditionHitResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "condition-hits.get";

    /// <inheritdoc />
    public ValueTask<Result<ConditionHitResponse>> HandleAsync(GetConditionHitRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        return queries.GetAsync(request.Id, context);
    }
}
