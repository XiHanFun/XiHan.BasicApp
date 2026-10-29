// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Access.Domain.Entities;
using XiHan.BasicApp.Access.Domain.Services;
using XiHan.BasicApp.Contracts.Auditing;
using XiHan.BasicApp.Contracts.Security;
using XiHan.Framework.Domain.Specifications;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Persistence.Abstractions.Identifiers;
using XiHan.Framework.Persistence.Abstractions.Queries;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Access.Infrastructure;

/// <summary>
/// 会话的角色激活。
/// </summary>
/// <remarks>
/// Access 对 <see cref="ISessionRoleActivator"/> 的实现，补上 4.x SysSessionRole 缺的写入口。
/// 激活集记在 <see cref="SysSessionRole"/>，
/// 候选按角色的排序号、再按编码排列，逐个尝试，加上它会违反拦截类动态职责分离规则的不激活（同一 DSD 组只留排序最前的）。
/// 激活的原因记调用的操作 ID（<c>auth.login</c>、<c>auth.switch-tenant</c> 等），停用的原因见 <see cref="SysSessionRole.Reason"/>。
/// 开会话与续签时有角色被 DSD 停掉或不再持有，另记一条登录日志（<see cref="LoginEvent.RoleActivation"/>，说明里是原因与角色）。
/// </remarks>
/// <param name="rows">会话角色仓储。</param>
/// <param name="roles">角色查找（排序）。</param>
/// <param name="enforcer">约束规则求值。</param>
/// <param name="ids">标识生成器。</param>
/// <param name="clock">时间源。</param>
public sealed class SessionRoleActivator(IRepository<SysSessionRole, Guid> rows, RoleService roles, ConstraintEnforcer enforcer, IIdGenerator<Guid> ids, TimeProvider clock) : ISessionRoleActivator
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> StartAsync(Guid sessionId, Guid userId, string tenantId, IReadOnlyList<string> heldRoleIds, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(heldRoleIds);
        ArgumentNullException.ThrowIfNull(context);
        var cancellationToken = context.CancellationToken;
        var now = clock.GetUtcNow();
        var existing = await RowsOfAsync(sessionId, cancellationToken).ConfigureAwait(false);
        var reason = ReasonOf(context);

        // 重新来过：旧的激活一律停用（换了一侧或重新登录）
        foreach (var row in existing.Where(static r => r.Status == SessionRoleStatus.Active))
        {
            Deactivate(row, SessionRoleStatus.Inactive, reason, now);
        }

        var ordered = await OrderAsync(tenantId, Parse(heldRoleIds), cancellationToken).ConfigureAwait(false);
        var (activated, dropped, tolerated) = await enforcer.SelectActivationAsync(tenantId, ordered, cancellationToken).ConfigureAwait(false);
        var added = new List<SysSessionRole>();
        foreach (var roleId in activated)
        {
            Activate(existing, added, sessionId, userId, tenantId, roleId, reason, now);
        }

        foreach (var (roleId, violation) in dropped)
        {
            Deactivate(Upsert(existing, added, sessionId, userId, tenantId, roleId, now), SessionRoleStatus.Inactive, "dsd:" + violation.Rule.Code, now);
        }

        foreach (var violation in tolerated)
        {
            ConstraintEnforcer.Record(context, violation, userId: userId);
        }

        await SaveAsync(existing, added, cancellationToken).ConfigureAwait(false);
        LogChanges(context, tenantId, userId, sessionId, dropped, []);
        return [.. activated.Select(static r => r.ToString("D"))];
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> RefreshAsync(Guid sessionId, Guid userId, string tenantId, IReadOnlyList<string> heldRoleIds, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(heldRoleIds);
        ArgumentNullException.ThrowIfNull(context);
        var cancellationToken = context.CancellationToken;
        var now = clock.GetUtcNow();
        var existing = (await RowsOfAsync(sessionId, cancellationToken).ConfigureAwait(false)).Where(r => string.Equals(r.OwnerTenantId, tenantId, StringComparison.Ordinal)).ToList();
        var held = Parse(heldRoleIds).ToHashSet();

        // 不再持有的记到期；本人或 DSD 停掉的不自动恢复，只有从没出现过的新角色才尝试激活
        var lapsed = existing.Where(r => r.Status == SessionRoleStatus.Active && !held.Contains(r.RoleId)).ToList();
        foreach (var row in lapsed)
        {
            Deactivate(row, SessionRoleStatus.Expired, "not-held", now);
        }

        var keep = await OrderAsync(tenantId, [.. existing.Where(r => r.Status == SessionRoleStatus.Active).Select(static r => r.RoleId)], cancellationToken).ConfigureAwait(false);
        var known = existing.Select(static r => r.RoleId).ToHashSet();
        var fresh = await OrderAsync(tenantId, [.. held.Where(r => !known.Contains(r))], cancellationToken).ConfigureAwait(false);
        var (activated, dropped, tolerated) = await enforcer.SelectActivationAsync(tenantId, [.. keep, .. fresh], cancellationToken).ConfigureAwait(false);
        var added = new List<SysSessionRole>();
        foreach (var roleId in activated.Where(r => !keep.Contains(r)))
        {
            Activate(existing, added, sessionId, userId, tenantId, roleId, ReasonOf(context), now);
        }

        foreach (var (roleId, violation) in dropped)
        {
            Deactivate(Upsert(existing, added, sessionId, userId, tenantId, roleId, now), SessionRoleStatus.Inactive, "dsd:" + violation.Rule.Code, now);
        }

        foreach (var violation in tolerated)
        {
            ConstraintEnforcer.Record(context, violation, userId: userId);
        }

        await SaveAsync(existing, added, cancellationToken).ConfigureAwait(false);
        LogChanges(context, tenantId, userId, sessionId, dropped, [.. lapsed.Select(static r => r.RoleId)]);
        return [.. activated.Select(static r => r.ToString("D"))];
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<string>>> ActivateAsync(Guid sessionId, Guid userId, string tenantId, IReadOnlyList<string> heldRoleIds, IReadOnlyList<string> requestedRoleIds, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(heldRoleIds);
        ArgumentNullException.ThrowIfNull(requestedRoleIds);
        ArgumentNullException.ThrowIfNull(context);
        var cancellationToken = context.CancellationToken;
        var held = Parse(heldRoleIds).ToHashSet();
        var requested = new List<Guid>();
        foreach (var raw in requestedRoleIds.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (!Guid.TryParse(raw, out var roleId) || !held.Contains(roleId))
            {
                return Error.Validation(AccessErrors.ActivationRoleNotHeld, AccessMessages.ActivationRoleNotHeld, MessageArguments.Of(("id", raw)));
            }

            requested.Add(roleId);
        }

        var violations = await enforcer.CheckActivationAsync(tenantId, requested, cancellationToken).ConfigureAwait(false);
        if (ConstraintEnforcer.Settle(context, violations, userId: userId) is { } blocked)
        {
            return blocked;
        }

        var now = clock.GetUtcNow();
        var existing = (await RowsOfAsync(sessionId, cancellationToken).ConfigureAwait(false)).Where(r => string.Equals(r.OwnerTenantId, tenantId, StringComparison.Ordinal)).ToList();
        foreach (var row in existing.Where(r => r.Status == SessionRoleStatus.Active && !requested.Contains(r.RoleId)))
        {
            Deactivate(row, held.Contains(row.RoleId) ? SessionRoleStatus.Inactive : SessionRoleStatus.Expired, held.Contains(row.RoleId) ? "manual" : "not-held", now);
        }

        var added = new List<SysSessionRole>();
        var ordered = await OrderAsync(tenantId, requested, cancellationToken).ConfigureAwait(false);
        foreach (var roleId in ordered)
        {
            Activate(existing, added, sessionId, userId, tenantId, roleId, "manual", now);
        }

        await SaveAsync(existing, added, cancellationToken).ConfigureAwait(false);
        return Result.Success<IReadOnlyList<string>>([.. ordered.Select(static r => r.ToString("D"))]);
    }

    private async Task<List<SysSessionRole>> RowsOfAsync(Guid sessionId, CancellationToken cancellationToken)
        => [.. await rows.ListAsync(EntityQuery.All<SysSessionRole>().Where(new Specification<SysSessionRole>(r => r.SessionId == sessionId)), cancellationToken).ConfigureAwait(false)];

    // 角色的排序号、再按编码；这一侧找不到的角色排在最后
    private async Task<List<Guid>> OrderAsync(string tenantId, List<Guid> roleIds, CancellationToken cancellationToken)
    {
        if (roleIds.Count == 0)
        {
            return [];
        }

        var found = (await roles.FindManyAsync(roleIds, tenantId, cancellationToken).ConfigureAwait(false)).ToDictionary(static r => r.Id);
        return [.. roleIds.Distinct()
            .OrderBy(r => found.TryGetValue(r, out var role) ? role.Order : int.MaxValue)
            .ThenBy(r => found.TryGetValue(r, out var role) ? role.Code : r.ToString("D"), StringComparer.Ordinal)];
    }

    // 被 DSD 停掉或不再持有的另记一条登录日志
    private static void LogChanges(InvocationContext context, string tenantId, Guid userId, Guid sessionId, IReadOnlyList<(Guid RoleId, ConstraintViolation Violation)> dropped, IReadOnlyList<Guid> lapsed)
    {
        if (dropped.Count == 0 && lapsed.Count == 0)
        {
            return;
        }

        context.AddAuditLog(new LoginLogEntry(LoginEvent.RoleActivation)
        {
            TenantId = tenantId,
            UserId = userId,
            SessionId = sessionId,
            Detail = string.Join("; ", dropped.Select(static d => $"dsd:{d.Violation.Rule.Code} -{d.RoleId:D}").Concat(lapsed.Select(static r => $"not-held -{r:D}"))),
        });
    }

    private static List<Guid> Parse(IEnumerable<string> roleIds)
        => [.. roleIds.Select(static v => Guid.TryParse(v, out var id) ? id : (Guid?)null).OfType<Guid>().Distinct()];

    private static string ReasonOf(InvocationContext context)
        => context.GetOperation().TryGetValue(out var operation) ? operation.Id : "internal";

    private void Activate(List<SysSessionRole> existing, List<SysSessionRole> added, Guid sessionId, Guid userId, string tenantId, Guid roleId, string reason, DateTimeOffset now)
    {
        var row = Upsert(existing, added, sessionId, userId, tenantId, roleId, now);
        row.Status = SessionRoleStatus.Active;
        row.ActivatedAt = now;
        row.DeactivatedAt = null;
        row.Reason = reason;
    }

    private SysSessionRole Upsert(List<SysSessionRole> existing, List<SysSessionRole> added, Guid sessionId, Guid userId, string tenantId, Guid roleId, DateTimeOffset now)
    {
        if (existing.FirstOrDefault(r => r.RoleId == roleId && string.Equals(r.OwnerTenantId, tenantId, StringComparison.Ordinal)) is { } found)
        {
            return found;
        }

        if (added.FirstOrDefault(r => r.RoleId == roleId) is { } pending)
        {
            return pending;
        }

        var row = new SysSessionRole { Id = ids.NewId(), OwnerTenantId = tenantId, SessionId = sessionId, UserId = userId, RoleId = roleId, ActivatedAt = now };
        added.Add(row);
        return row;
    }

    private static void Deactivate(SysSessionRole row, SessionRoleStatus status, string reason, DateTimeOffset now)
    {
        row.Status = status;
        row.DeactivatedAt = now;
        row.Reason = reason;
    }

    // 已有的行整体写回（状态可能变了），新行一次加入
    private async Task SaveAsync(IReadOnlyList<SysSessionRole> existing, List<SysSessionRole> added, CancellationToken cancellationToken)
    {
        foreach (var row in existing)
        {
            (await rows.UpdateAsync(row, cancellationToken).ConfigureAwait(false)).ThrowIfFailure();
        }

        if (added.Count > 0)
        {
            foreach (var outcome in await rows.AddRangeAsync(added, cancellationToken).ConfigureAwait(false))
            {
                outcome.ThrowIfFailure();
            }
        }
    }
}
