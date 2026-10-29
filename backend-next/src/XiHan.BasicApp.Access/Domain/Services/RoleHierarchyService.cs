// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Access.Domain.Entities;
using XiHan.BasicApp.Access.Infrastructure;
using XiHan.Framework.Domain.Specifications;
using XiHan.Framework.Persistence.Abstractions.Identifiers;
using XiHan.Framework.Persistence.Abstractions.Queries;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Access.Domain.Services;

/// <summary>
/// 角色继承的写入规则。
/// </summary>
/// <remarks>
/// 见方案 5.9，沿用 4.x。
/// 只在同一侧的角色之间；内置角色不参与；停用的角色不能新设为上级（已有的停用上级保留，只是不传下授予）；
/// 新上级不能是本角色的下级（防环），本角色的直接上级之间不能互为祖先（防冗余）。结构校验按库里的边，不按缓存。
/// </remarks>
/// <param name="edges">继承边仓储。</param>
/// <param name="roles">角色查找。</param>
/// <param name="hierarchy">继承图缓存（写入后失效）。</param>
/// <param name="ids">标识生成器。</param>
public sealed class RoleHierarchyService(IRepository<SysRoleInheritance, Guid> edges, RoleService roles, RoleHierarchy hierarchy, IIdGenerator<Guid> ids)
{
    /// <summary>
    /// 读一侧库里的继承图（带启停）。
    /// </summary>
    /// <remarks>
    /// 按本次调用的工作单元读：写入之后的读取看得到未提交的边，也不会把旧图填回缓存。
    /// </remarks>
    public async Task<RoleGraph> GraphOfAsync(string owner, CancellationToken cancellationToken)
    {
        var rows = await ListAsync(owner, cancellationToken).ConfigureAwait(false);
        var ids = rows.SelectMany(static e => new[] { e.RoleId, e.ParentId }).Distinct().ToList();
        var inactive = (await roles.FindManyAsync(ids, owner, cancellationToken).ConfigureAwait(false)).Where(static r => r.Status != RoleStatus.Active).Select(static r => r.Id);
        return new RoleGraph(rows.Select(static e => (e.RoleId, e.ParentId)), inactive);
    }

