// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text.Json;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 约束规则执法领域服务实现
/// </summary>
/// <remarks>
/// 语义约定（与 SysConstraintRuleItem 注释一致）：
/// - 目标匹配必须展开角色继承链：规则指向角色 A，则任何继承了 A 的角色均视为持有 A；
/// - 同组为互斥集合：同一 ConstraintGroup 内最多可同时持有 maxAllowed（默认 1，取自规则 Parameters 的 JSON）个目标；
/// - 一个规则含多个分组时逐组独立判定，任一超限即记一次违规。
/// </remarks>
public sealed class ConstraintRuleEnforcementDomainService
    : IConstraintRuleEnforcementDomainService
{
    private readonly IConstraintRuleRepository _constraintRuleRepository;

    private readonly IConstraintRuleItemRepository _constraintRuleItemRepository;

    private readonly IRoleHierarchyRepository _roleHierarchyRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public ConstraintRuleEnforcementDomainService(
        IConstraintRuleRepository constraintRuleRepository,
        IConstraintRuleItemRepository constraintRuleItemRepository,
        IRoleHierarchyRepository roleHierarchyRepository)
    {
        _constraintRuleRepository = constraintRuleRepository;
        _constraintRuleItemRepository = constraintRuleItemRepository;
        _roleHierarchyRepository = roleHierarchyRepository;
    }

    /// <summary>
    /// 评估指定角色集合在指定约束类型下的违规情况
    /// </summary>
    public async Task<ConstraintEnforcementResult> EvaluateRoleAssignmentsAsync(
        IEnumerable<long> roleIds,
        ConstraintType constraintType,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(roleIds);

        var results = await EvaluateRoleSetsAsync([[.. roleIds]], constraintType, cancellationToken);
        return results[0];
    }

    /// <summary>
    /// 一次评估多组角色集合（规则与继承链只读一次）
    /// </summary>
    public async Task<IReadOnlyList<ConstraintEnforcementResult>> EvaluateRoleSetsAsync(
        IReadOnlyList<IReadOnlyCollection<long>> roleSets,
        ConstraintType constraintType,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(roleSets);
        cancellationToken.ThrowIfCancellationRequested();

        var normalizedSets = roleSets
            .Select(set => set.Where(id => id > 0).Distinct().ToList())
            .ToList();
        var results = Enumerable.Repeat(ConstraintEnforcementResult.Pass, normalizedSets.Count).ToArray();
        if (normalizedSets.All(set => set.Count == 0))
        {
            return results;
        }

        var activeRules = await _constraintRuleRepository.GetActiveRulesAsync(DateTimeOffset.UtcNow, cancellationToken);
        var applicableRules = activeRules
            .Where(rule => rule.ConstraintType == constraintType && rule.TargetType == ConstraintTargetType.Role)
            .OrderByDescending(rule => rule.Priority)
            .ToList();
        if (applicableRules.Count == 0)
        {
            return results;
        }

        var ruleGroups = new List<(SysConstraintRule Rule, int MaxAllowed, IReadOnlyList<IGrouping<int, SysConstraintRuleItem>> Groups)>();
        foreach (var rule in applicableRules)
        {
            var items = await _constraintRuleItemRepository.GetByRuleIdAsync(rule.BasicId, cancellationToken);
            ruleGroups.Add((
                rule,
                ResolveMaxAllowed(rule.Parameters),
                [.. items.Where(item => item.TargetType == ConstraintTargetType.Role).GroupBy(item => item.ConstraintGroup)]));
        }

        // 约束目标判定必须展开角色继承链（含自身），使"继承互斥角色的后代角色"等效命中；
        // 静态约束不看角色启停：停用角色随时可能重新启用，按结构判定
        var graph = RoleInheritanceGraph.FromEdges(await _roleHierarchyRepository.GetEdgesAsync(cancellationToken));

        for (var index = 0; index < normalizedSets.Count; index++)
        {
            var set = normalizedSets[index];
            if (set.Count == 0)
            {
                continue;
            }

            var effectiveRoleIds = new HashSet<long>(set);
            foreach (var roleId in set)
            {
                effectiveRoleIds.UnionWith(graph.AncestorsOf(roleId).Keys);
            }

            var violations = new List<ConstraintViolation>();
            foreach (var (rule, maxAllowed, groups) in ruleGroups)
            {
                foreach (var group in groups)
                {
                    var matched = group
                        .Where(item => effectiveRoleIds.Contains(item.TargetId))
                        .Select(item => item.TargetId)
                        .Distinct()
                        .ToList();
                    if (matched.Count > maxAllowed)
                    {
                        violations.Add(new ConstraintViolation(
                            rule.BasicId,
                            rule.RuleCode,
                            rule.RuleName,
                            rule.ConstraintType,
                            group.Key,
                            matched,
                            rule.ViolationAction));
                    }
                }
            }

            if (violations.Count > 0)
            {
                results[index] = new ConstraintEnforcementResult(violations);
            }
        }

        return results;
    }

    /// <summary>
    /// 解析规则参数中的 maxAllowed（默认 1，非法 JSON / 缺失 / 小于 1 均兜底为 1）。
    /// 规则创建侧已校验参数为合法 JSON，此处兜底只防历史脏数据阻断授权判定。
    /// </summary>
    private static int ResolveMaxAllowed(string? parameters)
    {
        if (string.IsNullOrWhiteSpace(parameters))
        {
            return 1;
        }

        try
        {
            using var document = JsonDocument.Parse(parameters);
            if (document.RootElement.ValueKind == JsonValueKind.Object
                && document.RootElement.TryGetProperty("maxAllowed", out var element)
                && element.TryGetInt32(out var maxAllowed))
            {
                return Math.Max(1, maxAllowed);
            }
        }
        catch (JsonException)
        {
            // 兜底默认值
        }

        return 1;
    }
}
