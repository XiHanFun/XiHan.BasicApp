// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Users;
using XiHan.BasicApp.Organization.Domain.Entities;
using XiHan.BasicApp.Organization.Domain.Services;
using XiHan.Framework.Domain.Specifications;
using XiHan.Framework.Kernel.Security;
using XiHan.Framework.Persistence.Abstractions.Identifiers;
using XiHan.Framework.Persistence.Abstractions.Queries;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;
using XiHan.Framework.Security.Abstractions;
using XiHan.Framework.Security.Abstractions.Permissions;

namespace XiHan.BasicApp.Organization.Infrastructure;

/// <summary>
/// 导入用户时加入部门
/// </summary>
/// <remarks>
/// 导入行的 <c>departmentCode</c>。部门只在租户侧，导入人要有 <c>organization.memberships.create</c>，缺了整批拒绝；编码在本租户不存在的行记为失败
/// 新用户还没有归属，加入的部门就是主部门；加入照常发出 <c>basicapp.organization.membership-changed</c>
/// </remarks>
/// <param name="departments">部门仓储（当前租户）</param>
/// <param name="memberships">归属仓储</param>
/// <param name="permissions">权限判定</param>
/// <param name="ids">标识生成器</param>
/// <param name="clock">时间源</param>
/// <param name="snapshots">部门缓存（成员数随之变）</param>
public sealed class DepartmentUserImport(
    IRepository<SysDepartment, Guid> departments,
    IRepository<SysUserDepartment, Guid> memberships,
    IPermissionChecker permissions,
    IIdGenerator<Guid> ids,
    TimeProvider clock,
    DepartmentSnapshots snapshots) : IUserImportExtension
{
    private const string Field = "departmentCode";

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<UserImportIssue>>> ValidateAsync(IReadOnlyList<UserImportLine> lines, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(lines);
        ArgumentNullException.ThrowIfNull(context);
        var wanted = Wanted(lines);
        if (wanted.Count == 0)
        {
            return Array.Empty<UserImportIssue>();
        }

        var tenant = OrganizationScope.RequireTenant(context);
        if (tenant.IsFailure)
        {
            return tenant.Error!;
        }

        var held = await permissions.HasPermissionAsync(context.GetSecurity(), OrganizationPermissions.MembershipsCreate, context.CancellationToken).ConfigureAwait(false);
        if (!held.TryGetValue(out var granted))
        {
            return held.Error!;
        }

        if (!granted)
        {
            return Error.Denied(SecurityErrors.PermissionMissing, SecurityMessages.PermissionMissing, MessageArguments.Of(("permission", OrganizationPermissions.MembershipsCreate)));
        }

        var found = await ResolveAsync(wanted, context.CancellationToken).ConfigureAwait(false);
        return wanted
            .Where(l => !found.ContainsKey(l.DepartmentCode!))
            .Select(static l => new UserImportIssue(l.Index, Field, Error.NotFound(OrganizationErrors.DepartmentNotFound, OrganizationMessages.DepartmentNotFound, MessageArguments.Of(("id", l.DepartmentCode)))))
            .ToList();
    }

    /// <inheritdoc />
    public async Task<Result> ApplyAsync(IReadOnlyList<UserImportLine> lines, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(lines);
        ArgumentNullException.ThrowIfNull(context);
        var wanted = Wanted(lines);
        if (wanted.Count == 0)
        {
            return Result.Success();
        }

        var found = await ResolveAsync(wanted, context.CancellationToken).ConfigureAwait(false);
        var now = clock.GetUtcNow();
        var rows = wanted.Select(l =>
        {
            var membership = new SysUserDepartment { Id = ids.NewId(), UserId = l.UserId, DepartmentId = found[l.DepartmentCode!], IsPrimary = true };
            membership.RecordJoined(now);
            return membership;
        }).ToList();
        foreach (var outcome in await memberships.AddRangeAsync(rows, context.CancellationToken).ConfigureAwait(false))
        {
            if (outcome.IsFailure)
            {
                return outcome;
            }
        }

        if (OrganizationScope.RequireTenant(context).TryGetValue(out var tenantId))
        {
            await snapshots.InvalidateAsync(tenantId, context.CancellationToken).ConfigureAwait(false);
        }

        return Result.Success();
    }

    private static List<UserImportLine> Wanted(IReadOnlyList<UserImportLine> lines) => lines.Where(static l => l.DepartmentCode is not null).ToList();

    private async Task<Dictionary<string, Guid>> ResolveAsync(IReadOnlyList<UserImportLine> wanted, CancellationToken cancellationToken)
    {
        var codes = wanted.Select(static l => l.DepartmentCode!).Distinct(StringComparer.Ordinal).ToList();
        var rows = await departments.ListAsync(EntityQuery.All<SysDepartment>().Where(new Specification<SysDepartment>(d => codes.Contains(d.Code))), cancellationToken).ConfigureAwait(false);
        return rows.ToDictionary(static d => d.Code, static d => d.Id, StringComparer.Ordinal);
    }
}
