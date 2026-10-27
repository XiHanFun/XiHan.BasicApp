// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Access.Queries;
using XiHan.BasicApp.Contracts.Paging;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Access.Operations.Roles;

/// <summary>
/// 角色列表：<c>POST /api/roles:list</c>（分页、关键字、筛选、排序），只列当前作用侧。
/// </summary>
/// <param name="queries">角色查询。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = AccessPermissions.RolesRead)]
public sealed class ListRoles(RoleQueries queries) : IOperation<PageRequest, PageResult<RoleResponse>>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "roles.list";

    /// <inheritdoc />
    public ValueTask<Result<PageResult<RoleResponse>>> HandleAsync(PageRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        return queries.ListAsync(request, context);
    }
}
