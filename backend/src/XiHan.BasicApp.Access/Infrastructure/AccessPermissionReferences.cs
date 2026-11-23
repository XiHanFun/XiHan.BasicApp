// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Data.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SqlSugar;
using XiHan.BasicApp.Access.Domain.Entities;
using XiHan.BasicApp.Contracts.Security;
using XiHan.BasicApp.Runtime.MultiTenancy;
using XiHan.BasicApp.Runtime.Persistence;
using XiHan.Framework.Domain.Entities;
using XiHan.Framework.Domain.Specifications;
using XiHan.Framework.MultiTenancy.Abstractions.Sharding;
using XiHan.Framework.Persistence.Abstractions.Queries;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Persistence.Abstractions.UnitOfWork;
using XiHan.Framework.Primitives;
using XiHan.Framework.Security.Abstractions.Permissions;
using XiHan.Framework.Security.Database.Grants;

namespace XiHan.BasicApp.Access.Infrastructure;

/// <summary>
/// Access 里对一批权限码的引用
/// </summary>
/// <remarks>
/// 角色与用户的授予（框架授予表，平台库，带租户列）、授予规则、权限条件、按权限码的委托、互斥约束的项、运营节点的门控权限码都在平台库，在调用方的工作单元里清掉，缓存在提交之后失效
/// 待审的授权申请在各租户的库里，逐侧读出只列不改：申请是留档，批准时会因权限码不存在被拒（<c>ACCESS_VALIDATION_052</c>）；读不出的一侧即失败
/// 互斥约束清掉项之后不足两个权限码的，整条规则删掉（规则至少要两项才成立）
/// </remarks>
/// <param name="records">框架的授予记录</param>
/// <param name="administration">框架的授予管理（撤销时失效授予缓存）</param>
/// <param name="roles">角色仓储</param>
/// <param name="permissionRules">授予规则仓储</param>
/// <param name="conditions">权限条件仓储</param>
/// <param name="delegations">委托仓储</param>
/// <param name="constraintRules">约束规则仓储</param>
/// <param name="constraintItems">约束规则项仓储</param>
/// <param name="menus">菜单仓储</param>
/// <param name="rulesCache">授予规则缓存</param>
/// <param name="conditionsCache">权限条件缓存</param>
/// <param name="delegationsCache">委托缓存</param>
/// <param name="constraintsCache">约束规则缓存</param>
/// <param name="sides">全部一侧</param>
/// <param name="scopes">作用域工厂（逐侧读授权申请）</param>
/// <param name="logger">日志</param>
public sealed partial class AccessPermissionReferences(
    IRepository<PermissionGrantRecord, Guid> records,
    IPermissionGrantAdministration administration,
    IRepository<SysRole, Guid> roles,
    IRepository<SysPermissionRule, Guid> permissionRules,
    IRepository<SysPermissionCondition, Guid> conditions,
    IRepository<SysPermissionDelegation, Guid> delegations,
    IRepository<SysConstraintRule, Guid> constraintRules,
    IRepository<SysConstraintRuleItem, Guid> constraintItems,
    IRepository<SysMenu, Guid> menus,
    PermissionRules rulesCache,
    PermissionConditions conditionsCache,
    Delegations delegationsCache,
    ConstraintRules constraintsCache,
    TenantSides sides,
    IServiceScopeFactory scopes,
    ILogger<AccessPermissionReferences> logger) : IPermissionReferences
{
    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<PermissionReference>>> FindAsync(IReadOnlyCollection<string> codes, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(codes);
        if (codes.Count == 0)
        {
            return Result.Success<IReadOnlyList<PermissionReference>>([]);
        }

        var found = new List<PermissionReference>();
        var grants = await GrantsOfAsync(codes, cancellationToken).ConfigureAwait(false);
        var roleCodes = await RoleCodesAsync(grants, cancellationToken).ConfigureAwait(false);
        found.AddRange(grants.Select(g => string.Equals(g.SubjectKind, GrantSubject.Role, StringComparison.Ordinal)
            ? new PermissionReference(PermissionReferenceKind.RoleGrant, SideOf(g.TenantId), roleCodes.GetValueOrDefault(g.SubjectId, g.SubjectId), g.Permission)
            : new PermissionReference(PermissionReferenceKind.UserGrant, SideOf(g.TenantId), g.SubjectId, g.Permission)));
        found.AddRange((await RulesOfAsync(codes, cancellationToken).ConfigureAwait(false)).Select(static r => new PermissionReference(PermissionReferenceKind.PermissionRule, r.OwnerTenantId, $"{r.SubjectKind}:{r.SubjectId:D}", r.Permission)));
        found.AddRange((await ConditionsOfAsync(codes, cancellationToken).ConfigureAwait(false)).Select(static c => new PermissionReference(PermissionReferenceKind.PermissionCondition, c.OwnerTenantId, $"{c.SubjectKind}:{c.SubjectId:D}", c.Permission)));
        found.AddRange((await DelegationsOfAsync(codes, cancellationToken).ConfigureAwait(false)).Select(static d => new PermissionReference(PermissionReferenceKind.Delegation, d.OwnerTenantId, d.DelegateeId.ToString("D"), d.Permission!)));
        var (items, rules) = await ConstraintsOfAsync(codes, cancellationToken).ConfigureAwait(false);
        found.AddRange(items.Select(i => new PermissionReference(PermissionReferenceKind.ConstraintRule, i.OwnerTenantId, rules[i.RuleId].Code, i.TargetId)));
        found.AddRange((await MenusOfAsync(codes, cancellationToken).ConfigureAwait(false)).Select(static m => new PermissionReference(PermissionReferenceKind.Menu, PlatformSide, m.Code, m.Permission!)));

        var listed = await sides.ListAsync(cancellationToken).ConfigureAwait(false);
        if (!listed.TryGetValue(out var sideIds))
        {
            return listed.Error!;
        }

        foreach (var side in sideIds)
        {
            var requests = await PendingRequestsAsync(side, codes, cancellationToken).ConfigureAwait(false);
            if (!requests.TryGetValue(out var pending))
            {
                return requests.Error!;
            }

            found.AddRange(pending.Select(r => new PermissionReference(PermissionReferenceKind.PermissionRequest, side, r.Id.ToString("D"), r.PermissionCode!)));
        }

        return found;
    }

    /// <inheritdoc />
    public async Task<Result> RemoveAsync(IReadOnlyCollection<string> codes, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(codes);
        if (codes.Count == 0)
        {
            return Result.Success();
        }

        // 授予经框架的授予管理撤销：它记下受影响的主体，提交之后失效授予缓存
        var grants = await GrantsOfAsync(codes, cancellationToken).ConfigureAwait(false);
        if (grants.Count > 0)
        {
            foreach (var revoked in await administration.RevokeAsync([.. grants.Select(static g => g.ToGrant())], cancellationToken).ConfigureAwait(false))
            {
                if (revoked.IsFailure)
                {
                    return revoked;
                }
            }
        }

        var rules = await RulesOfAsync(codes, cancellationToken).ConfigureAwait(false);
        var removed = await RemoveAllAsync(permissionRules, rules, cancellationToken).ConfigureAwait(false);
        if (removed.IsFailure)
        {
            return removed;
        }

        foreach (var (owner, kind, subject) in rules.Select(static r => (r.OwnerTenantId, r.SubjectKind, r.SubjectId)).Distinct())
        {
            await rulesCache.InvalidateAsync(owner, kind, subject, cancellationToken).ConfigureAwait(false);
        }

        var bound = await ConditionsOfAsync(codes, cancellationToken).ConfigureAwait(false);
        removed = await RemoveAllAsync(conditions, bound, cancellationToken).ConfigureAwait(false);
        if (removed.IsFailure)
        {
            return removed;
        }

        foreach (var (owner, kind, subject) in bound.Select(static c => (c.OwnerTenantId, c.SubjectKind, c.SubjectId)).Distinct())
        {
            await conditionsCache.InvalidateAsync(owner, kind, subject, cancellationToken).ConfigureAwait(false);
        }

        var delegated = await DelegationsOfAsync(codes, cancellationToken).ConfigureAwait(false);
        removed = await RemoveAllAsync(delegations, delegated, cancellationToken).ConfigureAwait(false);
        if (removed.IsFailure)
        {
            return removed;
        }

        foreach (var (owner, delegatee) in delegated.Select(static d => (d.OwnerTenantId, d.DelegateeId)).Distinct())
        {
            await delegationsCache.InvalidateAsync(owner, delegatee, cancellationToken).ConfigureAwait(false);
        }

        var constrained = await RemoveConstraintItemsAsync(codes, cancellationToken).ConfigureAwait(false);
        if (constrained.IsFailure)
        {
            return constrained;
        }

        // 运营节点只去掉门控权限码，节点留着
        foreach (var menu in await MenusOfAsync(codes, cancellationToken).ConfigureAwait(false))
        {
            menu.Permission = null;
            var updated = await menus.UpdateAsync(menu, cancellationToken).ConfigureAwait(false);
            if (updated.IsFailure)
            {
                return updated;
            }
        }

        return Result.Success();
    }

    private const string PlatformSide = "platform";

    /// <summary>
    /// 授予表的租户列换成一侧：空是平台
    /// </summary>
    private static string SideOf(string tenantId) => tenantId.Length == 0 ? PlatformSide : tenantId;

    private Task<IReadOnlyList<PermissionGrantRecord>> GrantsOfAsync(IReadOnlyCollection<string> codes, CancellationToken cancellationToken)
        => records.ListAsync(EntityQuery.All<PermissionGrantRecord>().Where(new Specification<PermissionGrantRecord>(r => codes.Contains(r.Permission))), cancellationToken);

    private Task<IReadOnlyList<SysPermissionRule>> RulesOfAsync(IReadOnlyCollection<string> codes, CancellationToken cancellationToken)
        => permissionRules.ListAsync(EntityQuery.All<SysPermissionRule>().Where(new Specification<SysPermissionRule>(r => codes.Contains(r.Permission))), cancellationToken);

    private Task<IReadOnlyList<SysPermissionCondition>> ConditionsOfAsync(IReadOnlyCollection<string> codes, CancellationToken cancellationToken)
        => conditions.ListAsync(EntityQuery.All<SysPermissionCondition>().Where(new Specification<SysPermissionCondition>(c => codes.Contains(c.Permission))), cancellationToken);

    private Task<IReadOnlyList<SysPermissionDelegation>> DelegationsOfAsync(IReadOnlyCollection<string> codes, CancellationToken cancellationToken)
        => delegations.ListAsync(EntityQuery.All<SysPermissionDelegation>().Where(new Specification<SysPermissionDelegation>(d => d.Permission != null && codes.Contains(d.Permission))), cancellationToken);

    private Task<IReadOnlyList<SysMenu>> MenusOfAsync(IReadOnlyCollection<string> codes, CancellationToken cancellationToken)
        => menus.ListAsync(EntityQuery.All<SysMenu>().Where(new Specification<SysMenu>(m => m.Kind != MenuKind.Page && m.Permission != null && codes.Contains(m.Permission))), cancellationToken);

    /// <summary>
    /// 角色授予的主体换成角色编码
    /// </summary>
    private async Task<Dictionary<string, string>> RoleCodesAsync(IReadOnlyList<PermissionGrantRecord> grants, CancellationToken cancellationToken)
    {
        var ids = grants.Where(static g => string.Equals(g.SubjectKind, GrantSubject.Role, StringComparison.Ordinal))
            .Select(static g => Guid.TryParse(g.SubjectId, out var id) ? id : (Guid?)null)
            .OfType<Guid>()
            .Distinct()
            .ToList();
        return ids.Count == 0 ? [] : (await roles.FindManyAsync(ids, cancellationToken).ConfigureAwait(false)).ToDictionary(static r => r.Id.ToString("D"), static r => r.Code, StringComparer.Ordinal);
    }

    /// <summary>
    /// 以权限码为目标的约束项与它们的规则
    /// </summary>
    private async Task<(IReadOnlyList<SysConstraintRuleItem> Items, Dictionary<Guid, SysConstraintRule> Rules)> ConstraintsOfAsync(IReadOnlyCollection<string> codes, CancellationToken cancellationToken)
    {
        var candidates = await constraintItems.ListAsync(EntityQuery.All<SysConstraintRuleItem>().Where(new Specification<SysConstraintRuleItem>(i => codes.Contains(i.TargetId))), cancellationToken).ConfigureAwait(false);
        if (candidates.Count == 0)
        {
            return ([], []);
        }

        var rules = (await constraintRules.FindManyAsync([.. candidates.Select(static i => i.RuleId).Distinct()], cancellationToken).ConfigureAwait(false))
            .Where(static r => r.TargetType == ConstraintTargetType.Permission)
            .ToDictionary(static r => r.Id);
        return ([.. candidates.Where(i => rules.ContainsKey(i.RuleId))], rules);
    }

    /// <summary>
    /// 删掉以这些权限码为目标的约束项
    /// </summary>
    /// <remarks>
    /// 剩不到两个权限码的规则整条删掉
    /// </remarks>
    private async Task<Result> RemoveConstraintItemsAsync(IReadOnlyCollection<string> codes, CancellationToken cancellationToken)
    {
        var (items, rules) = await ConstraintsOfAsync(codes, cancellationToken).ConfigureAwait(false);
        if (items.Count == 0)
        {
            return Result.Success();
        }

        var removed = await RemoveAllAsync(constraintItems, items, cancellationToken).ConfigureAwait(false);
        if (removed.IsFailure)
        {
            return removed;
        }

        var ruleIds = rules.Keys.ToList();
        var leftovers = await constraintItems.ListAsync(EntityQuery.All<SysConstraintRuleItem>().Where(new Specification<SysConstraintRuleItem>(i => ruleIds.Contains(i.RuleId))), cancellationToken).ConfigureAwait(false);
        var targets = leftovers.GroupBy(static i => i.RuleId).ToDictionary(static g => g.Key, static g => g.Select(static i => i.TargetId).Distinct(StringComparer.Ordinal).Count());
        var broken = rules.Values.Where(r => targets.GetValueOrDefault(r.Id) < 2).ToList();
        var brokenIds = broken.Select(static r => r.Id).ToHashSet();
        removed = await RemoveAllAsync(constraintItems, [.. leftovers.Where(i => brokenIds.Contains(i.RuleId))], cancellationToken).ConfigureAwait(false);
        if (removed.IsFailure)
        {
            return removed;
        }

        removed = await RemoveAllAsync(constraintRules, broken, cancellationToken).ConfigureAwait(false);
        if (removed.IsFailure)
        {
            return removed;
        }

        foreach (var owner in rules.Values.Select(static r => r.OwnerTenantId).Distinct(StringComparer.Ordinal))
        {
            await constraintsCache.InvalidateAsync(owner, cancellationToken).ConfigureAwait(false);
        }

        return Result.Success();
    }

    private static async Task<Result> RemoveAllAsync<TEntity>(IRepository<TEntity, Guid> repository, IReadOnlyList<TEntity> rows, CancellationToken cancellationToken)
        where TEntity : class, IEntity<Guid>
    {
        if (rows.Count == 0)
        {
            return Result.Success();
        }

        foreach (var outcome in await repository.RemoveRangeAsync(rows, cancellationToken).ConfigureAwait(false))
        {
            if (outcome.IsFailure)
            {
                return outcome;
            }
        }

        return Result.Success();
    }

    /// <summary>
    /// 一侧库里还在待审的、申请这些权限码的授权申请
    /// </summary>
    private async Task<Result<List<SysPermissionRequest>>> PendingRequestsAsync(string side, IReadOnlyCollection<string> codes, CancellationToken cancellationToken)
    {
        await using var scope = scopes.CreateAsyncScope();
        var bound = await scope.ServiceProvider.GetRequiredService<TenantShardScope>().BindTenantAsync(side, cancellationToken).ConfigureAwait(false);
        if (bound.IsFailure)
        {
            return bound.Error!;
        }

        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        await unitOfWork.BeginAsync(new UnitOfWorkOptions { IsReadOnly = true, TenantId = side }, cancellationToken).ConfigureAwait(false);
        try
        {
            var found = await scope.ServiceProvider.GetRequiredService<SideRows>().Query<SysPermissionRequest>(side)
                .Where(r => r.Status == PermissionRequestStatus.Pending && r.PermissionCode != null && codes.Contains(r.PermissionCode))
                .ToListAsync(cancellationToken).ConfigureAwait(false);
            (await unitOfWork.CommitAsync(cancellationToken).ConfigureAwait(false)).ThrowIfFailure();
            return found;
        }
        catch (Exception exception) when (exception is DbException or SqlSugarException)
        {
            await RollbackAsync(unitOfWork).ConfigureAwait(false);
            LogSideUnreadable(logger, exception, side);
            return Error.Unavailable(AccessErrors.PermissionReferencesUnavailable, AccessMessages.PermissionReferencesUnavailable, MessageArguments.Of(("side", side)));
        }
        catch
        {
            await RollbackAsync(unitOfWork).ConfigureAwait(false);
            throw;
        }
    }

    private static async Task RollbackAsync(IUnitOfWork unitOfWork)
    {
        if (unitOfWork.State == UnitOfWorkState.Active)
        {
            await unitOfWork.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Permission requests of side {Side} could not be read.")]
    private static partial void LogSideUnreadable(ILogger logger, Exception exception, string side);
}
