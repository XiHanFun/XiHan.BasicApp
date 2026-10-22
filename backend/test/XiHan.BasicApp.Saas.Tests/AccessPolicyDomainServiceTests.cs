// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Linq.Expressions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Moq;
using XiHan.BasicApp.Saas.Application.AccessPolicies;
using XiHan.BasicApp.Saas.Application.Conditions;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Domain.Conditions;
using XiHan.BasicApp.Saas.Domain.Configurations;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Authorization.Conditions;
using static XiHan.BasicApp.Saas.Tests.AccessPolicyTestKit;
using ConditionOperator = XiHan.BasicApp.Saas.Domain.Entities.ConditionOperator;

namespace XiHan.BasicApp.Saas.Tests;

/// <summary>
/// 访问策略的写路径与防锁死：新建一律只记录、空条件与写错的条件拒绝保存、会拦下当前请求的拒绝策略不让保存
/// </summary>
public sealed class AccessPolicyDomainServiceTests
{
    private static readonly AccessPolicyConditionCommand BlockedNetwork = Clause(SaasConditionAttributes.EnvironmentIp, ConditionOperator.In, "203.0.113.0/24");

    /// <summary>
    /// 新建一律是只记录，条件随之保存
    /// </summary>
    [Fact]
    public async Task Create_IsAlwaysLogOnly()
    {
        var fixture = new Fixture();

        var result = await fixture.Service.CreateAccessPolicyAsync(Create("block-scanners", BlockedNetwork));

        var policy = fixture.Policies.Single(row => row.BasicId == result.PolicyId);
        Assert.Equal((AccessPolicyAction.LogOnly, EnableStatus.Enabled), (policy.Action, policy.Status));
        Assert.Equal("203.0.113.0/24", Assert.Single(fixture.Conditions).ConditionValue);
    }

