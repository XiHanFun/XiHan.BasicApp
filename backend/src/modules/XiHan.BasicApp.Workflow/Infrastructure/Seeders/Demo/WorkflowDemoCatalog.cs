// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Application.Services;
using XiHan.Framework.Workflow.Abstractions;
using XiHan.Framework.Workflow.Abstractions.Assignees;
using XiHan.Framework.Workflow.Abstractions.Definitions;
using XiHan.Framework.Workflow.Builders;

namespace XiHan.BasicApp.Workflow.Infrastructure.Seeders;

/// <summary>
/// 演示流程定义：写进演示租户「示例企业」并发布，覆盖全部可发起演示的节点种类与受理人规则写法
/// </summary>
/// <remarks>
/// <para>费用报销：直属主管或签（发起人第 1 级主管、排除发起人，找不到主管时自动通过），条件分支按金额加审总经理（岗位）。</para>
/// <para>请假：逐级主管依次审批（2 级）、找不到审批人转租户管理员、抄送人事，天数超过 3 天再由人事部审批（部门）。</para>
/// <para>采购：并行两路——指定用户会签（全部通过）与总部负责人或签（部门负责人），汇聚后总经理审批，120 秒未办走 timeout 分支记日志后超时关闭。</para>
/// <para>入职：设置变量、记日志，人事准备（角色，抄送发起人），导师确认（表达式取变量里的用户），等待 60 秒，每件设备并行发起一个采购（循环），发起差旅报销（子流程）。</para>
/// 受理人规则引用的角色、部门、岗位都是演示数据里已有的编码；指定用户与导师按演示账号的用户名在播种时查出主键。
/// </remarks>
public static class WorkflowDemoCatalog
{
    /// <summary>
    /// 演示租户编码
    /// </summary>
    public const string TenantCode = "demo-enterprise";

    /// <summary>
    /// 流程分类
    /// </summary>
    public const string Category = "演示";

    /// <summary>
    /// 费用报销
    /// </summary>
    public const string Expense = "demo-expense";

    /// <summary>
    /// 请假
    /// </summary>
    public const string Leave = "demo-leave";

    /// <summary>
    /// 采购
    /// </summary>
    public const string Procurement = "demo-procurement";

    /// <summary>
    /// 入职
    /// </summary>
    public const string Onboarding = "demo-onboarding";

    /// <summary>
    /// 采购的总经理审批超时秒数：演示时两分钟内就能看到它走到 timeout 分支
    /// </summary>
    public const int ProcurementTimeoutSeconds = 120;

    /// <summary>
    /// 入职流程里等待入职当天的秒数（演示缩短）
    /// </summary>
    public const int OnboardingDelaySeconds = 60;

    /// <summary>
    /// 播种时要查出主键的演示账号：采购会签的两位、入职的导师
    /// </summary>
    public static IReadOnlyList<string> UserNames { get; } = ["admin", "manager", "lead"];

    /// <summary>
    /// 全部演示流程的编码，按创建先后：被调用的子流程（费用报销、采购）排在入职之前
    /// </summary>
    public static IReadOnlyList<string> Codes { get; } = [Expense, Leave, Procurement, Onboarding];

    /// <summary>
    /// 构造全部演示流程定义
    /// </summary>
    /// <param name="userIds">演示账号用户名到用户主键（字符串）</param>
    /// <returns>按 <see cref="Codes"/> 的顺序排列的流程定义</returns>
    /// <exception cref="KeyNotFoundException"><paramref name="userIds"/> 缺少 <see cref="UserNames"/> 里的账号</exception>
    public static IReadOnlyList<WorkflowDefinition> Build(IReadOnlyDictionary<string, string> userIds)
    {
        return [BuildExpense(), BuildLeave(), BuildProcurement(userIds["admin"], userIds["manager"]), BuildOnboarding(userIds["lead"])];
    }

    private static WorkflowDefinition BuildExpense()
    {
        return WorkflowDefinitionBuilder.Create(Expense, "费用报销")
            .WithCategory(Category)
            .WithDescription("演示：直属主管或签，金额超过 10000 元再由总经理审批")
            .AddVariable("amount", true, null, "报销金额（元），超过 10000 由总经理加审")
            .AddVariable("reason", false, "差旅", "报销事由")
            .AddStart()
            .AddUserTask("leader-approve", "直属主管审批", node => node
                .WithAssigneeRule(OrganizationAssigneeRuleTypes.StarterLeader, "1")
                .WithProperty("CompletionPolicy", "Any")
                .WithProperty("ExcludeStarter", true)
                .WithProperty("EmptyAssigneeStrategy", "AutoApprove")
                .WithProperty("Title", "费用报销 {{ amount }} 元：{{ reason }}"))
            .AddDecision("amount-check", "金额判断")
            .AddUserTask("gm-approve", "总经理审批", node => node
                .WithAssigneeRule(OrganizationAssigneeRuleTypes.Position, "gm")
                .WithProperty("CompletionPolicy", "Any")
                .WithProperty("Title", "大额报销 {{ amount }} 元：{{ reason }}"))
            .AddOutcomeEnd("end-approved", WorkflowUserTaskOutcomes.Approved, "通过")
            .AddOutcomeEnd("end-rejected", WorkflowUserTaskOutcomes.Rejected, "驳回")
            .AddTransition("start", "leader-approve")
            .AddTransition("leader-approve", "amount-check", Approved)
            .AddTransition("leader-approve", "end-rejected", Rejected)
            .AddTransition("amount-check", "gm-approve", "amount > 10000", 1, "超过 10000 元")
            .AddDefaultTransition("amount-check", "end-approved", "不超过 10000 元")
            .AddTransition("gm-approve", "end-approved", Approved)
            .AddTransition("gm-approve", "end-rejected", Rejected)
            .Build();
    }

