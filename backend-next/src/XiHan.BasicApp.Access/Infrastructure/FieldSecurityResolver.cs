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
/// <see cref="IFieldSecurity"/> 的实现（方案 5.9，语义沿用 4.x）：取当前一侧在实体上的启用规则，按目标命中当前主体后合并——
/// 用户规则认本人；角色规则认此刻持有的角色（令牌里的角色声明与有效指派的交集，角色启用，只认直接持有）；部门规则认所在部门及其上级（只在租户侧，停用的部门不算）。
/// 同一次调用（作用域）内按（侧、主体、实体）缓存结果。
/// </summary>
/// <param name="catalog">字段安全实体目录。</param>
/// <param name="rules">字段安全规则。</param>
/// <param name="assignments">角色指派。</param>
/// <param name="options">安全选项（角色声明名）。</param>
/// <param name="memberships">部门归属仓储（租户库）。</param>
/// <param name="hierarchy">部门闭包表仓储（租户库）。</param>
/// <param name="departments">部门仓储（租户库）。</param>
/// <param name="clock">时钟。</param>
public sealed class FieldSecurityResolver(
    FieldSecurityCatalog catalog,
    FieldSecurityRules rules,
    RoleAssignments assignments,
    SecurityOptions options,
    IRepository<SysUserDepartment, Guid> memberships,
    IRepository<SysDepartmentHierarchy, Guid> hierarchy,
    IRepository<SysDepartment, Guid> departments,
    TimeProvider clock) : IFieldSecurity
{
    private readonly Dictionary<(string Owner, Guid UserId, string Entity), FieldRules> _resolved = [];
    private readonly Dictionary<(string Owner, Guid UserId), Subject> _subjects = [];

    /// <inheritdoc />
    public async ValueTask<FieldRules> ResolveAsync(string entity, InvocationContext context)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entity);
        ArgumentNullException.ThrowIfNull(context);

        // 先认实体：没登记的实体是装配错误，不因「没登录」「没规则」被悄悄放过
        if (catalog.Find(entity) is null)
        {
            throw new InvalidOperationException($"Field security entity '{entity}' is not registered.");
        }

        var security = context.GetSecurity();
        if (!Guid.TryParse(security.Principal.Id, out var userId))
        {
            return FieldRules.None;
        }

        var owner = RoleService.OwnerOf(context);
        if (_resolved.TryGetValue((owner, userId, entity), out var cached))
        {
            return cached;
        }

        var cancellationToken = context.CancellationToken;
        var candidates = await rules.OfAsync(owner, entity, cancellationToken).ConfigureAwait(false);
        var resolved = FieldRules.None;
        if (candidates.Count > 0)
        {
            if (!_subjects.TryGetValue((owner, userId), out var subject))
            {
                subject = await SubjectOfAsync(security, owner, userId, cancellationToken).ConfigureAwait(false);
                _subjects[(owner, userId)] = subject;
            }

            resolved = FieldRules.Merge(candidates.Where(r => r.TargetKind switch
            {
                FieldSecurityTargetKind.User => r.TargetId == userId,
                FieldSecurityTargetKind.Role => subject.Roles.Contains(r.TargetId),
                FieldSecurityTargetKind.Department => subject.Departments.Contains(r.TargetId),
                _ => false,
            }).Select(static r => r.ToRule()));
        }

        _resolved[(owner, userId, entity)] = resolved;
        return resolved;
    }

    private async Task<Subject> SubjectOfAsync(SecurityContext security, string owner, Guid userId, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var holding = (await assignments.OfAsync(owner, userId, cancellationToken).ConfigureAwait(false)).Where(a => a.HoldsAt(now)).Select(static a => a.RoleId).ToHashSet();
        holding.IntersectWith(security.Principal.ClaimValues(options.RoleClaim).Select(static c => Guid.TryParse(c, out var id) ? id : Guid.Empty));
        return new Subject(holding, RoleService.IsPlatform(owner) ? [] : await DepartmentsOfAsync(userId, cancellationToken).ConfigureAwait(false));
    }

    /// <summary>
    /// 所在部门沿上级链展开（闭包表里的全部祖先，含自己），只留启用的部门。
    /// </summary>
    private async Task<HashSet<Guid>> DepartmentsOfAsync(Guid userId, CancellationToken cancellationToken)
    {
        var own = (await memberships.ListAsync(EntityQuery.All<SysUserDepartment>().Where(new Specification<SysUserDepartment>(m => m.UserId == userId)), cancellationToken).ConfigureAwait(false))
            .Select(static m => m.DepartmentId).Distinct().ToList();
        if (own.Count == 0)
        {
            return [];
        }

        var ancestors = (await hierarchy.ListAsync(EntityQuery.All<SysDepartmentHierarchy>().Where(new Specification<SysDepartmentHierarchy>(h => own.Contains(h.DescendantId))), cancellationToken).ConfigureAwait(false))
            .Select(static h => h.AncestorId).Concat(own).Distinct().ToList();
        return (await departments.ListAsync(EntityQuery.All<SysDepartment>().Where(new Specification<SysDepartment>(d => ancestors.Contains(d.Id) && d.Status == OrganizationStatus.Active)), cancellationToken).ConfigureAwait(false))
            .Select(static d => d.Id).ToHashSet();
    }

    private sealed record Subject(IReadOnlySet<Guid> Roles, IReadOnlySet<Guid> Departments);
}
