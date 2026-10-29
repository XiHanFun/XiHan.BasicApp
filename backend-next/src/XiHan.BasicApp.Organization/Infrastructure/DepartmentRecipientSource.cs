// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Users;
using XiHan.BasicApp.Organization.Domain.Entities;
using XiHan.BasicApp.Runtime.Queries;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Organization.Infrastructure;

/// <summary>
/// 按部门解析接收人（<see cref="RecipientKind.Department"/>）。
/// </summary>
/// <remarks>
/// 这些部门及其全部下级部门的成员（经闭包表展开，闭包表含部门自身；4.x 只取直属成员）。
/// 部门是租户库实体，平台一侧的调用看不到任何部门，解析为空。
/// </remarks>
public sealed class DepartmentRecipientSource : IRecipientSource
{
    /// <inheritdoc />
    public RecipientKind Kind => RecipientKind.Department;

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<Guid>> ResolveAsync(InvocationContext context, IReadOnlyCollection<Guid> targetIds)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(targetIds);
        var ids = targetIds.Distinct().ToList();
        if (ids.Count == 0)
        {
            return [];
        }

        var departments = await TenantScopedQuery.For<SysDepartmentHierarchy>(context)
            .Where(h => ids.Contains(h.AncestorId))
            .Select(static h => h.DescendantId)
            .ToListAsync(context.CancellationToken).ConfigureAwait(false);
        if (departments.Count == 0)
        {
            return [];
        }

        var members = await TenantScopedQuery.For<SysUserDepartment>(context)
            .Where(m => departments.Contains(m.DepartmentId))
            .Select(static m => m.UserId)
            .ToListAsync(context.CancellationToken).ConfigureAwait(false);
        return members.ToHashSet();
    }
}
