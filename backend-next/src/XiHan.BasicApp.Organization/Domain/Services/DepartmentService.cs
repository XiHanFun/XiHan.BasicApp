// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Organization.Domain.Entities;
using XiHan.Framework.Domain.Specifications;
using XiHan.Framework.Persistence.Abstractions.Identifiers;
using XiHan.Framework.Persistence.Abstractions.Queries;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Organization.Domain.Services;

/// <summary>
/// 部门域服务（作用域）：编码唯一、闭包表维护（挂载、移动、摘除）、子孙与使用情况查询。仓储按当前租户严格过滤，所以这里不再带租户条件。
/// </summary>
/// <param name="departments">部门仓储。</param>
/// <param name="hierarchy">闭包表仓储。</param>
/// <param name="memberships">归属仓储。</param>
/// <param name="ids">标识生成器。</param>
public sealed class DepartmentService(
    IRepository<SysDepartment, Guid> departments,
    IRepository<SysDepartmentHierarchy, Guid> hierarchy,
    IRepository<SysUserDepartment, Guid> memberships,
    IIdGenerator<Guid> ids)
{
    /// <summary>
    /// 按主键找部门（当前租户）。
    /// </summary>
    public ValueTask<Maybe<SysDepartment>> FindAsync(Guid id, CancellationToken cancellationToken) => departments.FindAsync(id, cancellationToken);

    /// <summary>
    /// 编码在当前租户内是否已被占用。
    /// </summary>
    public async Task<bool> IsCodeTakenAsync(string code, CancellationToken cancellationToken)
        => await departments.CountAsync(EntityQuery.All<SysDepartment>().Where(new Specification<SysDepartment>(d => d.Code == code)), cancellationToken).ConfigureAwait(false) > 0;

    /// <summary>
    /// 挂载到树：写自身（深度 0）与上级的全部祖先（深度 +1）的闭包行。
    /// </summary>
    public async Task<Result> AttachAsync(SysDepartment department, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(department);
        var rows = new List<SysDepartmentHierarchy> { new() { Id = ids.NewId(), AncestorId = department.Id, DescendantId = department.Id, Depth = 0 } };
        if (department.ParentId is { } parentId)
        {
            foreach (var ancestor in await AncestorRowsAsync(parentId, cancellationToken).ConfigureAwait(false))
            {
                rows.Add(new SysDepartmentHierarchy { Id = ids.NewId(), AncestorId = ancestor.AncestorId, DescendantId = department.Id, Depth = ancestor.Depth + 1 });
            }
        }

        return Collect(await hierarchy.AddRangeAsync(rows, cancellationToken).ConfigureAwait(false));
    }

    /// <summary>
    /// 移动到新上级：拒绝移到自己或子孙之下；摘掉子树与树外祖先的闭包行，再按新上级的祖先重算整棵子树。
    /// </summary>
    public async Task<Result> MoveAsync(SysDepartment department, Guid? newParentId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(department);
        var departmentId = department.Id;
        var subtree = await hierarchy.ListAsync(EntityQuery.All<SysDepartmentHierarchy>().Where(new Specification<SysDepartmentHierarchy>(h => h.AncestorId == departmentId)), cancellationToken).ConfigureAwait(false);
        var subtreeIds = subtree.Select(static h => h.DescendantId).ToHashSet();
        if (newParentId is { } target && subtreeIds.Contains(target))
        {
            return Error.Validation(OrganizationErrors.CyclicMove, OrganizationMessages.CyclicMove, MessageArguments.Of(("id", departmentId)));
        }

        var subtreeList = subtreeIds.ToList();
        var stale = (await hierarchy.ListAsync(EntityQuery.All<SysDepartmentHierarchy>().Where(new Specification<SysDepartmentHierarchy>(h => subtreeList.Contains(h.DescendantId))), cancellationToken).ConfigureAwait(false))
            .Where(h => !subtreeIds.Contains(h.AncestorId))
            .ToList();
        if (stale.Count > 0)
        {
            var removed = Collect(await hierarchy.RemoveRangeAsync(stale, cancellationToken).ConfigureAwait(false));
            if (removed.IsFailure)
            {
                return removed;
            }
        }

        if (newParentId is { } parentId)
        {
            var ancestors = await AncestorRowsAsync(parentId, cancellationToken).ConfigureAwait(false);
            var rows = new List<SysDepartmentHierarchy>();
            foreach (var node in subtree)
            {
                foreach (var ancestor in ancestors)
                {
                    rows.Add(new SysDepartmentHierarchy { Id = ids.NewId(), AncestorId = ancestor.AncestorId, DescendantId = node.DescendantId, Depth = ancestor.Depth + 1 + node.Depth });
                }
            }

            if (rows.Count > 0)
            {
                var added = Collect(await hierarchy.AddRangeAsync(rows, cancellationToken).ConfigureAwait(false));
                if (added.IsFailure)
                {
                    return added;
                }
            }
        }

        department.ParentId = newParentId;
        return await departments.UpdateAsync(department, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// 摘除：软删部门并硬删它作为祖先或子孙的全部闭包行（调用前已确认没有子部门与成员）。
    /// </summary>
    public async Task<Result> DetachAsync(SysDepartment department, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(department);
        var departmentId = department.Id;
        var rows = await hierarchy.ListAsync(EntityQuery.All<SysDepartmentHierarchy>().Where(new Specification<SysDepartmentHierarchy>(h => h.AncestorId == departmentId || h.DescendantId == departmentId)), cancellationToken).ConfigureAwait(false);
        if (rows.Count > 0)
        {
            var removed = Collect(await hierarchy.RemoveRangeAsync(rows, cancellationToken).ConfigureAwait(false));
            if (removed.IsFailure)
            {
                return removed;
            }
        }

        return await departments.RemoveAsync(department, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// 直接子部门数。
    /// </summary>
    public Task<long> ChildCountAsync(Guid departmentId, CancellationToken cancellationToken)
        => hierarchy.CountAsync(EntityQuery.All<SysDepartmentHierarchy>().Where(new Specification<SysDepartmentHierarchy>(h => h.AncestorId == departmentId && h.Depth == 1)), cancellationToken);

    /// <summary>
    /// 成员数。
    /// </summary>
    public Task<long> MemberCountAsync(Guid departmentId, CancellationToken cancellationToken)
        => memberships.CountAsync(EntityQuery.All<SysUserDepartment>().Where(new Specification<SysUserDepartment>(m => m.DepartmentId == departmentId)), cancellationToken);

    /// <summary>
    /// 祖先链（不含自己），由近到远。
    /// </summary>
    public async Task<IReadOnlyList<SysDepartment>> AncestorsAsync(Guid departmentId, CancellationToken cancellationToken)
    {
        var rows = (await AncestorRowsAsync(departmentId, cancellationToken).ConfigureAwait(false)).Where(static h => h.Depth > 0).OrderBy(static h => h.Depth).ToList();
        if (rows.Count == 0)
        {
            return [];
        }

        var found = (await departments.FindManyAsync(rows.Select(static h => h.AncestorId).ToList(), cancellationToken).ConfigureAwait(false)).ToDictionary(static d => d.Id);
        return rows.Select(h => found.GetValueOrDefault(h.AncestorId)).Where(static d => d is not null).Select(static d => d!).ToList();
    }

    private Task<IReadOnlyList<SysDepartmentHierarchy>> AncestorRowsAsync(Guid departmentId, CancellationToken cancellationToken)
        => hierarchy.ListAsync(EntityQuery.All<SysDepartmentHierarchy>().Where(new Specification<SysDepartmentHierarchy>(h => h.DescendantId == departmentId)), cancellationToken);

    private static Result Collect(IReadOnlyList<Result> outcomes)
    {
        foreach (var outcome in outcomes)
        {
            if (outcome.IsFailure)
            {
                return outcome;
            }
        }

        return Result.Success();
    }
}