    /// <summary>
    /// 空条件、写错的条件、重复或带空白的编码都拒绝保存
    /// </summary>
    [Fact]
    public async Task Create_RejectsInvalidInput()
    {
        var fixture = new Fixture();
        _ = await fixture.Service.CreateAccessPolicyAsync(Create("block-scanners", BlockedNetwork));

        var empty = await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.CreateAccessPolicyAsync(Create("empty")));
        var broken = await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.CreateAccessPolicyAsync(Create("broken", Clause("resource.status", ConditionOperator.Equals, "draft"))));
        var duplicate = await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.CreateAccessPolicyAsync(Create("block-scanners", BlockedNetwork)));
        var spaced = await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.CreateAccessPolicyAsync(Create("block scanners", BlockedNetwork)));

        Assert.Contains("空条件", empty.Message, StringComparison.Ordinal);
        Assert.Contains("resource.status", broken.Message, StringComparison.Ordinal);
        Assert.Contains("已存在", duplicate.Message, StringComparison.Ordinal);
        Assert.Contains("空白", spaced.Message, StringComparison.Ordinal);
        Assert.Single(fixture.Policies);
    }

    /// <summary>
    /// 更新整体替换条件；动作单独改
    /// </summary>
    [Fact]
    public async Task Update_ReplacesConditionsAndActionChangesSeparately()
    {
        var fixture = new Fixture();
        var id = (await fixture.Service.CreateAccessPolicyAsync(Create("block-scanners", BlockedNetwork))).PolicyId;

        _ = await fixture.Service.UpdateAccessPolicyAsync(new AccessPolicyUpdateCommand(id, "拦截扫描器", null, 10, null, null, null,
            [Clause(SaasConditionAttributes.EnvironmentClientUserAgent, ConditionOperator.Contains, "sqlmap"), Clause(SaasConditionAttributes.ResourcePath, ConditionOperator.StartsWith, "/api", group: 1)]));
        _ = await fixture.Service.UpdateAccessPolicyActionAsync(new AccessPolicyActionCommand(id, AccessPolicyAction.Deny));

        var policy = Assert.Single(fixture.Policies);
        Assert.Equal(("拦截扫描器", 10, AccessPolicyAction.Deny), (policy.PolicyName, policy.Priority, policy.Action));
        Assert.Equal([SaasConditionAttributes.EnvironmentClientUserAgent, SaasConditionAttributes.ResourcePath], fixture.Conditions.Select(condition => condition.AttributeName));
    }

    /// <summary>
    /// 存量里写错的条件改对之前不能重新启用；停用不校验
    /// </summary>
    [Fact]
    public async Task Enable_RevalidatesStoredConditions()
    {
        var fixture = new Fixture();
        var id = (await fixture.Service.CreateAccessPolicyAsync(Create("block-scanners", BlockedNetwork))).PolicyId;
        _ = await fixture.Service.UpdateAccessPolicyStatusAsync(new AccessPolicyStatusCommand(id, EnableStatus.Disabled));
        fixture.Conditions[0].ConditionValue = "203.0.113.256/24";

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.UpdateAccessPolicyStatusAsync(new AccessPolicyStatusCommand(id, EnableStatus.Enabled)));

        Assert.Contains("environment.ip", error.Message, StringComparison.Ordinal);
        Assert.Equal(EnableStatus.Disabled, Assert.Single(fixture.Policies).Status);
    }

    /// <summary>
    /// 删除策略连同条件
    /// </summary>
    [Fact]
    public async Task Delete_RemovesConditions()
    {
        var fixture = new Fixture();
        var id = (await fixture.Service.CreateAccessPolicyAsync(Create("block-scanners", BlockedNetwork))).PolicyId;

        await fixture.Service.DeleteAccessPolicyAsync(id);

        Assert.Empty(fixture.Policies);
        Assert.Empty(fixture.Conditions);
    }

    /// <summary>
    /// 防锁死：启用中的拒绝策略要是会拦下当前请求就不让保存；只记录、停用或不命中的放过
    /// </summary>
    [Fact]
    public async Task SelfLockGuard_RejectsDenyPolicyThatBlocksCurrentRequest()
    {
        var fixture = new Fixture();
        var id = (await fixture.Service.CreateAccessPolicyAsync(Create("block-scanners", BlockedNetwork))).PolicyId;
        var trial = fixture.Trial("203.0.113.9");

        await trial.EnsureNotBlockingCurrentRequestAsync(id);

        _ = await fixture.Service.UpdateAccessPolicyActionAsync(new AccessPolicyActionCommand(id, AccessPolicyAction.Deny));
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => trial.EnsureNotBlockingCurrentRequestAsync(id));
        Assert.Contains("203.0.113.9", error.Message, StringComparison.Ordinal);

        await fixture.Trial("198.51.100.7").EnsureNotBlockingCurrentRequestAsync(id);
        _ = await fixture.Service.UpdateAccessPolicyStatusAsync(new AccessPolicyStatusCommand(id, EnableStatus.Disabled));
        await trial.EnsureNotBlockingCurrentRequestAsync(id);
    }

    /// <summary>
    /// 试运行条件草稿：用模拟的 IP 求值，给出逐子句结果与会不会拦下；条件写错时只报错误
    /// </summary>
    [Fact]
    public async Task Trial_EvaluatesDraftWithSimulatedContext()
    {
        var fixture = new Fixture();
        var trial = fixture.Trial("10.1.2.3");

        var blocked = await trial.TrialAsync(new AccessPolicyTrialInputDto
        {
            Action = AccessPolicyAction.Deny,
            Conditions = [new AccessPolicyConditionDto { AttributeName = SaasConditionAttributes.EnvironmentIp, Operator = ConditionOperator.In, ConditionValue = "203.0.113.0/24" }],
            Context = new AccessPolicyTrialContextDto { Ip = "203.0.113.9" }
        });
        var current = await trial.TrialAsync(new AccessPolicyTrialInputDto
        {
            Conditions = [new AccessPolicyConditionDto { AttributeName = SaasConditionAttributes.EnvironmentIp, Operator = ConditionOperator.In, ConditionValue = "203.0.113.0/24" }]
        });
        var broken = await trial.TrialAsync(new AccessPolicyTrialInputDto
        {
            Conditions = [new AccessPolicyConditionDto { AttributeName = "resource.status", Operator = ConditionOperator.Equals, ConditionValue = "draft" }]
        });

        Assert.Equal((ConditionOutcome.Matched, true, AccessPolicyPhase.BeforeAuthentication, "203.0.113.9"), (blocked.Outcome, blocked.WouldBlock, blocked.Phase, blocked.Ip));
        Assert.Equal(["203.0.113.9"], Assert.Single(Assert.Single(blocked.Groups).Clauses).AttributeValues);
        Assert.Equal((ConditionOutcome.NotMatched, false, "10.1.2.3"), (current.Outcome, current.WouldBlock, current.Ip));
        Assert.Null(broken.Outcome);
        Assert.Contains(broken.Errors, error => error.Contains("resource.status", StringComparison.Ordinal));
        await Assert.ThrowsAsync<InvalidOperationException>(() => trial.TrialAsync(new AccessPolicyTrialInputDto
        {
            Conditions = [new AccessPolicyConditionDto { AttributeName = SaasConditionAttributes.EnvironmentIp, Operator = ConditionOperator.In, ConditionValue = "203.0.113.0/24" }],
            Context = new AccessPolicyTrialContextDto { Ip = "not-an-ip" }
        }));
    }

    /// <summary>
    /// 登录失败自动封禁参数只认平台那一条，数值要在范围内
    /// </summary>
    [Fact]
    public async Task LoginAutoBanSetting_IsValidatedOnSave()
    {
        var tenantScope = new ConfigFixture(contextTenantId: 7);
        var tenantError = await Assert.ThrowsAsync<InvalidOperationException>(() => tenantScope.Service.CreateConfigAsync(ConfigFixture.Create("""{"isEnabled":true}""")));
        Assert.Contains("只在平台生效", tenantError.Message, StringComparison.Ordinal);

        var platform = new ConfigFixture(contextTenantId: null);
        var rangeError = await Assert.ThrowsAsync<InvalidOperationException>(() => platform.Service.CreateConfigAsync(ConfigFixture.Create("""{"isEnabled":true,"banMinutes":2000,"maxBanMinutes":1440}""")));
        Assert.Contains("首次封禁时长", rangeError.Message, StringComparison.Ordinal);
        _ = await Assert.ThrowsAsync<InvalidOperationException>(() => platform.Service.CreateConfigAsync(ConfigFixture.Create("""{"maxFailures":"many"}""")));

        _ = await platform.Service.CreateConfigAsync(ConfigFixture.Create("""{"isEnabled":true,"windowMinutes":5,"maxFailures":10,"banMinutes":15,"maxBanMinutes":60}"""));
        platform.Configs.Verify(repository => repository.AddAsync(It.IsAny<SysConfig>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    private static AccessPolicyCreateCommand Create(string code, params AccessPolicyConditionCommand[] conditions)
    {
        return new AccessPolicyCreateCommand(code, code, null, 100, EnableStatus.Enabled, null, null, null, null, conditions);
    }

    private sealed class Fixture
    {
        private long _nextId = 100;

        public Fixture()
        {
            var policies = new Mock<IAccessPolicyRepository>();
            policies
                .Setup(repository => repository.AnyAsync(It.IsAny<Expression<Func<SysAccessPolicy, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Expression<Func<SysAccessPolicy, bool>> predicate, CancellationToken _) => Policies.Any(predicate.Compile()));
            policies
                .Setup(repository => repository.AddAsync(It.IsAny<SysAccessPolicy>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((SysAccessPolicy row, CancellationToken _) =>
                {
                    SaasTestHelper.SetBasicId(row, ++_nextId);
                    Policies.Add(row);
                    return row;
                });
            policies
                .Setup(repository => repository.GetByIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((long id, CancellationToken _) => Policies.Find(row => row.BasicId == id));
            policies
                .Setup(repository => repository.UpdateAsync(It.IsAny<SysAccessPolicy>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((SysAccessPolicy row, CancellationToken _) => row);
            policies
                .Setup(repository => repository.DeleteAsync(It.IsAny<SysAccessPolicy>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((SysAccessPolicy row, CancellationToken _) => Policies.Remove(row));
            var conditions = new Mock<IAccessPolicyConditionRepository>();
            conditions
                .Setup(repository => repository.DeleteAsync(It.IsAny<Expression<Func<SysAccessPolicyCondition, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Expression<Func<SysAccessPolicyCondition, bool>> predicate, CancellationToken _) => Conditions.RemoveAll(new Predicate<SysAccessPolicyCondition>(predicate.Compile())) > 0);
            conditions
                .Setup(repository => repository.AddRangeAsync(It.IsAny<IEnumerable<SysAccessPolicyCondition>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((IEnumerable<SysAccessPolicyCondition> rows, CancellationToken _) =>
                {
                    var list = rows.ToList();
                    foreach (var row in list)
                    {
                        SaasTestHelper.SetBasicId(row, ++_nextId);
                    }

                    Conditions.AddRange(list);
                    return list;
                });
            conditions
                .Setup(repository => repository.GetByPolicyIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((long id, CancellationToken _) => Conditions.Where(row => row.PolicyId == id).ToList());
            PolicyRepository = policies.Object;
            ConditionRepository = conditions.Object;
            Service = new AccessPolicyDomainService(policies.Object, conditions.Object, Compiler());
        }

        public List<SysAccessPolicy> Policies { get; } = [];

        public List<SysAccessPolicyCondition> Conditions { get; } = [];

        public IAccessPolicyRepository PolicyRepository { get; }

        public IAccessPolicyConditionRepository ConditionRepository { get; }

        public AccessPolicyDomainService Service { get; }

        public AccessPolicyTrialService Trial(string currentIp)
        {
            var accessor = new HttpContextAccessor { HttpContext = Request(currentIp) };
            return new AccessPolicyTrialService(
                Compiler(),
                new DefaultConditionEvaluator(),
                new Sources(accessor),
                PolicyRepository,
                ConditionRepository,
                accessor,
                new NullIpRegions(),
                new ManualTimeProvider(Now),
                Options.Create(new SaasConditionOptions()));
        }
    }

    private sealed class ConfigFixture
    {
        public ConfigFixture(long? contextTenantId)
        {
            Configs
                .Setup(repository => repository.AnyAsync(It.IsAny<Expression<Func<SysConfig, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            Configs
                .Setup(repository => repository.AddAsync(It.IsAny<SysConfig>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((SysConfig row, CancellationToken _) => row);
            Service = new ConfigDomainService(Configs.Object, new Mock<IConfigValueSecretProtector>().Object, new TestCurrentTenant(contextTenantId));
        }

        public ConfigDomainService Service { get; }

        public Mock<IConfigRepository> Configs { get; } = new();

        public static ConfigCreateCommand Create(string value) =>
            new("登录失败自动封禁", SaasConfigKeys.Groups.Security, SaasConfigKeys.Security.LoginAutoBan, value, null, ConfigType.Feature, ConfigDataType.Json, null, false, EnableStatus.Enabled, 0, null);
    }
}
