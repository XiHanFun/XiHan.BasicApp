// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Reflection;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Numbering;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Core.Exceptions;
using XiHan.Framework.MultiTenancy.Abstractions;

namespace XiHan.BasicApp.Saas.Tests;

/// <summary>
/// 业务编号规则格式冻结、删除约束和安全重置领域测试。
/// </summary>
public sealed class NumberingRuleDomainServiceTests
{
    /// <summary>
    /// 首次发号后修改任一格式字段都必须失败。
    /// </summary>
    [Fact]
    public async Task UpdateAsync_ShouldFreezeFormatAfterFirstAllocation()
    {
        var rule = CreateRule(hasAllocated: true);
        var fixture = CreateFixture(rule, tenantId: 7);
        var command = CreateUpdateCommand(rule) with { Prefix = "CHANGED" };

        var exception = await Assert.ThrowsAsync<UserFriendlyException>(() => fixture.Service.UpdateAsync(command));

        Assert.Contains("不可修改", exception.Message, StringComparison.Ordinal);
        fixture.RuleRepository.Verify(
            repository => repository.UpdateAsync(It.IsAny<SysNumberingRule>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// 发号后仍应允许更新名称、备注等非格式元数据。
    /// </summary>
    [Fact]
    public async Task UpdateAsync_ShouldAllowMetadataChangesAfterFirstAllocation()
    {
        var rule = CreateRule(hasAllocated: true);
        var fixture = CreateFixture(rule, tenantId: 7);
        var command = CreateUpdateCommand(rule) with { RuleName = "新名称", Remark = "审计备注" };

        var result = await fixture.Service.UpdateAsync(command);

        Assert.Equal("新名称", result.Rule.RuleName);
        Assert.Equal("审计备注", result.Rule.Remark);
        fixture.RuleRepository.Verify(
            repository => repository.UpdateAsync(rule, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// 未发号规则经过安全重置后，缩短流水位数不能让预留值超过新容量。
    /// </summary>
    [Fact]
    public async Task UpdateAsync_ShouldRejectSerialLengthBelowReservedValueCapacity()
    {
        var rule = CreateRule(hasAllocated: false);
        rule.CurrentValue = 999;
        var fixture = CreateFixture(rule, tenantId: 7);
        var command = CreateUpdateCommand(rule) with { SerialLength = 2 };

        var exception = await Assert.ThrowsAsync<UserFriendlyException>(() => fixture.Service.UpdateAsync(command));

        Assert.Contains("超过新的流水位数容量", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 当前周期已有分配时，下一值不能回退到已使用区间。
    /// </summary>
    [Fact]
    public async Task ResetAsync_ShouldRejectPotentialDuplicateRange()
    {
        var rule = CreateRule(hasAllocated: true);
        rule.ResetCycle = NumberingResetCycle.Never;
        rule.CurrentPeriod = "never";
        rule.CurrentValue = 100;
        var fixture = CreateFixture(rule, tenantId: 7, maximumAllocated: 100);

        var exception = await Assert.ThrowsAsync<UserFriendlyException>(() => fixture.Service.ResetAsync(
            new NumberingRuleResetCommand(rule.BasicId, 100, "纠正外部单据迁移", null)));

        Assert.Contains("不能回退", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 安全重置允许向前跳号，并把当前值保存为下一值减一。
    /// </summary>
    [Fact]
    public async Task ResetAsync_ShouldAllowForwardGap()
    {
        var rule = CreateRule(hasAllocated: true);
        rule.ResetCycle = NumberingResetCycle.Never;
        rule.CurrentPeriod = "never";
        rule.CurrentValue = 100;
        var fixture = CreateFixture(rule, tenantId: 7, maximumAllocated: 100);

        var result = await fixture.Service.ResetAsync(
            new NumberingRuleResetCommand(rule.BasicId, 150, "预留线下历史号段", null));

        Assert.Equal(149, result.Rule.CurrentValue);
        Assert.Equal("never", result.Rule.CurrentPeriod);
    }

    /// <summary>
    /// 全局规则安全重置必须使用规则编码进行二次确认。
    /// </summary>
    [Fact]
    public async Task ResetAsync_GlobalRuleShouldRequireMatchingConfirmationCode()
    {
        var rule = CreateRule(hasAllocated: true, tenantId: 0);
        rule.ResetCycle = NumberingResetCycle.Never;
        var fixture = CreateFixture(rule, tenantId: null);

        var exception = await Assert.ThrowsAsync<UserFriendlyException>(() => fixture.Service.ResetAsync(
            new NumberingRuleResetCommand(rule.BasicId, 2, "平台规则调整", "OTHER")));

        Assert.Contains("确认编码不匹配", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 永久审计要求已经发号的规则不可删除。
    /// </summary>
    [Fact]
    public async Task DeleteAsync_ShouldRejectAllocatedRule()
    {
        var rule = CreateRule(hasAllocated: true);
        var fixture = CreateFixture(rule, tenantId: 7);

        var exception = await Assert.ThrowsAsync<UserFriendlyException>(() => fixture.Service.DeleteAsync(rule.BasicId));

        Assert.Contains("不能删除", exception.Message, StringComparison.Ordinal);
        fixture.RuleRepository.Verify(
            repository => repository.DeleteAsync(It.IsAny<SysNumberingRule>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// 创建带内存仓储行为的领域服务测试夹具。
    /// </summary>
    /// <param name="rule">当前作用域规则。</param>
    /// <param name="tenantId">当前租户；null 表示平台。</param>
    /// <param name="maximumAllocated">当前周期最大已分配值。</param>
    /// <returns>领域服务及其仓储模拟对象。</returns>
    private static DomainFixture CreateFixture(SysNumberingRule rule, long? tenantId, long maximumAllocated = 0)
    {
        var ruleRepository = new Mock<INumberingRuleRepository>();
        ruleRepository
            .Setup(repository => repository.FindByIdInScopeAsync(tenantId ?? 0, rule.BasicId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(rule);
        ruleRepository
            .Setup(repository => repository.UpdateAsync(It.IsAny<SysNumberingRule>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SysNumberingRule entity, CancellationToken _) => entity);

        var allocationRepository = new Mock<INumberingAllocationRepository>();
        allocationRepository
            .Setup(repository => repository.GetMaximumEndValueAsync(rule.TenantId, rule.BasicId, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(maximumAllocated);

        var currentTenant = new Mock<ICurrentTenant>();
        currentTenant.SetupGet(tenant => tenant.Id).Returns(tenantId);

        var service = new NumberingRuleDomainService(
            ruleRepository.Object,
            allocationRepository.Object,
            new NumberingFormatter(),
            currentTenant.Object,
            NullLogger<NumberingRuleDomainService>.Instance);
        return new DomainFixture(service, ruleRepository);
    }

    /// <summary>
    /// 创建具有稳定格式的规则测试数据。
    /// </summary>
    private static SysNumberingRule CreateRule(bool hasAllocated, long tenantId = 7)
    {
        var rule = new SysNumberingRule
        {
            TenantId = tenantId,
            RuleCode = "ORDER",
            RuleName = "订单编号",
            Prefix = "ORD",
            Separator = "-",
            DateFormat = NumberingDateFormat.YyyyMMdd,
            SerialLength = 4,
            ResetCycle = NumberingResetCycle.Daily,
            TimeZoneId = "UTC",
            HasAllocated = hasAllocated,
            Status = EnableStatus.Enabled
        };
        SetEntityId(rule, 1001);
        return rule;
    }

    /// <summary>
    /// 从规则创建保持格式不变的更新命令。
    /// </summary>
    private static NumberingRuleUpdateCommand CreateUpdateCommand(SysNumberingRule rule)
    {
        return new NumberingRuleUpdateCommand(
            rule.BasicId,
            rule.RuleName,
            rule.Prefix,
            rule.Separator,
            rule.DateFormat,
            rule.SerialLength,
            rule.ResetCycle,
            rule.TimeZoneId,
            rule.AllowTenantUse,
            rule.Sort,
            rule.Remark);
    }

    /// <summary>
    /// 测试中模拟持久化层回填受保护的实体主键。
    /// </summary>
    private static void SetEntityId(SysNumberingRule rule, long id)
    {
        var property = rule.GetType().GetProperty(
            nameof(SysNumberingRule.BasicId),
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("未找到实体主键属性。");
        property.SetValue(rule, id);
    }

    /// <summary>
    /// 领域测试依赖集合。
    /// </summary>
    private sealed record DomainFixture(
        NumberingRuleDomainService Service,
        Mock<INumberingRuleRepository> RuleRepository);
}
