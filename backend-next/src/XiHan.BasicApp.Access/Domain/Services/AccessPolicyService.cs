// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Access.Domain.Entities;
using XiHan.BasicApp.Access.Infrastructure;
using XiHan.BasicApp.Contracts;
using XiHan.Framework.Domain.Specifications;
using XiHan.Framework.Persistence.Abstractions.Identifiers;
using XiHan.Framework.Persistence.Abstractions.Queries;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;
using XiHan.Framework.Security.Abstractions.Conditions;

namespace XiHan.BasicApp.Access.Domain.Services;

/// <summary>
/// 访问策略的一条条件（写入用）。
/// </summary>
/// <param name="GroupNo">组号（同组取且、组间取或）。</param>
/// <param name="AttributeName">属性名。</param>
/// <param name="Operator">运算符。</param>
/// <param name="IsNegated">是否取反。</param>
/// <param name="Value">条件值（判空类运算符不填）。</param>
/// <param name="Description">说明。</param>
public sealed record PolicyConditionDefinition(int GroupNo, string AttributeName, ConditionOperator Operator, bool IsNegated, string? Value, string? Description);

/// <summary>
/// 访问策略的内容（新建与修改共用；动作与状态另有操作）。
/// </summary>
/// <param name="Code">编码。</param>
/// <param name="Name">名称。</param>
/// <param name="Description">说明。</param>
/// <param name="Priority">优先级（越小越先求值）。</param>
/// <param name="EffectiveAt">生效时间。</param>
/// <param name="ExpiresAt">失效时间。</param>
/// <param name="TemplateCode">来自哪个模板。</param>
/// <param name="Remark">备注。</param>
/// <param name="Conditions">条件（整组替换）。</param>
public sealed record AccessPolicyDefinition(string Code, string Name, string? Description, int Priority, DateTimeOffset? EffectiveAt, DateTimeOffset? ExpiresAt, string? TemplateCode, string? Remark, IReadOnlyList<PolicyConditionDefinition> Conditions);

