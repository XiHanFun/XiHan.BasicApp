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
/// 角色数据范围（作用域）。
/// </summary>
/// <remarks>
/// 校验（平台角色只能全部；自定义至少一个部门且部门须在当前租户）、读写自定义部门行（整体替换）。
/// </remarks>
/// <param name="customs">自定义部门行仓储。</param>
/// <param name="departments">部门仓储（租户库，按当前租户过滤）。</param>
/// <param name="ids">标识生成器。</param>
public sealed class RoleDataScopeService(IRepository<SysRoleDataScope, Guid> customs, IRepository<SysDepartment, Guid> departments, IIdGenerator<Guid> ids)
{
    /// <summary>
    /// 角色的自定义部门行。
    /// </summary>
    public Task<IReadOnlyList<SysRoleDataScope>> ListAsync(Guid roleId, CancellationToken cancellationToken)
        => customs.ListAsync(EntityQuery.All<SysRoleDataScope>().Where(new Specification<SysRoleDataScope>(s => s.RoleId == roleId)), cancellationToken);

    /// <summary>
    /// 校验请求的数据范围对该角色是否成立。
    /// </summary>
    /// <remarks>
    /// 平台角色只能 <see cref="DataScope.All"/> 且不能带部门；<see cref="DataScope.Custom"/> 至少一个部门，且部门都在当前租户。
    /// </remarks>
    public async Task<Result> ValidateAsync(SysRole role, DataScope scope, IReadOnlyList<DataScopeDepartmentRequest> customDepartments, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(role);
        ArgumentNullException.ThrowIfNull(customDepartments);
        if (RoleService.IsPlatform(role.OwnerTenantId))
        {
            return scope == DataScope.All && customDepartments.Count == 0
                ? Result.Success()
                : Error.Validation(AccessErrors.DataScopeNotApplicable, AccessMessages.DataScopeNotApplicable, MessageArguments.Of(("code", role.Code)));
        }

        if (scope != DataScope.Custom)
        {
            return Result.Success();
        }

        if (customDepartments.Count == 0)
        {
            return Error.Validation(AccessErrors.CustomScopeEmpty, AccessMessages.CustomScopeEmpty, MessageArguments.Of(("code", role.Code)));
        }

        var wanted = customDepartments.Select(static d => d.DepartmentId).Distinct().ToList();
        var found = (await departments.FindManyAsync(wanted, cancellationToken).ConfigureAwait(false)).Select(static d => d.Id).ToHashSet();
        var missing = wanted.FirstOrDefault(id => !found.Contains(id));
        return missing == Guid.Empty
            ? Result.Success()
            : Error.NotFound(AccessErrors.DepartmentNotFound, AccessMessages.DepartmentNotFound, MessageArguments.Of(("id", missing)));
    }

    /// <summary>
    /// 整体替换自定义部门行。
    /// </summary>
    /// <remarks>
    /// 非 <see cref="DataScope.Custom"/> 即清空。
    /// </remarks>
    public async Task<Result> ReplaceAsync(SysRole role, IReadOnlyList<DataScopeDepartmentRequest> customDepartments, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(role);
        ArgumentNullException.ThrowIfNull(customDepartments);
        var existing = await ListAsync(role.Id, cancellationToken).ConfigureAwait(false);
        if (existing.Count > 0)
        {
            foreach (var outcome in await customs.RemoveRangeAsync(existing, cancellationToken).ConfigureAwait(false))
            {
                if (outcome.IsFailure)
                {
                    return outcome;
                }
            }
        }

        if (role.DataScope != DataScope.Custom || customDepartments.Count == 0)
        {
            return Result.Success();
        }

        var rows = customDepartments
            .GroupBy(static d => d.DepartmentId)
            .Select(g => new SysRoleDataScope { Id = ids.NewId(), OwnerTenantId = role.OwnerTenantId, RoleId = role.Id, DepartmentId = g.Key, IncludeChildren = g.Any(static d => d.IncludeChildren) })
            .ToList();
        foreach (var outcome in await customs.AddRangeAsync(rows, cancellationToken).ConfigureAwait(false))
        {
            if (outcome.IsFailure)
            {
                return outcome;
            }
        }

        return Result.Success();
    }
}
