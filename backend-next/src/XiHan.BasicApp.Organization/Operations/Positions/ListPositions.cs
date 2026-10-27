// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Paging;
using XiHan.BasicApp.Organization.Domain.Services;
using XiHan.BasicApp.Organization.Queries;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Organization.Operations.Positions;

/// <summary>
/// 岗位列表：<c>POST /api/positions:list</c>（分页、关键字、筛选、排序）。
/// </summary>
/// <param name="queries">岗位查询。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = OrganizationPermissions.PositionsRead)]
public sealed class ListPositions(PositionQueries queries) : IOperation<PageRequest, PageResult<PositionResponse>>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "positions.list";

    /// <inheritdoc />
    public ValueTask<Result<PageResult<PositionResponse>>> HandleAsync(PageRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var scope = OrganizationScope.RequireTenant(context);
        return scope.IsFailure ? ValueTask.FromResult(Result.Failure<PageResult<PositionResponse>>(scope.Error!)) : queries.ListAsync(request, context);
    }
}
