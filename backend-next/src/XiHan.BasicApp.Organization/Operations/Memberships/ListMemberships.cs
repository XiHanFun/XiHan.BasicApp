// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Paging;
using XiHan.BasicApp.Organization.Domain.Services;
using XiHan.BasicApp.Organization.Queries;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Organization.Operations.Memberships;

/// <summary>
/// 归属列表：<c>POST /api/memberships:list</c>（筛选 <c>departmentId</c> / <c>userId</c> / <c>positionId</c>，排序 <c>order</c> / <c>createdAt</c>；关键字不支持——用户名在平台库，分库时不能联表）。
/// 只读目录类操作，同时作为 MCP / AI 工具暴露（5.4）。
/// </summary>
/// <param name="queries">归属查询。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http | Expose.Mcp | Expose.Ai, Permission = OrganizationPermissions.MembershipsRead)]
public sealed class ListMemberships(MembershipQueries queries) : IOperation<PageRequest, PageResult<MembershipResponse>>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "memberships.list";

    /// <inheritdoc />
    public ValueTask<Result<PageResult<MembershipResponse>>> HandleAsync(PageRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var scope = OrganizationScope.RequireTenant(context);
        return scope.IsFailure ? ValueTask.FromResult(Result.Failure<PageResult<MembershipResponse>>(scope.Error!)) : queries.ListAsync(request, context);
    }
}