    private static WorkflowDefinition BuildLeave()
    {
        return WorkflowDefinitionBuilder.Create(Leave, "请假")
            .WithCategory(Category)
            .WithDescription("演示：逐级主管依次审批，找不到审批人时转租户管理员，抄送人事；超过 3 天再由人事部审批")
            .AddVariable("days", true, null, "请假天数，超过 3 天由人事部加审")
            .AddVariable("reason", false, "事假", "请假事由")
            .AddStart()
            .AddUserTask("leader-chain", "逐级主管审批", node => node
                .WithAssigneeRule(OrganizationAssigneeRuleTypes.StarterLeaderChain, "2")
                .WithProperty("CompletionPolicy", "Sequential")
                .WithProperty("ExcludeStarter", true)
                .WithProperty("EmptyAssigneeStrategy", "Fallback")
                .WithProperty("FallbackAssigneeRules", Rules(OrganizationAssigneeRuleTypes.Role, "tenant_admin"))
                .WithProperty("CcRules", Rules(OrganizationAssigneeRuleTypes.Role, "hr_specialist"))
                .WithProperty("Title", "请假 {{ days }} 天：{{ reason }}"))
            .AddDecision("days-check", "天数判断")
            .AddUserTask("hr-approve", "人事部审批", node => node
                .WithAssigneeRule(OrganizationAssigneeRuleTypes.Department, "hr")
                .WithProperty("CompletionPolicy", "Any")
                .WithProperty("Title", "长假备案 {{ days }} 天：{{ reason }}"))
            .AddOutcomeEnd("end-approved", WorkflowUserTaskOutcomes.Approved, "通过")
            .AddOutcomeEnd("end-rejected", WorkflowUserTaskOutcomes.Rejected, "驳回")
            .AddTransition("start", "leader-chain")
            .AddTransition("leader-chain", "days-check", Approved)
            .AddTransition("leader-chain", "end-rejected", Rejected)
            .AddTransition("days-check", "hr-approve", "days > 3", 1, "超过 3 天")
            .AddDefaultTransition("days-check", "end-approved", "不超过 3 天")
            .AddTransition("hr-approve", "end-approved", Approved)
            .AddTransition("hr-approve", "end-rejected", Rejected)
            .Build();
    }

    private static WorkflowDefinition BuildProcurement(string adminId, string managerId)
    {
        return WorkflowDefinitionBuilder.Create(Procurement, "采购")
            .WithCategory(Category)
            .WithDescription($"演示：指定用户会签与总部负责人审核并行，汇聚后总经理审批；总经理 {ProcurementTimeoutSeconds} 秒未办走超时分支")
            .AddVariable("itemName", false, "办公设备", "采购物品")
            .AddVariable("amount", false, 5000, "采购金额（元）")
            .AddStart()
            .AddParallel("parallel-review", "并行审核")
            .AddUserTask("countersign", "财务会签", node => node
                .WithAssigneeRule(WorkflowAssigneeRuleTypes.User, adminId, managerId)
                .WithProperty("CompletionPolicy", "All")
                .WithProperty("Title", "采购会签：{{ itemName }}（{{ amount }} 元）"))
            .AddUserTask("hq-review", "总部负责人审核", node => node
                .WithAssigneeRule(OrganizationAssigneeRuleTypes.DepartmentLeader, "hq")
                .WithProperty("CompletionPolicy", "Any")
                .WithProperty("Title", "采购审核：{{ itemName }}（{{ amount }} 元）"))
            .AddJoin("join-review", false, "汇聚")
            .AddUserTask("gm-approve", "总经理审批", node => node
                .WithAssigneeRule(OrganizationAssigneeRuleTypes.Position, "gm")
                .WithProperty("CompletionPolicy", "Any")
                .WithProperty("Title", "采购终审：{{ itemName }}（{{ amount }} 元）")
                .WithTimeout(ProcurementTimeoutSeconds))
            .AddNode("log-timeout", WorkflowActivityTypes.Log, "记录超时", node => node
                .WithProperty("Message", "采购「{{ itemName }}」总经理超时未审批，自动关闭")
                .WithProperty("Level", "Warning"))
            .AddOutcomeEnd("end-approved", WorkflowUserTaskOutcomes.Approved, "通过")
            .AddOutcomeEnd("end-rejected", WorkflowUserTaskOutcomes.Rejected, "驳回")
            .AddOutcomeEnd("end-timeout", WorkflowUserTaskOutcomes.Timeout, "超时关闭")
            .AddTransition("start", "parallel-review")
            .AddTransition("parallel-review", "countersign")
            .AddTransition("parallel-review", "hq-review")
            .AddTransition("countersign", "join-review", Approved)
            .AddTransition("countersign", "end-rejected", Rejected)
            .AddTransition("hq-review", "join-review", Approved)
            .AddTransition("hq-review", "end-rejected", Rejected)
            .AddTransition("join-review", "gm-approve")
            .AddTransition("gm-approve", "end-approved", Approved)
            .AddTransition("gm-approve", "end-rejected", Rejected)
            .AddTransition("gm-approve", "log-timeout", $"outcome == '{WorkflowUserTaskOutcomes.Timeout}'")
            .AddTransition("log-timeout", "end-timeout")
            .Build();
    }

