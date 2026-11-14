// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Organization.Domain.Services;
using XiHan.BasicApp.Organization.Infrastructure;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Organization.Operations.Departments;

/// <summary>
/// 部门树
/// </summary>
/// <remarks>
/// <c>GET /api/departments:tree</c>（声明只读，请求没有嵌套对象，按 ADR-0041 走 GET）。只读目录类操作，同时作为 MCP / AI 工具暴露（5.4）。一次取出当前租户全部部门与归属行，在内存里组树并数直接成员（一个租户的部门数以百计，不分页）
/// 带关键字时只留命中的部门（不分大小写，匹配编码、名称、电话、邮箱、地址与说明）：上级没命中的当作顶层，不补上它的祖先
/// 部门与成员数按租户整份缓存（<see cref="DepartmentSnapshots"/>），关键字在缓存之上过滤
/// </remarks>
/// <param name="snapshots">部门缓存</param>
[Operation(Id, Stability = Stability.Preview, ReadOnly = true, Expose = Expose.Http | Expose.Mcp | Expose.Ai, Permission = OrganizationPermissions.DepartmentsRead)]
public sealed class DepartmentTree(DepartmentSnapshots snapshots) : IOperation<DepartmentTreeRequest, DepartmentTreeResponse>
{
    /// <summary>
    /// 操作 ID
    /// </summary>
    public const string Id = "departments.tree";

    /// <inheritdoc />
    public async ValueTask<Result<DepartmentTreeResponse>> HandleAsync(DepartmentTreeRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        if (!OrganizationScope.RequireTenant(context).TryGetValue(out var tenantId))
        {
            return OrganizationScope.RequireTenant(context).Error!;
        }

        var entries = await snapshots.OfAsync(tenantId, context.CancellationToken).ConfigureAwait(false);
        var keyword = request.Keyword?.Trim();
        var all = (string.IsNullOrEmpty(keyword) ? entries : entries.Where(e => Matches(e.Department, keyword))).Select(static e => e.Department).ToList();
        var counts = entries.ToDictionary(static e => e.Department.Id, static e => e.Members);
        var byParent = all.GroupBy(static d => d.ParentId ?? Guid.Empty).ToDictionary(static g => g.Key, static g => g.ToList());
        var known = all.Select(static d => d.Id).ToHashSet();

        // 上级已被删除（或不在本租户、或没被关键字命中）的部门当作顶层展示，不丢行
        List<DepartmentNode> Build(Guid parent)
        {
            if (!byParent.TryGetValue(parent, out var children))
            {
                return [];
            }

            return children
                .OrderBy(static d => d.Order).ThenBy(static d => d.Name, StringComparer.Ordinal)
                .Select(d => new DepartmentNode(d, counts.GetValueOrDefault(d.Id), Build(d.Id)))
                .ToList();
        }

        var roots = Build(Guid.Empty);
        var orphans = all.Where(d => d.ParentId is { } p && !known.Contains(p))
            .OrderBy(static d => d.Order).ThenBy(static d => d.Name, StringComparer.Ordinal)
            .Select(d => new DepartmentNode(d, counts.GetValueOrDefault(d.Id), Build(d.Id)));
        return new DepartmentTreeResponse([.. roots, .. orphans], all.Count);
    }

    private static bool Matches(DepartmentResponse department, string keyword)
    {
        string?[] fields = [department.Code, department.Name, department.Phone, department.Email, department.Address, department.Description];
        return fields.Any(field => field is not null && field.Contains(keyword, StringComparison.OrdinalIgnoreCase));
    }
}
