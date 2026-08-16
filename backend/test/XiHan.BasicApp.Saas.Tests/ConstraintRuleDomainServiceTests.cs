// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Moq;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.MultiTenancy.Abstractions;

namespace XiHan.BasicApp.Saas.Tests;

/// <summary>
/// 约束规则领域服务测试：SSD/DSD/互斥/先决条件约束的结构不变量与目标可用性校验。
/// </summary>
public sealed class ConstraintRuleDomainServiceTests
{
    /// <summary>
    /// 规则项为空时必须拒绝。
    /// </summary>
    [Fact]
    public async Task Create_WithEmptyItems_ShouldThrow()
    {
        var fixture = CreateFixture();
        var command = CreateCommand(items: []);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.CreateConstraintRuleAsync(command));

        Assert.Contains("至少需要一个规则项", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 同一目标（类型+主键）重复出现时必须拒绝。
    /// </summary>
    [Fact]
    public async Task Create_WithDuplicateTargets_ShouldThrow()
    {
        var fixture = CreateFixture();
        var command = CreateCommand(items: [Item(101, 0), Item(101, 1)]);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.CreateConstraintRuleAsync(command));

        Assert.Contains("目标不能重复", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 非先决条件约束的规则项目标类型必须与规则目标类型一致。
    /// </summary>
    [Fact]
    public async Task Create_NonPrerequisiteWithMixedTargetTypes_ShouldThrow()
    {
        var fixture = CreateFixture();
        var command = CreateCommand(
            items:
            [
                Item(ConstraintTargetType.Role, 101, 0),
                Item(ConstraintTargetType.Permission, 201, 0)
            ]);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.CreateConstraintRuleAsync(command));

        Assert.Contains("目标类型必须与规则目标类型一致", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 静态职责分离（SSD）至少需要两个目标项。
    /// </summary>
    [Fact]
    public async Task Create_SsdWithSingleItem_ShouldThrow()
    {
        var fixture = CreateFixture();
        var command = CreateCommand(items: [Item(101, 0)]);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.CreateConstraintRuleAsync(command));

        Assert.Contains("至少需要两个目标项", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 互斥约束同样至少需要两个目标项。
    /// </summary>
    [Fact]
    public async Task Create_MutualExclusionWithSingleItem_ShouldThrow()
    {
        var fixture = CreateFixture();
        var command = CreateCommand(
            constraintType: ConstraintType.MutualExclusion,
            items: [Item(101, 0)]);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.CreateConstraintRuleAsync(command));

        Assert.Contains("至少需要两个目标项", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 先决条件约束必须同时包含必备项分组 0 与目标项分组 1。
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public async Task Create_PrerequisiteWithSingleGroup_ShouldThrow(int group)
    {
        var fixture = CreateFixture();
        var command = CreateCommand(
            constraintType: ConstraintType.Prerequisite,
            items: [Item(101, group), Item(102, group)]);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.CreateConstraintRuleAsync(command));

        Assert.Contains("分组 0 和目标项分组 1", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 约束参数必须是合法 JSON。
    /// </summary>
    [Fact]
    public async Task Create_WithInvalidJsonParameters_ShouldThrow()
    {
        var fixture = CreateFixture();
        var command = CreateCommand(parameters: "not-json");

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.CreateConstraintRuleAsync(command));

        Assert.Contains("合法 JSON", exception.Message, StringComparison.Ordinal);
        fixture.RuleRepository.Verify(
            repo => repo.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<SysConstraintRule, bool>>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// 规则编码不能包含空白字符。
    /// </summary>
    [Fact]
    public async Task Create_WithWhitespaceInRuleCode_ShouldThrow()
    {
        var fixture = CreateFixture();
        var command = CreateCommand(ruleCode: "RULE CODE");

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.CreateConstraintRuleAsync(command));

        Assert.Contains("不能包含空白字符", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 空规则编码必须拒绝。
    /// </summary>
    [Fact]
    public async Task Create_WithBlankRuleCode_ShouldThrow()
    {
        var fixture = CreateFixture();
        var command = CreateCommand(ruleCode: "  ");

        await Assert.ThrowsAsync<ArgumentException>(
            () => fixture.Service.CreateConstraintRuleAsync(command));
    }

    /// <summary>
    /// 失效时间必须晚于当前时间。
    /// </summary>
    [Fact]
    public async Task Create_WithExpirationInPast_ShouldThrow()
    {
        var fixture = CreateFixture();
        var command = CreateCommand(expirationTime: DateTimeOffset.UtcNow.AddMinutes(-1));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.CreateConstraintRuleAsync(command));

        Assert.Contains("失效时间必须晚于当前时间", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 生效时间必须早于失效时间。
    /// </summary>
    [Fact]
    public async Task Create_WithEffectiveAfterExpiration_ShouldThrow()
    {
        var fixture = CreateFixture();
        var command = CreateCommand(
            effectiveTime: DateTimeOffset.UtcNow.AddHours(2),
            expirationTime: DateTimeOffset.UtcNow.AddHours(1));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.CreateConstraintRuleAsync(command));

        Assert.Contains("生效时间必须早于失效时间", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 优先级不能为负数。
    /// </summary>
    [Fact]
    public async Task Create_WithNegativePriority_ShouldThrow()
    {
        var fixture = CreateFixture();
        var command = CreateCommand(priority: -1);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => fixture.Service.CreateConstraintRuleAsync(command));
    }

    /// <summary>
    /// 规则编码已存在时必须拒绝。
    /// </summary>
    [Fact]
    public async Task Create_WithDuplicateRuleCode_ShouldThrow()
    {
        var fixture = CreateFixture();
        fixture.RuleRepository
            .Setup(repo => repo.AnyAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<SysConstraintRule, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var command = CreateCommand();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.CreateConstraintRuleAsync(command));

        Assert.Contains("规则编码已存在", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 停用权限不能配置约束规则。
    /// </summary>
    [Fact]
    public async Task Create_WithDisabledPermissionTarget_ShouldThrow()
    {
        var fixture = CreateFixture();
        fixture.PermissionRepository
            .Setup(repo => repo.GetByIdAsync(201, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreatePermission(201, EnableStatus.Disabled));
        var command = CreateCommand(
            targetType: ConstraintTargetType.Permission,
            items: [Item(ConstraintTargetType.Permission, 201, 0), Item(ConstraintTargetType.Permission, 202, 0)]);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.CreateConstraintRuleAsync(command));

        Assert.Contains("停用权限不能配置约束规则", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 停用角色不能配置约束规则。
    /// </summary>
    [Fact]
    public async Task Create_WithDisabledRoleTarget_ShouldThrow()
    {
        var fixture = CreateFixture();
        fixture.RoleRepository
            .Setup(repo => repo.GetByIdAsync(101, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateRole(101, EnableStatus.Disabled));
        var command = CreateCommand();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.CreateConstraintRuleAsync(command));

        Assert.Contains("停用角色不能配置约束规则", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 未接受邀请的租户成员不能配置约束规则。
    /// </summary>
    [Fact]
    public async Task Create_WithUnacceptedUserTarget_ShouldThrow()
    {
        var fixture = CreateFixture();
        fixture.TenantUserRepository
            .Setup(repo => repo.GetMembershipAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SysTenantUser
            {
                UserId = 301,
                InviteStatus = TenantMemberInviteStatus.Pending,
                Status = ValidityStatus.Valid
            });
        var command = CreateCommand(
            targetType: ConstraintTargetType.User,
            items: [Item(ConstraintTargetType.User, 301, 0), Item(ConstraintTargetType.User, 302, 0)]);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.CreateConstraintRuleAsync(command));

        Assert.Contains("未接受邀请的租户成员不能配置约束规则", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 合法规则创建成功：持久化规则与规则项并返回规则主键。
    /// </summary>
    [Fact]
    public async Task Create_WithValidInput_ShouldPersistRuleAndItems()
    {
        var fixture = CreateFixture();
        fixture.RoleRepository
            .Setup(repo => repo.GetByIdAsync(101, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateRole(101));
        fixture.RoleRepository
            .Setup(repo => repo.GetByIdAsync(102, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateRole(102));
        var command = CreateCommand(parameters: """{"maxAllowed":1}""");

        var result = await fixture.Service.CreateConstraintRuleAsync(command);

        Assert.Equal(500, result.RuleId);
        fixture.RuleRepository.Verify(
            repo => repo.AddAsync(It.Is<SysConstraintRule>(rule =>
                rule.RuleCode == "RULE-SSD-01"
                && rule.ConstraintType == ConstraintType.SSD
                && rule.TargetType == ConstraintTargetType.Role
                && rule.Status == EnableStatus.Enabled
                && rule.Priority == 0),
                It.IsAny<CancellationToken>()),
            Times.Once);
        fixture.ItemRepository.Verify(
            repo => repo.DeleteAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<SysConstraintRuleItem, bool>>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
        fixture.ItemRepository.Verify(
            repo => repo.AddRangeAsync(
                It.Is<IEnumerable<SysConstraintRuleItem>>(items => items.Count() == 2),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// 构造约束规则创建命令（默认两个角色目标项）。
    /// </summary>
    private static ConstraintRuleCreateCommand CreateCommand(
        string? ruleCode = "RULE-SSD-01",
        ConstraintType constraintType = ConstraintType.SSD,
        ConstraintTargetType targetType = ConstraintTargetType.Role,
        string? parameters = null,
        int priority = 0,
        DateTimeOffset? effectiveTime = null,
        DateTimeOffset? expirationTime = null,
        IReadOnlyList<ConstraintRuleItemCommand>? items = null)
    {
        return new ConstraintRuleCreateCommand(
            ruleCode!,
            "职责分离规则",
            constraintType,
            targetType,
            parameters,
            EnableStatus.Enabled,
            ViolationAction.Deny,
            null,
            priority,
            effectiveTime,
            expirationTime,
            null,
            items ??
            [
                Item(targetType, 101, 0),
                Item(targetType, 102, 0)
            ]);
    }

    /// <summary>
    /// 构造规则项命令。
    /// </summary>
    private static ConstraintRuleItemCommand Item(long targetId, int group)
    {
        return Item(ConstraintTargetType.Role, targetId, group);
    }

    /// <summary>
    /// 构造指定目标类型的规则项命令。
    /// </summary>
    private static ConstraintRuleItemCommand Item(ConstraintTargetType targetType, long targetId, int group)
    {
        return new ConstraintRuleItemCommand(targetType, targetId, group, null);
    }

    /// <summary>
    /// 构造启用/停用角色。
    /// </summary>
    private static SysRole CreateRole(long id, EnableStatus status = EnableStatus.Enabled)
    {
        var role = new SysRole
        {
            TenantId = 7,
            RoleCode = $"ROLE-{id}",
            RoleName = $"角色{id}",
            Status = status
        };
        SaasTestHelper.SetBasicId(role, id);
        return role;
    }

    /// <summary>
    /// 构造启用/停用权限。
    /// </summary>
    private static SysPermission CreatePermission(long id, EnableStatus status = EnableStatus.Enabled)
    {
        var permission = new SysPermission
        {
            TenantId = 7,
            PermissionCode = $"saas:res:{id}",
            PermissionName = $"权限{id}",
            Status = status
        };
        SaasTestHelper.SetBasicId(permission, id);
        return permission;
    }

    /// <summary>
    /// 创建带仓储模拟的约束规则测试夹具。
    /// </summary>
    private static ConstraintFixture CreateFixture()
    {
        var ruleRepository = new Mock<IConstraintRuleRepository>();
        ruleRepository
            .Setup(repo => repo.AnyAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<SysConstraintRule, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        ruleRepository
            .Setup(repo => repo.AddAsync(It.IsAny<SysConstraintRule>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SysConstraintRule rule, CancellationToken _) =>
            {
                SaasTestHelper.SetBasicId(rule, 500);
                return rule;
            });

        var itemRepository = new Mock<IConstraintRuleItemRepository>();
        itemRepository
            .Setup(repo => repo.DeleteAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<SysConstraintRuleItem, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        itemRepository
            .Setup(repo => repo.AddRangeAsync(
                It.IsAny<IEnumerable<SysConstraintRuleItem>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((IEnumerable<SysConstraintRuleItem> items, CancellationToken _) => items.ToArray());

        var roleRepository = new Mock<IRoleRepository>();
        var permissionRepository = new Mock<IPermissionRepository>();
        var tenantUserRepository = new Mock<ITenantUserRepository>();
        var currentTenant = new Mock<ICurrentTenant>();
        currentTenant.SetupGet(tenant => tenant.Id).Returns((long?)7);

        var service = new ConstraintRuleDomainService(
            ruleRepository.Object,
            itemRepository.Object,
            roleRepository.Object,
            permissionRepository.Object,
            tenantUserRepository.Object,
            currentTenant.Object);
        return new ConstraintFixture(service, ruleRepository, itemRepository, roleRepository, permissionRepository, tenantUserRepository);
    }

    /// <summary>
    /// 约束规则测试依赖集合。
    /// </summary>
    private sealed record ConstraintFixture(
        ConstraintRuleDomainService Service,
        Mock<IConstraintRuleRepository> RuleRepository,
        Mock<IConstraintRuleItemRepository> ItemRepository,
        Mock<IRoleRepository> RoleRepository,
        Mock<IPermissionRepository> PermissionRepository,
        Mock<ITenantUserRepository> TenantUserRepository);
}
