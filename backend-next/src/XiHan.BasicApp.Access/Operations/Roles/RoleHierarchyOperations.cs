// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using XiHan.BasicApp.Access.Domain.Entities;
using XiHan.BasicApp.Access.Domain.Services;
using XiHan.BasicApp.Access.Infrastructure;
using XiHan.BasicApp.Contracts.Auditing;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Access.Operations.Roles;

/// <summary>
/// <c>roles.set-parents</c> 的请求：把角色的直接上级整体设为给定的一组（空即解除全部继承）。
/// </summary>
/// <param name="Id">角色 ID。</param>
/// <param name="ParentIds">直接上级，最多 50 个。</param>
public sealed record SetRoleParentsRequest([Required] Guid Id, [Required, MaxLength(50)] IReadOnlyList<Guid> ParentIds);

/// <summary>
/// 继承链上的一个角色。
/// </summary>
/// <param name="RoleId">角色 ID。</param>
/// <param name="Code">角色码。</param>
/// <param name="Name">角色名。</param>
/// <param name="Status">状态。</param>
/// <param name="Depth">最短的继承深度（直接为 1）。</param>
/// <param name="Path">最短路径上的角色名，按「上级 → 下级」排列，含两端。</param>
/// <param name="IsEffective">授予此刻是否沿继承传到（路径上作为上级的角色都启用）。</param>
public sealed record RoleRelativeResponse(Guid RoleId, string Code, string Name, RoleStatus Status, int Depth, IReadOnlyList<string> Path, bool IsEffective);

/// <summary>
/// 从此刻传得到授予的上级继承来的一条授予或拒绝；同一权限码来自多个上级时逐条列出。
/// </summary>
/// <param name="Permission">权限码。</param>
/// <param name="Action">授予或拒绝（上级的拒绝同样作用于本角色）。</param>
/// <param name="SourceRoleId">来源上级。</param>
/// <param name="SourceRoleName">来源上级的名称。</param>
/// <param name="Depth">来源上级的深度。</param>
public sealed record InheritedPermissionResponse(string Permission, PermissionAction Action, Guid SourceRoleId, string SourceRoleName, int Depth);

/// <summary>
/// 角色的继承关系：全部上级、全部下级与继承来的授予。
/// </summary>
/// <param name="RoleId">角色 ID。</param>
/// <param name="Ancestors">全部上级（含停用与被截断的），按深度排列。</param>
/// <param name="Descendants">全部下级，按深度排列。</param>
/// <param name="Inherited">继承来的授予与拒绝。</param>
public sealed record RoleHierarchyResponse(Guid RoleId, IReadOnlyList<RoleRelativeResponse> Ancestors, IReadOnlyList<RoleRelativeResponse> Descendants, IReadOnlyList<InheritedPermissionResponse> Inherited);

/// <summary>
/// 角色的继承关系：<c>GET /api/roles:hierarchy?id=</c>。数据范围与字段安全不继承，这里只列授予。
/// </summary>
/// <param name="service">角色查找。</param>
/// <param name="reader">继承关系的组装。</param>
[Operation(Id, Stability = Stability.Preview, ReadOnly = true, Expose = Expose.Http, Permission = AccessPermissions.RolesRead)]
public sealed class GetRoleHierarchy(RoleService service, RoleHierarchyReader reader) : IOperation<GetRoleRequest, RoleHierarchyResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "roles.hierarchy";

    /// <inheritdoc />
    public async ValueTask<Result<RoleHierarchyResponse>> HandleAsync(GetRoleRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        if (!(await service.FindAsync(request.Id, RoleService.OwnerOf(context), context.CancellationToken).ConfigureAwait(false)).TryGetValue(out var role))
        {
            return Error.NotFound(AccessErrors.RoleNotFound, AccessMessages.RoleNotFound, MessageArguments.Of(("id", request.Id)));
        }

        return await reader.ReadAsync(role, context.CancellationToken).ConfigureAwait(false);
    }
}

