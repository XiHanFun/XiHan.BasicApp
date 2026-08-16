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

    private readonly IRoleHierarchyDomainService _roleHierarchyDomainService;

    /// <summary>
    /// 构造函数
    /// </summary>
    public ConstraintRuleEnforcementDomainService(
        IConstraintRuleRepository constraintRuleRepository,
        IConstraintRuleItemRepository constraintRuleItemRepository,
        IRoleHierarchyDomainService roleHierarchyDomainService)
    {
        _constraintRuleRepository = constraintRuleRepository;
        _constraintRuleItemRepository = constraintRuleItemRepository;
        _roleHierarchyDomainService = roleHierarchyDomainService;
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
        cancellationToken.ThrowIfCancellationRequested();

        var roleIdList = roleIds.Where(id => id > 0).Distinct().ToList();
        if (roleIdList.Count == 0)
        {
            return ConstraintEnforcementResult.Pass;
        }

        var activeRules = await _constraintRuleRepository.GetActiveRulesAsync(DateTimeOffset.UtcNow, cancellationToken);
        var applicableRules = activeRules
            .Where(rule => rule.ConstraintType == constraintType && rule.TargetType == ConstraintTargetType.Role)
            .OrderByDescending(rule => rule.Priority)
            .ToList();
        if (applicableRules.Count == 0)
        {
            return ConstraintEnforcementResult.Pass;
        }

        // 约束目标判定必须展开角色继承链（含自身），使"继承互斥角色的后代角色"等效命中。
        var effectiveRoleIds = new HashSet<long>(
            await _roleHierarchyDomainService.ExpandRoleHierarchyAsync(roleIdList, cancellationToken));

        var violations = new List<ConstraintViolation>();
        foreach (var rule in applicableRules)
        {
            var items = await _constraintRuleItemRepository.GetByRuleIdAsync(rule.BasicId, cancellationToken);
            var maxAllowed = ResolveMaxAllowed(rule.Parameters);

            foreach (var group in items
                         .Where(item => item.TargetType == ConstraintTargetType.Role)
                         .GroupBy(item => item.ConstraintGroup))
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

        return new ConstraintEnforcementResult(violations);
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
