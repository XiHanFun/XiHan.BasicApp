// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Organization;
using XiHan.BasicApp.Organization.Domain.Entities;
using XiHan.BasicApp.Runtime.Queries;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Organization.Infrastructure;

/// <summary>
/// <see cref="IOrganizationDirectory"/> 的实现
/// </summary>
/// <remarks>
/// 部门、岗位与任职都是租户库实体，经 <see cref="TenantScopedQuery"/> 只读调用所在租户的行（平台一侧什么都查不到，结果为空）
/// </remarks>
public sealed class OrganizationDirectory : IOrganizationDirectory
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<Guid>> DepartmentMembersAsync(InvocationContext context, IReadOnlyCollection<string> departmentCodes)
    {
        ArgumentNullException.ThrowIfNull(context);
        var departments = await ActiveDepartmentsAsync(context, departmentCodes).ConfigureAwait(false);
        if (departments.Count == 0)
        {
            return [];
        }

        var ids = departments.Select(static d => d.Id).ToList();
        var memberships = await TenantScopedQuery.For<SysUserDepartment>(context)
            .Where(m => ids.Contains(m.DepartmentId))
            .OrderBy(static m => m.Order)
            .ToListAsync(context.CancellationToken)
            .ConfigureAwait(false);
        return departments.SelectMany(d => memberships.Where(m => m.DepartmentId == d.Id).Select(static m => m.UserId)).Distinct().ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Guid>> DepartmentLeadersAsync(InvocationContext context, IReadOnlyCollection<string> departmentCodes)
    {
        ArgumentNullException.ThrowIfNull(context);
        var departments = await ActiveDepartmentsAsync(context, departmentCodes).ConfigureAwait(false);
        return departments.Where(static d => d.LeaderUserId.HasValue).Select(static d => d.LeaderUserId!.Value).Distinct().ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Guid>> PositionHoldersAsync(InvocationContext context, IReadOnlyCollection<string> positionCodes)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(positionCodes);
        var codes = positionCodes.Where(static c => !string.IsNullOrWhiteSpace(c)).Distinct(StringComparer.Ordinal).ToList();
        if (codes.Count == 0)
        {
            return [];
        }

        var positions = await TenantScopedQuery.For<SysPosition>(context)
            .Where(p => codes.Contains(p.Code) && p.Status == OrganizationStatus.Active)
            .ToListAsync(context.CancellationToken)
            .ConfigureAwait(false);
        if (positions.Count == 0)
        {
            return [];
        }

        var positionIds = positions.Select(static p => (Guid?)p.Id).ToList();
        var memberships = await TenantScopedQuery.For<SysUserDepartment>(context)
            .Where(m => positionIds.Contains(m.PositionId))
            .OrderBy(static m => m.Order)
            .ToListAsync(context.CancellationToken)
            .ConfigureAwait(false);
        var departmentIds = memberships.Select(static m => m.DepartmentId).Distinct().ToList();
        var active = departmentIds.Count == 0
            ? []
            : (await TenantScopedQuery.For<SysDepartment>(context)
                .Where(d => departmentIds.Contains(d.Id) && d.Status == OrganizationStatus.Active)
                .Select(static d => d.Id)
                .ToListAsync(context.CancellationToken)
                .ConfigureAwait(false)).ToHashSet();
        return codes
            .SelectMany(code => positions.Where(p => p.Code == code))
            .SelectMany(p => memberships.Where(m => m.PositionId == p.Id && active.Contains(m.DepartmentId)).Select(static m => m.UserId))
            .Distinct()
            .ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<DepartmentLeaderStep>> LeaderPathAsync(InvocationContext context, Guid userId)
    {
        ArgumentNullException.ThrowIfNull(context);
        var memberships = await TenantScopedQuery.For<SysUserDepartment>(context)
            .Where(m => m.UserId == userId)
            .ToListAsync(context.CancellationToken)
            .ConfigureAwait(false);
        var start = memberships.OrderByDescending(static m => m.IsPrimary).ThenBy(static m => m.CreatedAt).FirstOrDefault();
        if (start is null)
        {
            return [];
        }

        // 部门树按上级 ID 逐级上溯，防环
        var departments = (await TenantScopedQuery.For<SysDepartment>(context).ToListAsync(context.CancellationToken).ConfigureAwait(false)).ToDictionary(static d => d.Id);
        var path = new List<DepartmentLeaderStep>();
        var visited = new HashSet<Guid>();
        Guid? current = start.DepartmentId;
        while (current is { } id && visited.Add(id) && departments.TryGetValue(id, out var department))
        {
            path.Add(new DepartmentLeaderStep(department.Id, department.Status == OrganizationStatus.Active, department.LeaderUserId));
            current = department.ParentId;
        }

        return path;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<Guid, string>> PrimaryDepartmentNamesAsync(InvocationContext context, IReadOnlyCollection<Guid> userIds)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(userIds);
        var ids = userIds.Distinct().ToList();
        if (ids.Count == 0)
        {
            return new Dictionary<Guid, string>();
        }

        var memberships = await TenantScopedQuery.For<SysUserDepartment>(context)
            .Where(m => ids.Contains(m.UserId))
            .ToListAsync(context.CancellationToken)
            .ConfigureAwait(false);
        if (memberships.Count == 0)
        {
            return new Dictionary<Guid, string>();
        }

        var departmentIds = memberships.Select(static m => m.DepartmentId).Distinct().ToList();
        var names = (await TenantScopedQuery.For<SysDepartment>(context)
                .Where(d => departmentIds.Contains(d.Id))
                .ToListAsync(context.CancellationToken)
                .ConfigureAwait(false))
            .ToDictionary(static d => d.Id, static d => d.Name);
        var primary = new Dictionary<Guid, string>();
        foreach (var group in memberships.GroupBy(static m => m.UserId))
        {
            if (names.TryGetValue(group.OrderByDescending(static m => m.IsPrimary).ThenBy(static m => m.CreatedAt).First().DepartmentId, out var name))
            {
                primary[group.Key] = name;
            }
        }

        return primary;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<DepartmentOption>> DepartmentsAsync(InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var rows = await TenantScopedQuery.For<SysDepartment>(context)
            .OrderBy(static d => d.Order)
            .OrderBy(static d => d.Code)
            .ToListAsync(context.CancellationToken)
            .ConfigureAwait(false);
        return rows.Select(static d => new DepartmentOption(d.Id, d.ParentId, d.Code, d.Name, d.Status == OrganizationStatus.Active, d.Order)).ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PositionOption>> PositionsAsync(InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var rows = await TenantScopedQuery.For<SysPosition>(context)
            .Where(p => p.Status == OrganizationStatus.Active)
            .OrderBy(static p => p.Order)
            .OrderBy(static p => p.Code)
            .ToListAsync(context.CancellationToken)
            .ConfigureAwait(false);
        return rows.Select(static p => new PositionOption(p.Id, p.Code, p.Name)).ToList();
    }

    private static async Task<List<SysDepartment>> ActiveDepartmentsAsync(InvocationContext context, IReadOnlyCollection<string> departmentCodes)
    {
        ArgumentNullException.ThrowIfNull(departmentCodes);
        var codes = departmentCodes.Where(static c => !string.IsNullOrWhiteSpace(c)).Distinct(StringComparer.Ordinal).ToList();
        if (codes.Count == 0)
        {
            return [];
        }

        var rows = await TenantScopedQuery.For<SysDepartment>(context)
            .Where(d => codes.Contains(d.Code) && d.Status == OrganizationStatus.Active)
            .ToListAsync(context.CancellationToken)
            .ConfigureAwait(false);
        return codes.SelectMany(code => rows.Where(d => d.Code == code)).ToList();
    }
}