    /// <summary>
    /// 把角色的直接上级整体设为 <paramref name="parentIds"/>。
    /// </summary>
    /// <returns>新增与解除的上级。</returns>
    public async Task<Result<(IReadOnlyList<SysRole> Added, IReadOnlyList<SysRole> Removed)>> SetParentsAsync(SysRole role, IReadOnlyList<Guid> parentIds, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(role);
        ArgumentNullException.ThrowIfNull(parentIds);
        var owner = role.OwnerTenantId;
        var wanted = parentIds.Distinct().ToList();
        if (wanted.Contains(role.Id))
        {
            return Error.Validation(AccessErrors.RoleSelfParent, AccessMessages.RoleSelfParent);
        }

        var rows = await ListAsync(owner, cancellationToken).ConfigureAwait(false);
        var current = rows.Where(e => e.RoleId == role.Id).ToList();
        var currentIds = current.Select(static e => e.ParentId).ToHashSet();
        var addedIds = wanted.Where(id => !currentIds.Contains(id)).ToList();
        var removedIds = currentIds.Where(id => !wanted.Contains(id)).ToList();
        if (addedIds.Count == 0 && removedIds.Count == 0)
        {
            return (Array.Empty<SysRole>(), Array.Empty<SysRole>());
        }

        if (role.IsBuiltIn)
        {
            return Error.Validation(AccessErrors.InheritanceBuiltIn, AccessMessages.InheritanceBuiltIn, MessageArguments.Of(("name", role.Name)));
        }

        var found = (await roles.FindManyAsync([.. addedIds, .. removedIds], owner, cancellationToken).ConfigureAwait(false)).ToDictionary(static r => r.Id);
        var added = new List<SysRole>(addedIds.Count);
        foreach (var id in addedIds)
        {
            if (!found.TryGetValue(id, out var parent))
            {
                return Error.NotFound(AccessErrors.RoleNotFound, AccessMessages.RoleNotFound, MessageArguments.Of(("id", id)));
            }

            if (parent.IsBuiltIn)
            {
                return Error.Validation(AccessErrors.InheritanceBuiltIn, AccessMessages.InheritanceBuiltIn, MessageArguments.Of(("name", parent.Name)));
            }

            if (parent.Status != RoleStatus.Active)
            {
                return Error.Validation(AccessErrors.ParentDisabled, AccessMessages.ParentDisabled, MessageArguments.Of(("name", parent.Name)));
            }

            added.Add(parent);
        }

        // 换边之后再看结构：新上级在本角色的下级里即成环；直接上级之间互为祖先即冗余（含已有上级被新上级间接覆盖）
        var after = new RoleGraph(
            rows.Where(e => e.RoleId != role.Id).Select(static e => (e.RoleId, e.ParentId)).Concat(wanted.Select(p => (role.Id, p))),
            []);
        var descendants = after.DescendantsOf(role.Id).Select(static d => d.RoleId).ToHashSet();
        foreach (var parent in added)
        {
            if (descendants.Contains(parent.Id))
            {
                return Error.Validation(AccessErrors.InheritanceCycle, AccessMessages.InheritanceCycle, MessageArguments.Of(("name", parent.Name)));
            }
        }

        foreach (var parentId in wanted)
        {
            if (wanted.Any(other => other != parentId && after.AncestorsOf(other).Any(a => a.RoleId == parentId)))
            {
                var name = found.TryGetValue(parentId, out var known) ? known.Name : (await roles.FindAsync(parentId, owner, cancellationToken).ConfigureAwait(false)).TryGetValue(out var existing) ? existing.Name : parentId.ToString();
                return Error.Validation(AccessErrors.InheritanceRedundant, AccessMessages.InheritanceRedundant, MessageArguments.Of(("name", name)));
            }
        }

        var removing = current.Where(e => removedIds.Contains(e.ParentId)).ToList();
        if (removing.Count > 0)
        {
            foreach (var outcome in await edges.RemoveRangeAsync(removing, cancellationToken).ConfigureAwait(false))
            {
                if (outcome.IsFailure)
                {
                    return outcome.Error!;
                }
            }
        }

        if (added.Count > 0)
        {
            var additions = added.Select(p => new SysRoleInheritance { Id = ids.NewId(), OwnerTenantId = owner, RoleId = role.Id, ParentId = p.Id }).ToList();
            foreach (var outcome in await edges.AddRangeAsync(additions, cancellationToken).ConfigureAwait(false))
            {
                if (outcome.IsFailure)
                {
                    return outcome.Error!;
                }
            }
        }

        await hierarchy.InvalidateAsync(owner, cancellationToken).ConfigureAwait(false);
        var removed = removedIds.Select(id => found.TryGetValue(id, out var r) ? r : null).OfType<SysRole>().ToList();
        return (added, removed);
    }

    /// <summary>
    /// 删除角色时去掉它作为下级的边。
    /// </summary>
    /// <returns>它若还是别的角色的上级，返回 <see langword="false"/>（不能删除）。</returns>
    public async Task<Result<bool>> DetachAsync(SysRole role, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(role);
        var rows = await ListAsync(role.OwnerTenantId, cancellationToken).ConfigureAwait(false);
        if (rows.Any(e => e.ParentId == role.Id))
        {
            return false;
        }

        var own = rows.Where(e => e.RoleId == role.Id).ToList();
        if (own.Count > 0)
        {
            foreach (var outcome in await edges.RemoveRangeAsync(own, cancellationToken).ConfigureAwait(false))
            {
                if (outcome.IsFailure)
                {
                    return outcome.Error!;
                }
            }

            await hierarchy.InvalidateAsync(role.OwnerTenantId, cancellationToken).ConfigureAwait(false);
        }

        return true;
    }

    private Task<IReadOnlyList<SysRoleInheritance>> ListAsync(string owner, CancellationToken cancellationToken)
        => edges.ListAsync(EntityQuery.All<SysRoleInheritance>().Where(new Specification<SysRoleInheritance>(e => e.OwnerTenantId == owner)), cancellationToken);
}