/// <summary>
/// 设置角色的直接上级：<c>POST /api/roles:set-parents</c>。本角色的持有人随即拿到上级（及其上级）的授予，上级的拒绝同样作用于本角色；
/// 只在同一侧的角色之间，内置角色不参与，停用的角色不能新设为上级，不能成环，直接上级之间不能互为祖先。
/// </summary>
/// <param name="service">角色查找。</param>
/// <param name="hierarchy">继承写入。</param>
/// <param name="reader">继承关系的组装。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = AccessPermissions.RolesSetParents)]
public sealed class SetRoleParents(RoleService service, RoleHierarchyService hierarchy, RoleHierarchyReader reader) : IOperation<SetRoleParentsRequest, RoleHierarchyResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "roles.set-parents";

    /// <inheritdoc />
    public async ValueTask<Result<RoleHierarchyResponse>> HandleAsync(SetRoleParentsRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        if (!(await service.FindAsync(request.Id, RoleService.OwnerOf(context), context.CancellationToken).ConfigureAwait(false)).TryGetValue(out var role))
        {
            return Error.NotFound(AccessErrors.RoleNotFound, AccessMessages.RoleNotFound, MessageArguments.Of(("id", request.Id)));
        }

        var changed = await hierarchy.SetParentsAsync(role, request.ParentIds, context.CancellationToken).ConfigureAwait(false);
        if (!changed.TryGetValue(out var change))
        {
            return changed.Error!;
        }

        if (change.Added.Count > 0 || change.Removed.Count > 0)
        {
            context.AddAuditLog(new PermissionChangeLogEntry(PermissionChangeType.RoleInheritance)
            {
                RoleId = role.Id,
                RoleName = role.Name,
                Detail = $"add:[{string.Join(", ", change.Added.Select(static r => r.Code))}] remove:[{string.Join(", ", change.Removed.Select(static r => r.Code))}]",
            });
        }

        return await reader.ReadAsync(role, context.CancellationToken).ConfigureAwait(false);
    }
}

/// <summary>
/// 组装角色的继承关系（按本次调用的工作单元读继承图，写入之后的响应看得到新边）。
/// </summary>
/// <param name="hierarchy">继承写入与读取。</param>
/// <param name="roles">角色查找。</param>
/// <param name="grants">角色授予。</param>
/// <param name="rules">授予规则。</param>
public sealed class RoleHierarchyReader(RoleHierarchyService hierarchy, RoleService roles, RoleGrants grants, PermissionRuleWriter rules)
{
    /// <summary>
    /// 读一个角色的继承关系。
    /// </summary>
    public async Task<RoleHierarchyResponse> ReadAsync(SysRole role, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(role);
        var graph = await hierarchy.GraphOfAsync(role.OwnerTenantId, cancellationToken).ConfigureAwait(false);
        var ancestors = graph.AncestorsOf(role.Id);
        var descendants = graph.DescendantsOf(role.Id);
        var ids = ancestors.Concat(descendants).SelectMany(static r => r.Path).Append(role.Id).Distinct().ToList();
        var found = (await roles.FindManyAsync(ids, role.OwnerTenantId, cancellationToken).ConfigureAwait(false)).ToDictionary(static r => r.Id);
        found[role.Id] = role;

        var inherited = new List<InheritedPermissionResponse>();
        foreach (var (ancestorId, depth) in graph.EffectiveAncestorsOf(role.Id))
        {
            if (!found.TryGetValue(ancestorId, out var ancestor))
            {
                continue;
            }

            foreach (var permission in await grants.ListAsync(ancestor, cancellationToken).ConfigureAwait(false))
            {
                inherited.Add(new InheritedPermissionResponse(permission, PermissionAction.Grant, ancestor.Id, ancestor.Name, depth));
            }

            foreach (var rule in (await rules.ListAsync(ancestor.OwnerTenantId, GrantSubjectKind.Role, ancestor.Id, cancellationToken).ConfigureAwait(false)).Where(static r => r.Action == PermissionAction.Deny))
            {
                inherited.Add(new InheritedPermissionResponse(rule.Permission, PermissionAction.Deny, ancestor.Id, ancestor.Name, depth));
            }
        }

        return new RoleHierarchyResponse(
            role.Id,
            ancestors.Select(a => Relative(a, found)).OfType<RoleRelativeResponse>().ToList(),
            descendants.Select(d => Relative(d, found)).OfType<RoleRelativeResponse>().ToList(),
            [.. inherited.OrderBy(static i => i.Depth).ThenBy(static i => i.Permission, StringComparer.Ordinal)]);
    }

    private static RoleRelativeResponse? Relative(RoleRelative relative, Dictionary<Guid, SysRole> found)
        => found.TryGetValue(relative.RoleId, out var role)
            ? new RoleRelativeResponse(role.Id, role.Code, role.Name, role.Status, relative.Depth, relative.Path.Select(id => found.TryGetValue(id, out var r) ? r.Name : id.ToString()).ToList(), relative.IsEffective)
            : null;
}
