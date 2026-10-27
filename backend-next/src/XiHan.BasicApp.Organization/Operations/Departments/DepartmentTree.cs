// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Organization.Domain.Entities;
using XiHan.BasicApp.Organization.Domain.Services;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Persistence.Abstractions.Queries;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Organization.Operations.Departments;

/// <summary>
/// 部门树：<c>POST /api/departments:tree</c>。一次取出当前租户全部部门与归属行，在内存里组树并数直接成员（一个租户的部门数以百计，不分页）。
/// </summary>
/// <param name="departments">部门仓储。</param>
/// <param name="memberships">归属仓储。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = OrganizationPermissions.DepartmentsRead)]
public sealed class DepartmentTree(IRepository<SysDepartment, Guid> departments, IRepository<SysUserDepartment, Guid> memberships) : IOperation<DepartmentTreeRequest, DepartmentTreeResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "departments.tree";

    /// <inheritdoc />
    public async ValueTask<Result<DepartmentTreeResponse>> HandleAsync(DepartmentTreeRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var scope = OrganizationScope.RequireTenant(context);
        if (scope.IsFailure)
        {
            return scope.Error!;
        }

        var all = await departments.ListAsync(EntityQuery.All<SysDepartment>().OrderBy(static d => d.Order).OrderBy(static d => d.Name), context.CancellationToken).ConfigureAwait(false);
        var counts = (await memberships.ListAsync(EntityQuery.All<SysUserDepartment>(), context.CancellationToken).ConfigureAwait(false))
            .GroupBy(static m => m.DepartmentId)
            .ToDictionary(static g => g.Key, static g => (long)g.Count());
        var byParent = all.GroupBy(static d => d.ParentId ?? Guid.Empty).ToDictionary(static g => g.Key, static g => g.ToList());
        var known = all.Select(static d => d.Id).ToHashSet();

        // 上级已被删除（或不在本租户）的部门当作顶层展示，不丢行
        List<DepartmentNode> Build(Guid parent)
        {
            if (!byParent.TryGetValue(parent, out var children))
            {
                return [];
            }

            return children
                .OrderBy(static d => d.Order).ThenBy(static d => d.Name, StringComparer.Ordinal)
                .Select(d => new DepartmentNode(DepartmentResponse.From(d), counts.GetValueOrDefault(d.Id), Build(d.Id)))
                .ToList();
        }

        var roots = Build(Guid.Empty);
        var orphans = all.Where(d => d.ParentId is { } p && !known.Contains(p))
            .OrderBy(static d => d.Order).ThenBy(static d => d.Name, StringComparer.Ordinal)
            .Select(d => new DepartmentNode(DepartmentResponse.From(d), counts.GetValueOrDefault(d.Id), Build(d.Id)));
        return new DepartmentTreeResponse([.. roots, .. orphans], all.Count);
    }
}
