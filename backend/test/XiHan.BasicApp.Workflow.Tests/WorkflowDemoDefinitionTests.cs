// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using XiHan.BasicApp.Saas.Application.Services;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.BasicApp.Saas.Infrastructure.Seeders;
using XiHan.BasicApp.Workflow.Extensions;
using XiHan.BasicApp.Workflow.Infrastructure.Seeders;
using XiHan.Framework.Workflow.Abstractions;
using XiHan.Framework.Workflow.Abstractions.Assignees;
using XiHan.Framework.Workflow.Abstractions.Definitions;
using XiHan.Framework.Workflow.Definitions;
using XiHan.Framework.Timing;
using XiHan.Framework.Workflow.Extensions.DependencyInjection;

namespace XiHan.BasicApp.Workflow.Tests;

/// <summary>
/// 演示流程：四个定义都过得了发布时的结构校验，覆盖全部受理人规则与可演示的节点种类，引用的组织编码都在演示数据里。
/// </summary>
public sealed class WorkflowDemoDefinitionTests
{
    private static readonly Dictionary<string, string> UserIds = new(StringComparer.Ordinal)
    {
        ["admin"] = "1001",
        ["manager"] = "1002",
        ["lead"] = "1003"
    };

    private static readonly IReadOnlyList<WorkflowDefinition> Definitions = WorkflowDemoCatalog.Build(UserIds);

    /// <summary>
    /// 每个演示流程都通过引擎发布前的结构校验（节点、连线、表达式、受理人规则类型与参数）。
    /// </summary>
    [Fact]
    public void Definitions_ShouldPassPublishValidation()
    {
        using var provider = BuildWorkflowServices();
        var validator = provider.GetRequiredService<WorkflowDefinitionValidator>();

        Assert.All(Definitions, definition =>
        {
            var errors = validator.Validate(definition);
            Assert.True(errors.Count == 0, $"{definition.Code}：{string.Join("；", errors)}");
        });
    }

    /// <summary>
    /// 四个流程按编码排好：编码不重复、不占审批业务的保留编码，循环与子流程调用的流程排在调用方之前。
    /// </summary>
    [Fact]
    public void Codes_ShouldBeUniqueUnreservedAndOrderedChildrenFirst()
    {
        Assert.Equal(WorkflowDemoCatalog.Codes, Definitions.Select(definition => definition.Code));
        Assert.Equal(Definitions.Count, Definitions.Select(definition => definition.Code).Distinct(StringComparer.Ordinal).Count());
        Assert.DoesNotContain(SaasWorkflowBusinessCodes.PermissionRequest, WorkflowDemoCatalog.Codes);
        Assert.All(Definitions, definition => Assert.Equal(WorkflowDemoCatalog.Category, definition.Category));

        var position = WorkflowDemoCatalog.Codes.Select((code, index) => (code, index)).ToDictionary(item => item.code, item => item.index, StringComparer.Ordinal);
        foreach (var definition in Definitions)
        {
            foreach (var node in definition.Nodes.Where(node => node.ActivityType is WorkflowActivityTypes.SubWorkflow or WorkflowActivityTypes.ForEach))
            {
                var child = Assert.IsType<string>(node.Properties["DefinitionCode"]);
                Assert.True(position[child] < position[definition.Code], $"{definition.Code} 的 {node.Id} 调用的 {child} 要先创建");
            }
        }
    }

    /// <summary>
    /// 覆盖可演示的全部节点种类；需要外部服务或只用来报错的节点不演示。
    /// </summary>
    [Fact]
    public void Nodes_ShouldCoverEveryDemonstrableActivityType()
    {
        var used = Definitions.SelectMany(definition => definition.Nodes).Select(node => node.ActivityType).ToHashSet(StringComparer.Ordinal);

        string[] expected =
        [
            WorkflowActivityTypes.Start, WorkflowActivityTypes.End, WorkflowActivityTypes.Decision,
            WorkflowActivityTypes.Parallel, WorkflowActivityTypes.Join, WorkflowActivityTypes.Delay,
            WorkflowActivityTypes.UserTask, WorkflowActivityTypes.SetVariable, WorkflowActivityTypes.Log,
            WorkflowActivityTypes.ForEach, WorkflowActivityTypes.SubWorkflow
        ];
        Assert.Equal(expected.Order(StringComparer.Ordinal), used.Order(StringComparer.Ordinal));
    }

    /// <summary>
    /// 覆盖全部受理人规则类型（审批人、找不到审批人时的兜底与抄送里出现的都算）。
    /// </summary>
    [Fact]
    public void AssigneeRules_ShouldCoverEveryRuleType()
    {
        var used = AllRules().Select(rule => rule.Type).ToHashSet(StringComparer.Ordinal);

        string[] expected =
        [
            WorkflowAssigneeRuleTypes.User, WorkflowAssigneeRuleTypes.Starter, WorkflowAssigneeRuleTypes.Expression,
            OrganizationAssigneeRuleTypes.Role, OrganizationAssigneeRuleTypes.Department, OrganizationAssigneeRuleTypes.DepartmentLeader,
            OrganizationAssigneeRuleTypes.Position, OrganizationAssigneeRuleTypes.StarterLeader, OrganizationAssigneeRuleTypes.StarterLeaderChain
        ];
        Assert.Equal(expected.Order(StringComparer.Ordinal), used.Order(StringComparer.Ordinal));
    }

