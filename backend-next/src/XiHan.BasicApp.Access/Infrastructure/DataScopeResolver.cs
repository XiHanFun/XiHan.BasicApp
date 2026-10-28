// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Access.Domain.Entities;
using XiHan.BasicApp.Access.Domain.Services;
using XiHan.BasicApp.Contracts.Security;
using XiHan.BasicApp.Organization.Domain.Entities;
using XiHan.Framework.Domain.Specifications;
using XiHan.Framework.Kernel.Security;
using XiHan.Framework.Persistence.Abstractions.Queries;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;
using XiHan.Framework.Security.Abstractions;

namespace XiHan.BasicApp.Access.Infrastructure;

/// <summary>
/// <see cref="IDataScopeResolver"/> 的实现（作用域，结果按调用缓存）：
/// 平台侧不受限；租户侧先看用户级覆盖（<see cref="SysUserDataScope"/>）——有即完全取代角色的数据范围（不取并集，与 4.x 一致）；
/// 没有覆盖时按令牌 <c>role</c> 声明里能解析成本租户应用角色的角色求并集——任一 <see cref="DataScope.All"/> 即不受限，
/// 否则把「本部门 / 本部门及子部门 / 自定义部门」经归属与闭包表展开成部门集合，再展开成用户集合（总含自己）。
/// 没有覆盖、也没有一个声明能解析成应用角色（只有框架级的角色字符串）时不受限：数据范围是应用角色的属性。
/// </summary>
/// <param name="roles">角色仓储。</param>
/// <param name="scopes">角色数据范围服务。</param>
/// <param name="userScopes">用户级数据范围覆盖服务。</param>
/// <param name="memberships">归属仓储（租户库）。</param>
/// <param name="hierarchy">部门闭包表仓储（租户库）。</param>
/// <param name="options">安全配置（角色声明名）。</param>
public sealed class DataScopeResolver(
    IRepository<SysRole, Guid> roles,
    RoleDataScopeService scopes,
    UserDataScopeService userScopes,
    IRepository<SysUserDepartment, Guid> memberships,
    IRepository<SysDepartmentHierarchy, Guid> hierarchy,
    SecurityOptions options) : IDataScopeResolver
{
    private DataScopeDecision? _cached;

    /// <inheritdoc />
    public async ValueTask<DataScopeDecision> ResolveAsync(InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return _cached ??= await ComputeAsync(context).ConfigureAwait(false);
    }

    private async Task<DataScopeDecision> ComputeAsync(InvocationContext context)
    {
        var security = context.GetSecurity();
        var tenantId = security.Tenant?.Id;
        if (tenantId is null || !Guid.TryParse(security.Principal.Id, out var userId))
        {
            return DataScopeDecision.Everything;
        }

        var cancellationToken = context.CancellationToken;
        if ((await userScopes.FindAsync(tenantId, userId, cancellationToken).ConfigureAwait(false)).TryGetValue(out var overridden))
        {
            if (overridden.Scope.DataScope == DataScope.All)
            {
                return DataScopeDecision.Everything;
            }

            var departments = new HashSet<Guid>();
            await AddAsync(departments, overridden.Scope.DataScope, () => Task.FromResult(overridden.Departments), new OwnDepartments(this, userId), cancellationToken).ConfigureAwait(false);
            return await DecideAsync(userId, departments, cancellationToken).ConfigureAwait(false);
        }

        var roleIds = security.Principal.ClaimValues(options.RoleClaim).Select(static value => Guid.TryParse(value, out var id) ? id : (Guid?)null).OfType<Guid>().Distinct().ToList();
        if (roleIds.Count == 0)
        {
            return DataScopeDecision.Everything;
        }

        var found = (await roles.FindManyAsync(roleIds, cancellationToken).ConfigureAwait(false))
            .Where(r => string.Equals(r.OwnerTenantId, tenantId, StringComparison.Ordinal) && r.Status == RoleStatus.Active)
            .ToList();
        if (found.Count == 0 || found.Any(static r => r.DataScope == DataScope.All))
        {
            return DataScopeDecision.Everything;
        }

        var union = new HashSet<Guid>();
        var own = new OwnDepartments(this, userId);
        foreach (var role in found)
        {
            await AddAsync(union, role.DataScope, async () => (await scopes.ListAsync(role.Id, cancellationToken).ConfigureAwait(false)).Select(static r => (r.DepartmentId, r.IncludeChildren)).ToList(), own, cancellationToken).ConfigureAwait(false);
        }

        return await DecideAsync(userId, union, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// 把一种数据范围展开成部门并进集合；自定义部门按需读取。
    /// </summary>
    private async Task AddAsync(HashSet<Guid> departments, DataScope scope, Func<Task<IReadOnlyList<(Guid DepartmentId, bool IncludeChildren)>>> custom, OwnDepartments own, CancellationToken cancellationToken)
    {
        switch (scope)
        {
            case DataScope.DepartmentOnly:
                departments.UnionWith(await own.GetAsync(cancellationToken).ConfigureAwait(false));
                break;
            case DataScope.DepartmentAndChildren:
                var mine = await own.GetAsync(cancellationToken).ConfigureAwait(false);
                departments.UnionWith(mine);
                departments.UnionWith(await DescendantsAsync(mine, cancellationToken).ConfigureAwait(false));
                break;
            case DataScope.Custom:
                var rows = await custom().ConfigureAwait(false);
                departments.UnionWith(rows.Select(static r => r.DepartmentId));
                var withChildren = rows.Where(static r => r.IncludeChildren).Select(static r => r.DepartmentId).ToList();
                if (withChildren.Count > 0)
                {
                    departments.UnionWith(await DescendantsAsync(withChildren, cancellationToken).ConfigureAwait(false));
                }

                break;
            case DataScope.SelfOnly:
            case DataScope.All:
            default:
                break;
        }
    }

    private async Task<DataScopeDecision> DecideAsync(Guid userId, HashSet<Guid> departments, CancellationToken cancellationToken)
    {
        var users = new HashSet<Guid> { userId };
        if (departments.Count > 0)
        {
            var list = departments.ToList();
            var members = await memberships.ListAsync(EntityQuery.All<SysUserDepartment>().Where(new Specification<SysUserDepartment>(m => list.Contains(m.DepartmentId))), cancellationToken).ConfigureAwait(false);
            users.UnionWith(members.Select(static m => m.UserId));
        }

        return new DataScopeDecision(false, users, departments);
    }

    private async Task<IReadOnlyList<Guid>> OwnDepartmentsAsync(Guid userId, CancellationToken cancellationToken)
    {
        var rows = await memberships.ListAsync(EntityQuery.All<SysUserDepartment>().Where(new Specification<SysUserDepartment>(m => m.UserId == userId)), cancellationToken).ConfigureAwait(false);
        return rows.Select(static m => m.DepartmentId).Distinct().ToList();
    }

    private async Task<IReadOnlyList<Guid>> DescendantsAsync(IReadOnlyList<Guid> ancestors, CancellationToken cancellationToken)
    {
        if (ancestors.Count == 0)
        {
            return [];
        }

        var list = ancestors.ToList();
        var rows = await hierarchy.ListAsync(EntityQuery.All<SysDepartmentHierarchy>().Where(new Specification<SysDepartmentHierarchy>(h => list.Contains(h.AncestorId) && h.Depth > 0)), cancellationToken).ConfigureAwait(false);
        return rows.Select(static h => h.DescendantId).Distinct().ToList();
    }

    /// <summary>
    /// 用户本人的部门：多个角色共用、只读一次。
    /// </summary>
    private sealed class OwnDepartments(DataScopeResolver resolver, Guid userId)
    {
        private IReadOnlyList<Guid>? _departments;

        public async Task<IReadOnlyList<Guid>> GetAsync(CancellationToken cancellationToken)
            => _departments ??= await resolver.OwnDepartmentsAsync(userId, cancellationToken).ConfigureAwait(false);
    }
}
