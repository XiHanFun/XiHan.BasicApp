// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Access.Domain.Entities;
using XiHan.BasicApp.Access.Operations.Roles;
using XiHan.BasicApp.Organization.Domain.Entities;
using XiHan.Framework.Domain.Specifications;
using XiHan.Framework.Persistence.Abstractions.Identifiers;
using XiHan.Framework.Persistence.Abstractions.Queries;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Access.Domain.Services;

/// <summary>
/// 一个用户的数据范围覆盖与它的自定义部门。
/// </summary>
/// <param name="Scope">覆盖行。</param>
/// <param name="Departments">自定义部门（只有 <see cref="DataScope.Custom"/> 时非空）。</param>
public sealed record UserDataScopeView(SysUserDataScope Scope, IReadOnlyList<(Guid DepartmentId, bool IncludeChildren)> Departments);

/// <summary>
/// 用户级数据范围覆盖（作用域服务，只在租户侧）：读、整体替换与清除。
/// </summary>
/// <param name="scopes">覆盖仓储。</param>
/// <param name="departments">自定义部门仓储。</param>
/// <param name="directory">部门仓储（租户库，按当前租户过滤）。</param>
/// <param name="ids">标识生成器。</param>
public sealed class UserDataScopeService(IRepository<SysUserDataScope, Guid> scopes, IRepository<SysUserDataScopeDepartment, Guid> departments, IRepository<SysDepartment, Guid> directory, IIdGenerator<Guid> ids)
{
    /// <summary>
    /// 取用户的数据范围覆盖。
    /// </summary>
    /// <returns>用户的覆盖；没有即按角色。</returns>
    public async Task<Maybe<UserDataScopeView>> FindAsync(string owner, Guid userId, CancellationToken cancellationToken)
    {
        var rows = await scopes.ListAsync(EntityQuery.All<SysUserDataScope>().Where(new Specification<SysUserDataScope>(s => s.OwnerTenantId == owner && s.UserId == userId)), cancellationToken).ConfigureAwait(false);
        if (rows.Count == 0)
        {
            return default;
        }

        var custom = rows[0].DataScope == DataScope.Custom
            ? (await DepartmentsAsync(owner, userId, cancellationToken).ConfigureAwait(false)).Select(static d => (d.DepartmentId, d.IncludeChildren)).ToList()
            : [];
        return Maybe.Some(new UserDataScopeView(rows[0], custom));
    }

    /// <summary>
    /// 设置用户的数据范围覆盖。
    /// </summary>
    /// <remarks>
    /// <paramref name="scope"/> 为 <see langword="null"/> 即清除、回到按角色。
    /// 自定义范围要列出部门，其他范围不能带部门，部门须在本租户。
    /// </remarks>
    public async Task<Result> SetAsync(string owner, Guid userId, DataScope? scope, IReadOnlyList<DataScopeDepartmentRequest> custom, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(custom);
        if ((scope == DataScope.Custom) != (custom.Count > 0))
        {
            return Error.Validation(AccessErrors.UserDataScopeDepartments, AccessMessages.UserDataScopeDepartments);
        }

        var wanted = custom.GroupBy(static d => d.DepartmentId).Select(static g => g.Last()).ToList();
        if (wanted.Count > 0)
        {
            var found = (await directory.FindManyAsync(wanted.Select(static d => d.DepartmentId).ToList(), cancellationToken).ConfigureAwait(false)).Select(static d => d.Id).ToHashSet();
            if (wanted.FirstOrDefault(d => !found.Contains(d.DepartmentId)) is { } missing)
            {
                return Error.NotFound(AccessErrors.DepartmentNotFound, AccessMessages.DepartmentNotFound, MessageArguments.Of(("id", missing.DepartmentId)));
            }
        }

        var existing = await scopes.ListAsync(EntityQuery.All<SysUserDataScope>().Where(new Specification<SysUserDataScope>(s => s.OwnerTenantId == owner && s.UserId == userId)), cancellationToken).ConfigureAwait(false);
        var oldDepartments = await DepartmentsAsync(owner, userId, cancellationToken).ConfigureAwait(false);
        foreach (var outcomes in new[]
        {
            existing.Count > 0 ? await scopes.RemoveRangeAsync(existing, cancellationToken).ConfigureAwait(false) : [],
            oldDepartments.Count > 0 ? await departments.RemoveRangeAsync(oldDepartments, cancellationToken).ConfigureAwait(false) : [],
            scope is { } value ? [await scopes.AddAsync(new SysUserDataScope { Id = ids.NewId(), OwnerTenantId = owner, UserId = userId, DataScope = value }, cancellationToken).ConfigureAwait(false)] : [],
            wanted.Count > 0 ? await departments.AddRangeAsync(wanted.Select(d => new SysUserDataScopeDepartment { Id = ids.NewId(), OwnerTenantId = owner, UserId = userId, DepartmentId = d.DepartmentId, IncludeChildren = d.IncludeChildren }).ToList(), cancellationToken).ConfigureAwait(false) : [],
        })
        {
            foreach (var outcome in outcomes)
            {
                if (outcome.IsFailure)
                {
                    return outcome;
                }
            }
        }

        return Result.Success();
    }

    private Task<IReadOnlyList<SysUserDataScopeDepartment>> DepartmentsAsync(string owner, Guid userId, CancellationToken cancellationToken)
        => departments.ListAsync(EntityQuery.All<SysUserDataScopeDepartment>().Where(new Specification<SysUserDataScopeDepartment>(d => d.OwnerTenantId == owner && d.UserId == userId)), cancellationToken);
}