    private static WorkflowDefinition BuildOnboarding(string mentorId)
    {
        return WorkflowDefinitionBuilder.Create(Onboarding, "入职")
            .WithCategory(Category)
            .WithDescription($"演示：设置变量、记日志、人事准备（抄送发起人）、导师确认（表达式受理人）、等待 {OnboardingDelaySeconds} 秒、每件设备并行发起采购（循环）、发起差旅报销（子流程）")
            .AddVariable("employeeName", true, null, "新员工姓名")
            .AddVariable("travelAmount", false, 800, "入职差旅报销金额（元）")
            .AddStart()
            .AddNode("init", WorkflowActivityTypes.SetVariable, "准备入职清单", node => node
                .WithProperty("Values", new Dictionary<string, object?>
                {
                    ["equipment"] = new List<string> { "笔记本电脑", "显示器", "工牌" },
                    ["mentorId"] = mentorId
                })
                .WithProperty("Expressions", new Dictionary<string, string>
                {
                    ["budget"] = "travelAmount + 5000"
                }))
            .AddNode("log-start", WorkflowActivityTypes.Log, "记录开始", node => node
                .WithProperty("Message", "入职流程开始：{{ employeeName }}，预算 {{ budget }} 元"))
            .AddUserTask("hr-prepare", "人事准备", node => node
                .WithAssigneeRule(OrganizationAssigneeRuleTypes.Role, "hr_specialist")
                .WithProperty("CompletionPolicy", "Any")
                .WithProperty("CcRules", Rules(WorkflowAssigneeRuleTypes.Starter))
                .WithProperty("Title", "准备入职：{{ employeeName }}"))
            .AddUserTask("mentor-confirm", "导师确认", node => node
                .WithAssigneeRule(WorkflowAssigneeRuleTypes.Expression, "mentorId")
                .WithProperty("CompletionPolicy", "Any")
                .WithProperty("EmptyAssigneeStrategy", "AutoApprove")
                .WithProperty("Title", "带教确认：{{ employeeName }}"))
            .AddDelay("wait-first-day", OnboardingDelaySeconds, "等待入职当天")
            .AddNode("equipment-purchase", WorkflowActivityTypes.ForEach, "逐件采购设备", node => node
                .WithProperty("ItemsExpression", "equipment")
                .WithProperty("ItemVariableName", "itemName")
                .WithProperty("DefinitionCode", Procurement)
                .WithProperty("Parallel", true))
            .AddNode("travel-expense", WorkflowActivityTypes.SubWorkflow, "差旅报销", node => node
                .WithProperty("DefinitionCode", Expense)
                .WithProperty("VariableExpressions", new Dictionary<string, string>
                {
                    ["amount"] = "travelAmount",
                    ["reason"] = "'入职差旅'"
                }))
            .AddNode("log-done", WorkflowActivityTypes.Log, "记录完成", node => node
                .WithProperty("Message", "入职流程完成：{{ employeeName }}"))
            .AddOutcomeEnd("end-approved", WorkflowUserTaskOutcomes.Approved, "入职完成")
            .AddOutcomeEnd("end-rejected", WorkflowUserTaskOutcomes.Rejected, "取消入职")
            .AddTransition("start", "init")
            .AddTransition("init", "log-start")
            .AddTransition("log-start", "hr-prepare")
            .AddTransition("hr-prepare", "mentor-confirm", Approved)
            .AddTransition("hr-prepare", "end-rejected", Rejected)
            .AddTransition("mentor-confirm", "wait-first-day", Approved)
            .AddTransition("mentor-confirm", "end-rejected", Rejected)
            .AddTransition("wait-first-day", "equipment-purchase")
            .AddTransition("equipment-purchase", "travel-expense")
            .AddTransition("travel-expense", "log-done")
            .AddTransition("log-done", "end-approved")
            .Build();
    }

    private const string Approved = "outcome == 'approved'";

    private const string Rejected = "outcome == 'rejected'";

    private static List<WorkflowAssigneeRule> Rules(string type, params string[] values)
    {
        return [new WorkflowAssigneeRule { Type = type, Values = [.. values] }];
    }
}
