// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Access.Domain.Entities;
using XiHan.Framework.Domain.Specifications;
using XiHan.Framework.Persistence.Abstractions.Identifiers;
using XiHan.Framework.Persistence.Abstractions.Queries;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Access.Infrastructure;

/// <summary>
/// 授予规则的写入（作用域服务）：同一主体同一权限码只保留一行，写完失效 <see cref="PermissionRules"/> 的缓存。
/// 普通授予（没有有效期也没有原因）不留规则行，所以把授予改回普通即删掉它的规则行。
/// </summary>
/// <param name="rules">规则仓储。</param>
/// <param name="cache">规则缓存。</param>
/// <param name="ids">标识生成器。</param>
public sealed class PermissionRuleWriter(IRepository<SysPermissionRule, Guid> rules, PermissionRules cache, IIdGenerator<Guid> ids)
{
    /// <summary>
    /// 主体的全部规则。
    /// </summary>
    public Task<IReadOnlyList<SysPermissionRule>> ListAsync(string owner, GrantSubjectKind kind, Guid subjectId, CancellationToken cancellationToken)
        => rules.ListAsync(EntityQuery.All<SysPermissionRule>().Where(new Specification<SysPermissionRule>(r => r.OwnerTenantId == owner && r.SubjectKind == kind && r.SubjectId == subjectId)), cancellationToken);

    /// <summary>
    /// 给一批权限码写同一种规则（已有的覆盖动作、有效期与原因）；Grant 且既无有效期也无原因即删除规则行（普通授予）。
    /// </summary>
    public async Task<Result> SetAsync(string owner, GrantSubjectKind kind, Guid subjectId, IReadOnlyCollection<string> permissions, PermissionAction action, DateTimeOffset? effectiveAt, DateTimeOffset? expiresAt, string? reason, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(permissions);
        if (permissions.Count == 0)
        {
            return Result.Success();
        }

        var existing = (await ListAsync(owner, kind, subjectId, cancellationToken).ConfigureAwait(false)).ToDictionary(static r => r.Permission, StringComparer.Ordinal);
        var plain = action == PermissionAction.Grant && effectiveAt is null && expiresAt is null && string.IsNullOrWhiteSpace(reason);
        var additions = new List<SysPermissionRule>();
        var updates = new List<SysPermissionRule>();
        var removals = new List<SysPermissionRule>();
        foreach (var permission in permissions)
        {
            if (existing.TryGetValue(permission, out var rule))
            {
                if (plain)
                {
                    removals.Add(rule);
                    continue;
                }

                rule.Action = action;
                rule.EffectiveAt = effectiveAt;
                rule.ExpiresAt = expiresAt;
                rule.Reason = reason;
                updates.Add(rule);
            }
            else if (!plain)
            {
                additions.Add(new SysPermissionRule { Id = ids.NewId(), OwnerTenantId = owner, SubjectKind = kind, SubjectId = subjectId, Permission = permission, Action = action, EffectiveAt = effectiveAt, ExpiresAt = expiresAt, Reason = reason });
            }
        }

        var written = await WriteAsync(additions, updates, removals, cancellationToken).ConfigureAwait(false);
        if (written.IsSuccess)
        {
            await cache.InvalidateAsync(owner, kind, subjectId, cancellationToken).ConfigureAwait(false);
        }

        return written;
    }

    /// <summary>
    /// 删掉一批权限码上的规则（不管授予还是拒绝）；返回确实删掉了规则的权限码。
    /// </summary>
    public async Task<Result<IReadOnlySet<string>>> RemoveAsync(string owner, GrantSubjectKind kind, Guid subjectId, IReadOnlyCollection<string>? permissions, CancellationToken cancellationToken)
    {
        var existing = await ListAsync(owner, kind, subjectId, cancellationToken).ConfigureAwait(false);
        var removals = permissions is null ? existing.ToList() : existing.Where(r => permissions.Contains(r.Permission)).ToList();
        var written = await WriteAsync([], [], removals, cancellationToken).ConfigureAwait(false);
        if (written.IsFailure)
        {
            return written.Error!;
        }

        if (removals.Count > 0)
        {
            await cache.InvalidateAsync(owner, kind, subjectId, cancellationToken).ConfigureAwait(false);
        }

        return removals.Select(static r => r.Permission).ToHashSet(StringComparer.Ordinal);
    }

    private async Task<Result> WriteAsync(List<SysPermissionRule> additions, List<SysPermissionRule> updates, List<SysPermissionRule> removals, CancellationToken cancellationToken)
    {
        foreach (var outcomes in new[]
        {
            additions.Count > 0 ? await rules.AddRangeAsync(additions, cancellationToken).ConfigureAwait(false) : [],
            updates.Count > 0 ? await rules.UpdateRangeAsync(updates, cancellationToken).ConfigureAwait(false) : [],
            removals.Count > 0 ? await rules.RemoveRangeAsync(removals, cancellationToken).ConfigureAwait(false) : [],
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
}
