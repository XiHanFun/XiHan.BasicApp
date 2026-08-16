// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Moq;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Repositories;

namespace XiHan.BasicApp.Saas.Tests;

/// <summary>
/// 约束规则执法领域服务测试：SSD/DSD 评估的继承链展开、分组互斥与 maxAllowed 语义。
/// </summary>
public sealed class ConstraintRuleEnforcementDomainServiceTests
{
    /// <summary>
    /// 无生效规则时直接通过。
    /// </summary>
    [Fact]
    public async Task Evaluate_WithNoActiveRules_ShouldPass()
    {
        var fixture = CreateFixture(activeRules: [], itemsByRuleId: new Dictionary<long, IReadOnlyList<SysConstraintRuleItem>>());

        var result = await fixture.Service.EvaluateRoleAssignmentsAsync([101, 102], ConstraintType.SSD);

        Assert.False(result.HasViolations);
    }

    /// <summary>
    /// 空角色集合直接通过且不查询规则。
    /// </summary>
    [Fact]
    public async Task Evaluate_WithEmptyRoleIds_ShouldPassWithoutQueryingRules()
    {
        var fixture = CreateFixture(activeRules: [], itemsByRuleId: new Dictionary<long, IReadOnlyList<SysConstraintRuleItem>>());

        var result = await fixture.Service.EvaluateRoleAssignmentsAsync([], ConstraintType.SSD);

        Assert.False(result.HasViolations);
        fixture.RuleRepository.Verify(
            repo => repo.GetActiveRulesAsync(It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// 同组两个互斥目标都被持有时必须报告违规。
    /// </summary>
    [Fact]
    public async Task Evaluate_WhenBothTargetsHeld_ShouldReportViolation()
    {
        var rule = CreateRule(1, "SSD-01");
        var fixture = CreateFixture(
            activeRules: [rule],
            itemsByRuleId: new Dictionary<long, IReadOnlyList<SysConstraintRuleItem>>
            {
                [1] = [CreateItem(1, 101, 0), CreateItem(1, 102, 0)]
            });

        var result = await fixture.Service.EvaluateRoleAssignmentsAsync([101, 102], ConstraintType.SSD);

        var violation = Assert.Single(result.Violations);
        Assert.Equal("SSD-01", violation.RuleCode);
        Assert.Equal(1, violation.RuleId);
        Assert.Equal(0, violation.ConstraintGroup);
        Assert.Equal([101L, 102L], violation.MatchedTargetIds);
        Assert.Equal(ViolationAction.Deny, violation.ViolationAction);
        Assert.NotNull(result.FirstBlockingViolation);
    }

    /// <summary>
    /// 仅持有一个互斥目标时通过。
    /// </summary>
    [Fact]
    public async Task Evaluate_WhenOnlyOneTargetHeld_ShouldPass()
    {
        var rule = CreateRule(1, "SSD-01");
        var fixture = CreateFixture(
            activeRules: [rule],
            itemsByRuleId: new Dictionary<long, IReadOnlyList<SysConstraintRuleItem>>
            {
                [1] = [CreateItem(1, 101, 0), CreateItem(1, 102, 0)]
            });

        var result = await fixture.Service.EvaluateRoleAssignmentsAsync([101], ConstraintType.SSD);

        Assert.False(result.HasViolations);
    }

    /// <summary>
    /// 核心继承语义：持有继承互斥角色的后代角色应等效命中（持有 C 继承 A，规则互斥 A/B → 违规）。
    /// </summary>
    [Fact]
    public async Task Evaluate_ShouldExpandHierarchyBeforeMatching()
    {
        var rule = CreateRule(1, "SSD-01");
        var fixture = CreateFixture(
            activeRules: [rule],
            itemsByRuleId: new Dictionary<long, IReadOnlyList<SysConstraintRuleItem>>
            {
                [1] = [CreateItem(1, 101, 0), CreateItem(1, 102, 0)]
            },
            hierarchyExpansion: ids => ids.Concat([101L]).ToList());

        // 用户持有角色 103（其继承链包含 101），同时持有 102
        var result = await fixture.Service.EvaluateRoleAssignmentsAsync([103, 102], ConstraintType.SSD);

        var violation = Assert.Single(result.Violations);
        Assert.Equal([101L, 102L], violation.MatchedTargetIds);
    }

    /// <summary>
    /// maxAllowed 参数生效：允许同组持有 N 个目标。
    /// </summary>
    [Fact]
    public async Task Evaluate_ShouldHonorMaxAllowedFromParameters()
    {
        var rule = CreateRule(1, "SSD-02", parameters: """{"maxAllowed":2}""");
        var fixture = CreateFixture(
            activeRules: [rule],
            itemsByRuleId: new Dictionary<long, IReadOnlyList<SysConstraintRuleItem>>
            {
                [1] = [CreateItem(1, 101, 0), CreateItem(1, 102, 0), CreateItem(1, 103, 0)]
            });

        var passResult = await fixture.Service.EvaluateRoleAssignmentsAsync([101, 102], ConstraintType.SSD);
        Assert.False(passResult.HasViolations);

        var violationResult = await fixture.Service.EvaluateRoleAssignmentsAsync([101, 102, 103], ConstraintType.SSD);
        Assert.Single(violationResult.Violations);
    }

    /// <summary>
    /// 参数非法 JSON 时兜底按默认 maxAllowed=1 判定，不因脏数据放行。
    /// </summary>
    [Fact]
    public async Task Evaluate_WithInvalidJsonParameters_ShouldDefaultToMaxAllowedOne()
    {
        var rule = CreateRule(1, "SSD-03", parameters: "not-json");
        var fixture = CreateFixture(
            activeRules: [rule],
            itemsByRuleId: new Dictionary<long, IReadOnlyList<SysConstraintRuleItem>>
            {
                [1] = [CreateItem(1, 101, 0), CreateItem(1, 102, 0)]
            });

        var result = await fixture.Service.EvaluateRoleAssignmentsAsync([101, 102], ConstraintType.SSD);

        Assert.True(result.HasViolations);
    }

    /// <summary>
    /// 只评估请求的约束类型，其它类型规则不参与。
    /// </summary>
    [Fact]
    public async Task Evaluate_ShouldSkipRulesOfOtherConstraintTypes()
    {
        var ssdRule = CreateRule(1, "SSD-01", ConstraintType.SSD);
        var dsdRule = CreateRule(2, "DSD-01", ConstraintType.DSD);
        var fixture = CreateFixture(
            activeRules: [ssdRule, dsdRule],
            itemsByRuleId: new Dictionary<long, IReadOnlyList<SysConstraintRuleItem>>
            {
                [1] = [CreateItem(1, 101, 0), CreateItem(1, 102, 0)],
                [2] = [CreateItem(2, 201, 0), CreateItem(2, 202, 0)]
            });

        var result = await fixture.Service.EvaluateRoleAssignmentsAsync([101, 102, 201, 202], ConstraintType.SSD);

        var violation = Assert.Single(result.Violations);
        Assert.Equal(ConstraintType.SSD, violation.ConstraintType);
    }

    /// <summary>
    /// 目标类型非角色的规则不参与角色授予评估。
    /// </summary>
    [Fact]
    public async Task Evaluate_ShouldSkipRulesTargetingNonRole()
    {
        var rule = CreateRule(1, "SSD-04", targetType: ConstraintTargetType.Permission);
        var fixture = CreateFixture(
            activeRules: [rule],
            itemsByRuleId: new Dictionary<long, IReadOnlyList<SysConstraintRuleItem>>
            {
                [1] = [CreateItem(1, 101, 0, ConstraintTargetType.Permission), CreateItem(1, 102, 0, ConstraintTargetType.Permission)]
            });

        var result = await fixture.Service.EvaluateRoleAssignmentsAsync([101, 102], ConstraintType.SSD);

        Assert.False(result.HasViolations);
    }

    /// <summary>
    /// 一个规则含多个分组时逐组独立判定，各组分别产生违规。
    /// </summary>
    [Fact]
    public async Task Evaluate_ShouldReportViolationPerGroup()
    {
        var rule = CreateRule(1, "SSD-05");
        var fixture = CreateFixture(
            activeRules: [rule],
            itemsByRuleId: new Dictionary<long, IReadOnlyList<SysConstraintRuleItem>>
            {
                [1] =
                [
                    CreateItem(1, 101, 0), CreateItem(1, 102, 0),
                    CreateItem(1, 201, 1), CreateItem(1, 202, 1)
                ]
            });

        var result = await fixture.Service.EvaluateRoleAssignmentsAsync([101, 102, 201, 202], ConstraintType.SSD);

        Assert.Equal(2, result.Violations.Count);
        Assert.Contains(result.Violations, violation => violation.ConstraintGroup == 0);
        Assert.Contains(result.Violations, violation => violation.ConstraintGroup == 1);
    }

    /// <summary>
    /// 多规则违规时按规则优先级降序排列（高优先级规则先于低优先级）。
    /// </summary>
    [Fact]
    public async Task Evaluate_ShouldOrderViolationsByRulePriorityDescending()
    {
        var lowRule = CreateRule(1, "LOW", priority: 5);
        var highRule = CreateRule(2, "HIGH", priority: 9);
        var fixture = CreateFixture(
            activeRules: [lowRule, highRule],
            itemsByRuleId: new Dictionary<long, IReadOnlyList<SysConstraintRuleItem>>
            {
                [1] = [CreateItem(1, 101, 0), CreateItem(1, 102, 0)],
                [2] = [CreateItem(2, 201, 0), CreateItem(2, 202, 0)]
            });

        var result = await fixture.Service.EvaluateRoleAssignmentsAsync([101, 102, 201, 202], ConstraintType.SSD);

        Assert.Equal("HIGH", result.Violations[0].RuleCode);
    }

    /// <summary>
    /// 非正角色主键过滤后再评估。
    /// </summary>
    [Fact]
    public async Task Evaluate_ShouldFilterNonPositiveRoleIds()
    {
        var rule = CreateRule(1, "SSD-01");
        List<IEnumerable<long>>? capturedInputs = [];
        var fixture = CreateFixture(
            activeRules: [rule],
            itemsByRuleId: new Dictionary<long, IReadOnlyList<SysConstraintRuleItem>>
            {
                [1] = [CreateItem(1, 101, 0), CreateItem(1, 102, 0)]
            },
            hierarchyExpansion: ids =>
            {
                capturedInputs.Add(ids.ToList());
                return ids.ToList();
            });

        var result = await fixture.Service.EvaluateRoleAssignmentsAsync([0, -1, 101, 102], ConstraintType.SSD);

        Assert.True(result.HasViolations);
        Assert.Equal([101L, 102L], Assert.Single(capturedInputs));
    }

    /// <summary>
    /// 已取消令牌必须立即抛出。
    /// </summary>
    [Fact]
    public async Task Evaluate_Cancelled_ShouldThrow()
    {
        var fixture = CreateFixture(activeRules: [], itemsByRuleId: new Dictionary<long, IReadOnlyList<SysConstraintRuleItem>>());
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => fixture.Service.EvaluateRoleAssignmentsAsync([101], ConstraintType.SSD, cts.Token));
    }

    /// <summary>
    /// 构造约束规则。
    /// </summary>
    private static SysConstraintRule CreateRule(
        long id,
        string code,
        ConstraintType type = ConstraintType.SSD,
        ConstraintTargetType targetType = ConstraintTargetType.Role,
        ViolationAction action = ViolationAction.Deny,
        string? parameters = null,
        int priority = 0)
    {
        var rule = new SysConstraintRule
        {
            TenantId = 7,
            RuleCode = code,
            RuleName = $"规则{code}",
            ConstraintType = type,
            TargetType = targetType,
            Parameters = parameters,
            ViolationAction = action,
            Priority = priority,
            Status = EnableStatus.Enabled
        };
        SaasTestHelper.SetBasicId(rule, id);
        return rule;
    }

    /// <summary>
    /// 构造约束规则项。
    /// </summary>
    private static SysConstraintRuleItem CreateItem(long ruleId, long targetId, int group, ConstraintTargetType targetType = ConstraintTargetType.Role)
    {
        return new SysConstraintRuleItem
        {
            ConstraintRuleId = ruleId,
            TargetType = targetType,
            TargetId = targetId,
            ConstraintGroup = group
        };
    }

    /// <summary>
    /// 创建带仓储模拟的约束执法测试夹具。
    /// </summary>
    private static EnforcementFixture CreateFixture(
        IReadOnlyList<SysConstraintRule> activeRules,
        IReadOnlyDictionary<long, IReadOnlyList<SysConstraintRuleItem>> itemsByRuleId,
        Func<IEnumerable<long>, IReadOnlyList<long>>? hierarchyExpansion = null)
    {
        var ruleRepository = new Mock<IConstraintRuleRepository>();
        ruleRepository
            .Setup(repo => repo.GetActiveRulesAsync(It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(activeRules);

        var itemRepository = new Mock<IConstraintRuleItemRepository>();
        itemRepository
            .Setup(repo => repo.GetByRuleIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((long ruleId, CancellationToken _) =>
                itemsByRuleId.TryGetValue(ruleId, out var items) ? items : []);

        var hierarchy = new Mock<IRoleHierarchyDomainService>();
        hierarchy
            .Setup(service => service.ExpandRoleHierarchyAsync(It.IsAny<IEnumerable<long>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IEnumerable<long> ids, CancellationToken _) =>
                hierarchyExpansion is null ? ids.ToList() : hierarchyExpansion(ids));

        var service = new ConstraintRuleEnforcementDomainService(
            ruleRepository.Object,
            itemRepository.Object,
            hierarchy.Object);
        return new EnforcementFixture(service, ruleRepository);
    }

    /// <summary>
    /// 约束执法测试依赖集合。
    /// </summary>
    private sealed record EnforcementFixture(
        ConstraintRuleEnforcementDomainService Service,
        Mock<IConstraintRuleRepository> RuleRepository);
}