    /// <summary>
    /// 规则里的角色、部门、岗位编码都是示例企业演示数据里已有的启用项；指定用户与导师取播种时查出的演示账号主键。
    /// </summary>
    [Fact]
    public void AssigneeRules_ShouldReferenceExistingDemoCodesAndAccounts()
    {
        var tenant = SaasDemoScenario.Tenants.Single(tenant => tenant.Code == WorkflowDemoCatalog.TenantCode);
        var roles = tenant.Roles!.Where(role => role.Status == Saas.Domain.Enums.EnableStatus.Enabled).Select(role => role.Code).ToHashSet(StringComparer.Ordinal);
        var departments = tenant.Departments!.Where(department => department.Status == Saas.Domain.Enums.EnableStatus.Enabled).Select(department => department.Code).ToHashSet(StringComparer.Ordinal);
        var positions = tenant.Positions!.Where(position => position.Status == Saas.Domain.Enums.EnableStatus.Enabled).Select(position => position.Code).ToHashSet(StringComparer.Ordinal);
        var accounts = tenant.Accounts!.Select(account => account.UserName).ToHashSet(StringComparer.Ordinal);

        Assert.All(WorkflowDemoCatalog.UserNames, name => Assert.Contains(name, accounts));
        foreach (var rule in AllRules())
        {
            var known = rule.Type switch
            {
                OrganizationAssigneeRuleTypes.Role => roles,
                OrganizationAssigneeRuleTypes.Department or OrganizationAssigneeRuleTypes.DepartmentLeader => departments,
                OrganizationAssigneeRuleTypes.Position => positions,
                WorkflowAssigneeRuleTypes.User => UserIds.Values.ToHashSet(StringComparer.Ordinal),
                _ => null
            };
            if (known is not null)
            {
                Assert.All(rule.Values, value => Assert.True(known.Contains(value), $"{rule.Type} 规则引用的 {value} 不在演示数据里"));
            }
        }

        var init = Definitions.Single(definition => definition.Code == WorkflowDemoCatalog.Onboarding).Nodes.Single(node => node.Id == "init");
        var values = Assert.IsType<Dictionary<string, object?>>(init.Properties["Values"]);
        Assert.Equal(UserIds["lead"], values["mentorId"]);
    }

    /// <summary>
    /// 采购的总经理审批按演示时长超时，超时结果有自己的出口连线并以超时结局结束。
    /// </summary>
    [Fact]
    public void Procurement_TimeoutShouldBeShortAndRouted()
    {
        var procurement = Definitions.Single(definition => definition.Code == WorkflowDemoCatalog.Procurement);
        var approval = procurement.Nodes.Single(node => node.Id == "gm-approve");

        Assert.Equal(WorkflowDemoCatalog.ProcurementTimeoutSeconds, approval.TimeoutSeconds);
        Assert.InRange(WorkflowDemoCatalog.ProcurementTimeoutSeconds, 60, 600);
        Assert.Contains(procurement.Transitions, transition =>
            transition.SourceNodeId == approval.Id
            && transition.Condition == $"outcome == '{WorkflowUserTaskOutcomes.Timeout}'");
        Assert.Contains(procurement.Nodes, node =>
            node.ActivityType == WorkflowActivityTypes.End
            && Equals(node.Properties.GetValueOrDefault("Outcome"), WorkflowUserTaskOutcomes.Timeout));
    }

    private static IEnumerable<WorkflowAssigneeRule> AllRules()
    {
        string[] ruleProperties = ["AssigneeRules", "FallbackAssigneeRules", "CcRules"];
        return Definitions
            .SelectMany(definition => definition.Nodes)
            .SelectMany(node => ruleProperties.Select(name => node.Properties.GetValueOrDefault(name)))
            .OfType<List<WorkflowAssigneeRule>>()
            .SelectMany(rules => rules);
    }

    private static ServiceProvider BuildWorkflowServices()
    {
        var services = new ServiceCollection()
            .AddLogging()
            .AddSingleton(Mock.Of<IClock>())
            .AddSingleton(Mock.Of<IUserDirectory>())
            .AddSingleton(Mock.Of<IRoleRepository>())
            .AddSingleton(Mock.Of<IUserRoleRepository>())
            .AddSingleton(Mock.Of<IDepartmentRepository>())
            .AddSingleton(Mock.Of<IUserDepartmentRepository>())
            .AddSingleton(Mock.Of<IPositionRepository>());
        services.AddXiHanWorkflow(new ConfigurationBuilder().Build());
        services.AddWorkflowAssigneeProviders();
        return services.BuildServiceProvider();
    }
}