/// <summary>
/// 访问策略的写入规则（方案 5.9，4.x 的 AccessPolicyDomainService）：编码同一侧唯一、不含空白；至少 1 条、最多 100 条条件与 20 个组，整组按条件引擎编译；
/// 新建一律只记录、启用，动作与状态只经 <see cref="SetActionAsync"/> 与 <see cref="SetStatusAsync"/> 修改；改动之后仍是启用中、此刻有效的拒绝策略时，
/// 按发起这次修改的请求求值，命中即拒绝保存（防自锁，4.x 同）。写入后这一侧的策略缓存失效。
/// </summary>
/// <param name="policies">策略仓储。</param>
/// <param name="conditions">策略条件仓储。</param>
/// <param name="cache">策略缓存。</param>
/// <param name="evaluator">策略求值。</param>
/// <param name="ids">标识生成器。</param>
/// <param name="services">服务提供器（属性来源查库）。</param>
/// <param name="options">授权包配置。</param>
/// <param name="clock">时间源。</param>
public sealed class AccessPolicyService(
    IRepository<SysAccessPolicy, Guid> policies,
    IRepository<SysAccessPolicyCondition, Guid> conditions,
    AccessPolicies cache,
    AccessPolicyEvaluator evaluator,
    IIdGenerator<Guid> ids,
    IServiceProvider services,
    AccessOptions options,
    TimeProvider clock)
{
    /// <summary>
    /// 按 ID 取这一侧的策略。
    /// </summary>
    public async Task<Maybe<SysAccessPolicy>> FindAsync(Guid id, string owner, CancellationToken cancellationToken)
    {
        var found = await policies.FindAsync(id, cancellationToken).ConfigureAwait(false);
        return found.TryGetValue(out var policy) && string.Equals(policy.OwnerTenantId, owner, StringComparison.Ordinal) ? Maybe.Some(policy) : Maybe.None<SysAccessPolicy>();
    }

    /// <summary>
    /// 按编码取这一侧的策略。
    /// </summary>
    public async Task<Maybe<SysAccessPolicy>> FindByCodeAsync(string owner, string code, CancellationToken cancellationToken)
    {
        var rows = await policies.ListAsync(EntityQuery.All<SysAccessPolicy>().Where(new Specification<SysAccessPolicy>(p => p.OwnerTenantId == owner && p.Code == code)).Page(0, 1), cancellationToken).ConfigureAwait(false);
        return rows.Count > 0 ? Maybe.Some(rows[0]) : Maybe.None<SysAccessPolicy>();
    }

    /// <summary>
    /// 一批策略的条件，按写入顺序。
    /// </summary>
    public async Task<ILookup<Guid, SysAccessPolicyCondition>> ConditionsOfAsync(string owner, IReadOnlyCollection<Guid> policyIds, CancellationToken cancellationToken)
    {
        if (policyIds.Count == 0)
        {
            return Array.Empty<SysAccessPolicyCondition>().ToLookup(static c => c.PolicyId);
        }

        var ids = policyIds.ToList();
        var rows = await conditions.ListAsync(EntityQuery.All<SysAccessPolicyCondition>().Where(new Specification<SysAccessPolicyCondition>(c => c.OwnerTenantId == owner && ids.Contains(c.PolicyId))), cancellationToken).ConfigureAwait(false);
        return rows.OrderBy(static c => c.Ordinal).ToLookup(static c => c.PolicyId);
    }

    /// <summary>
    /// 新建（一律只记录、启用）。
    /// </summary>
    public async Task<Result<SysAccessPolicy>> CreateAsync(string owner, AccessPolicyDefinition definition, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(definition);
        var policy = new SysAccessPolicy { Id = ids.NewId(), OwnerTenantId = owner, Action = AccessPolicyAction.LogOnly, Status = AccessPolicyStatus.Active };
        var applied = await ApplyAsync(policy, definition, cancellationToken).ConfigureAwait(false);
        if (applied.IsFailure)
        {
            return applied.Error!;
        }

        var added = await policies.AddAsync(policy, cancellationToken).ConfigureAwait(false);
        if (added.IsFailure)
        {
            return added.Error!;
        }

        var written = await ReplaceConditionsAsync(policy, definition.Conditions, cancellationToken).ConfigureAwait(false);
        if (written.IsFailure)
        {
            return written.Error!;
        }

        await cache.InvalidateAsync(owner, cancellationToken).ConfigureAwait(false);
        return policy;
    }

    /// <summary>
    /// 按模板写入一条预置策略（种子用：带模板的动作与状态）。
    /// </summary>
    public async Task<Result> SeedAsync(string owner, AccessPolicyTemplate template, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(template);
        var created = await CreateAsync(owner, new AccessPolicyDefinition(template.Code, template.Name, template.Description, template.Priority, null, null, template.Code, null, template.Conditions), cancellationToken).ConfigureAwait(false);
        if (!created.TryGetValue(out var policy))
        {
            return created.Error!;
        }

        policy.Action = template.Action;
        policy.Status = template.Status;
        var updated = await policies.UpdateAsync(policy, cancellationToken).ConfigureAwait(false);
        if (updated.IsFailure)
        {
            return updated;
        }

        await cache.InvalidateAsync(owner, cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    /// <summary>
    /// 整体修改内容（动作与状态不变）。
    /// </summary>
    /// <param name="policy">策略。</param>
    /// <param name="definition">新内容。</param>
    /// <param name="current">发起这次修改的请求（防自锁）。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    public async Task<Result> UpdateAsync(SysAccessPolicy policy, AccessPolicyDefinition definition, ConditionRequest current, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(policy);
        ArgumentNullException.ThrowIfNull(definition);
        var applied = await ApplyAsync(policy, definition, cancellationToken).ConfigureAwait(false);
        if (applied.IsFailure)
        {
            return applied;
        }

        var guarded = await GuardAsync(policy, definition.Conditions, current, cancellationToken).ConfigureAwait(false);
        if (guarded.IsFailure)
        {
            return guarded;
        }

        var updated = await policies.UpdateAsync(policy, cancellationToken).ConfigureAwait(false);
        if (updated.IsFailure)
        {
            return updated;
        }

        var written = await ReplaceConditionsAsync(policy, definition.Conditions, cancellationToken).ConfigureAwait(false);
        if (written.IsFailure)
        {
            return written;
        }

        await cache.InvalidateAsync(policy.OwnerTenantId, cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    /// <summary>
    /// 改动作（只记录、拒绝、放行）。
    /// </summary>
    public Task<Result> SetActionAsync(SysAccessPolicy policy, AccessPolicyAction action, ConditionRequest current, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(policy);
        policy.Action = action;
        return SaveStateAsync(policy, current, cancellationToken);
    }

    /// <summary>
    /// 启用或停用。
    /// </summary>
    public Task<Result> SetStatusAsync(SysAccessPolicy policy, AccessPolicyStatus status, ConditionRequest current, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(policy);
        policy.Status = status;
        return SaveStateAsync(policy, current, cancellationToken);
    }

    /// <summary>
    /// 删除（连同条件）。
    /// </summary>
    public async Task<Result> DeleteAsync(SysAccessPolicy policy, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(policy);
        var cleared = await ReplaceConditionsAsync(policy, [], cancellationToken).ConfigureAwait(false);
        if (cleared.IsFailure)
        {
            return cleared;
        }

        var removed = await policies.RemoveAsync(policy, cancellationToken).ConfigureAwait(false);
        if (removed.IsFailure)
        {
            return removed;
        }

        await cache.InvalidateAsync(policy.OwnerTenantId, cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    /// <summary>
    /// 校验一组条件并编译（试运行与写入共用）：至少 1 条、最多 100 条与 20 个组，编译不过返回引擎的错误。
    /// </summary>
    public Result<CompiledConditionSet> Compile(IReadOnlyList<PolicyConditionDefinition> definitions)
    {
        ArgumentNullException.ThrowIfNull(definitions);
        if (definitions.Count == 0)
        {
            return Error.Validation(AccessErrors.PolicyConditionsRequired, AccessMessages.PolicyConditionsRequired);
        }

        if (definitions.Count > SysAccessPolicy.MaxConditions || definitions.Select(static c => c.GroupNo).Distinct().Count() > SysAccessPolicy.MaxGroups)
        {
            return Error.Validation(AccessErrors.PolicyConditionsExceeded, AccessMessages.PolicyConditionsExceeded, MessageArguments.Of(("maxGroups", SysAccessPolicy.MaxGroups), ("maxConditions", SysAccessPolicy.MaxConditions)));
        }

        var compiled = evaluator.Compile(ViewOf(Guid.Empty, string.Empty, string.Empty, AccessPolicyAction.LogOnly, 0, null, null, definitions));
        if (compiled.Errors.Count > 0)
        {
            return compiled.Errors[0].Error;
        }

        return compiled;
    }

    /// <summary>
    /// 判定用的视图。
    /// </summary>
    public static AccessPolicyView ViewOf(Guid id, string code, string name, AccessPolicyAction action, int priority, DateTimeOffset? effectiveAt, DateTimeOffset? expiresAt, IEnumerable<PolicyConditionDefinition> definitions)
        => new(id, code, name, action, priority, effectiveAt, expiresAt, [.. definitions.Select(static c => new PolicyClauseView(c.GroupNo, c.AttributeName.Trim(), c.Operator, c.IsNegated, ValueOf(c)))]);

    private async Task<Result> SaveStateAsync(SysAccessPolicy policy, ConditionRequest current, CancellationToken cancellationToken)
    {
        var existing = (await ConditionsOfAsync(policy.OwnerTenantId, [policy.Id], cancellationToken).ConfigureAwait(false))[policy.Id]
            .Select(static c => new PolicyConditionDefinition(c.GroupNo, c.AttributeName, c.Operator, c.IsNegated, c.Value, c.Description))
            .ToList();
        var guarded = await GuardAsync(policy, existing, current, cancellationToken).ConfigureAwait(false);
        if (guarded.IsFailure)
        {
            return guarded;
        }

        var updated = await policies.UpdateAsync(policy, cancellationToken).ConfigureAwait(false);
        if (updated.IsFailure)
        {
            return updated;
        }

        await cache.InvalidateAsync(policy.OwnerTenantId, cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    // 改动之后仍是启用中、此刻有效的拒绝策略：按发起修改的请求求值，命中即拒绝保存
    private async Task<Result> GuardAsync(SysAccessPolicy policy, IReadOnlyList<PolicyConditionDefinition> definitions, ConditionRequest current, CancellationToken cancellationToken)
    {
        if (policy.Action != AccessPolicyAction.Deny || policy.Status != AccessPolicyStatus.Active || !policy.IsEffectiveAt(clock.GetUtcNow()))
        {
            return Result.Success();
        }

        var view = ViewOf(policy.Id, policy.Code, policy.Name, policy.Action, policy.Priority, null, null, definitions);
        var layer = await evaluator.EvaluateAsync([view], ConditionAttributeSource.Create(current, services, options), current.Time, string.Equals(policy.OwnerTenantId, TenantIds.Platform, StringComparison.Ordinal), cancellationToken).ConfigureAwait(false);
        return layer.BlockedBy is null
            ? Result.Success()
            : Error.Validation(AccessErrors.PolicySelfLockout, AccessMessages.PolicySelfLockout, MessageArguments.Of(("code", policy.Code)));
    }

    private async Task<Result> ApplyAsync(SysAccessPolicy policy, AccessPolicyDefinition definition, CancellationToken cancellationToken)
    {
        var code = definition.Code.Trim();
        if (code.Length == 0 || code.Any(char.IsWhiteSpace))
        {
            return Error.Validation(AccessErrors.PolicyCodeInvalid, AccessMessages.PolicyCodeInvalid, MessageArguments.Of(("code", definition.Code)));
        }

        if (definition.EffectiveAt is { } from && definition.ExpiresAt is { } to && to <= from)
        {
            return Error.Validation(AccessErrors.PolicyWindowInvalid, AccessMessages.PolicyWindowInvalid);
        }

        var template = string.IsNullOrWhiteSpace(definition.TemplateCode) ? null : definition.TemplateCode.Trim();
        if (template is not null && AccessPolicyTemplates.Find(template) is null)
        {
            return Error.Validation(AccessErrors.PolicyTemplateUnknown, AccessMessages.PolicyTemplateUnknown, MessageArguments.Of(("code", template)));
        }

        var compiled = Compile(definition.Conditions);
        if (compiled.IsFailure)
        {
            return compiled.Error!;
        }

        if ((await FindByCodeAsync(policy.OwnerTenantId, code, cancellationToken).ConfigureAwait(false)).TryGetValue(out var taken) && taken.Id != policy.Id)
        {
            return Error.Conflict(AccessErrors.PolicyCodeTaken, AccessMessages.PolicyCodeTaken, MessageArguments.Of(("code", code)));
        }

        policy.Code = code;
        policy.Name = definition.Name.Trim();
        policy.Description = Trimmed(definition.Description);
        policy.Priority = definition.Priority;
        policy.EffectiveAt = definition.EffectiveAt;
        policy.ExpiresAt = definition.ExpiresAt;
        policy.TemplateCode = template;
        policy.Remark = Trimmed(definition.Remark);
        return Result.Success();
    }

    private async Task<Result> ReplaceConditionsAsync(SysAccessPolicy policy, IReadOnlyList<PolicyConditionDefinition> definitions, CancellationToken cancellationToken)
    {
        var existing = (await ConditionsOfAsync(policy.OwnerTenantId, [policy.Id], cancellationToken).ConfigureAwait(false))[policy.Id].ToList();
        if (existing.Count > 0)
        {
            foreach (var outcome in await conditions.RemoveRangeAsync(existing, cancellationToken).ConfigureAwait(false))
            {
                if (outcome.IsFailure)
                {
                    return outcome;
                }
            }
        }

        var rows = definitions.Select((c, index) => new SysAccessPolicyCondition
        {
            Id = ids.NewId(),
            OwnerTenantId = policy.OwnerTenantId,
            PolicyId = policy.Id,
            Ordinal = index,
            GroupNo = c.GroupNo,
            AttributeName = c.AttributeName.Trim(),
            Operator = c.Operator,
            IsNegated = c.IsNegated,
            Value = ValueOf(c),
            Description = Trimmed(c.Description),
        }).ToList();
        if (rows.Count == 0)
        {
            return Result.Success();
        }

        foreach (var outcome in await conditions.AddRangeAsync(rows, cancellationToken).ConfigureAwait(false))
        {
            if (outcome.IsFailure)
            {
                return outcome;
            }
        }

        return Result.Success();
    }

    private static string? ValueOf(PolicyConditionDefinition condition)
        => condition.Operator is ConditionOperator.IsNull or ConditionOperator.IsNotNull ? null : Trimmed(condition.Value);

    private static string? Trimmed(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
