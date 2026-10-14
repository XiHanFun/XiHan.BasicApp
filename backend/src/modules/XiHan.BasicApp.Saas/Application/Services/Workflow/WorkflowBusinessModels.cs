// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.Framework.Workflow.Abstractions;
using XiHan.Framework.Workflow.Abstractions.Definitions;
using XiHan.Framework.Workflow.Abstractions.Runtime;

namespace XiHan.BasicApp.Saas.Application.Services;

/// <summary>
/// 审批业务描述
/// </summary>
public sealed class WorkflowBusinessDescriptor
{
    /// <summary>
    /// 业务编码（同时是流程定义编码；小写短横线，如 permission-request）
    /// </summary>
    public required string Code { get; init; }

    /// <summary>
    /// 业务名称
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// 业务说明
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// 单据视图组件（相对前端 src/views 的组件路径，与菜单 Component 同口径，如 identity/authorization/permission-request-approval）
    /// </summary>
    /// <remarks>
    /// 审批抽屉按此路径加载组件，传入 businessId、data（<see cref="IWorkflowBusinessHandler.GetFormDataAsync"/> 的结果）、
    /// mode（approve 办理中 / view 只读）与 nodeId（当前待办节点）；组件暴露 getVariables() 时，办理时其返回值作为附加流程变量提交。
    /// </remarks>
    public required string FormComponent { get; init; }

    /// <summary>
    /// 发起时写入的流程变量（设计器据此提示分支条件与表达式可用的变量）
    /// </summary>
    public IReadOnlyList<WorkflowBusinessVariable> Variables { get; init; } = [];

    /// <summary>
    /// 允许的流程结果（结果结束节点只能声明其中之一）
    /// </summary>
    public IReadOnlyList<string> Outcomes { get; init; } = [WorkflowUserTaskOutcomes.Approved, WorkflowUserTaskOutcomes.Rejected];

    /// <summary>
    /// 默认流程模板（可空）
    /// </summary>
    /// <remarks>
    /// 某租户首次发起该业务、且租户里还没有任何同编码的流程定义时，按模板建一个并发布，之后与手工设计的流程一样可改可发新版本；
    /// 租户里已有同编码定义（哪怕只是草稿或已停用）就不再自动建，未发布则发起失败，尊重管理员的选择。
    /// 模板的编码必须等于 <see cref="Code"/>。
    /// </remarks>
    public Func<WorkflowDefinition>? DefaultDefinition { get; init; }
}

/// <summary>
/// 审批业务流程变量声明
/// </summary>
/// <param name="Name">变量名（流程表达式中引用）</param>
/// <param name="Label">显示名</param>
/// <param name="ValueType">值类型（string / number / boolean / user，user 为用户主键字符串）</param>
/// <param name="Description">说明</param>
public sealed record WorkflowBusinessVariable(string Name, string Label, string ValueType, string? Description = null);

/// <summary>
/// 发起审批流请求
/// </summary>
public sealed class WorkflowBusinessStartRequest
{
    /// <summary>
    /// 业务编码
    /// </summary>
    public required string BusinessCode { get; init; }

    /// <summary>
    /// 业务单据标识（写入实例的业务相关性标识）
    /// </summary>
    public required string BusinessId { get; init; }

    /// <summary>
    /// 实例标题（显示在待办与审批中心，如「权限申请：角色授予」）
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// 流程变量（分支条件与表达式规则可引用）
    /// </summary>
    public Dictionary<string, object?> Variables { get; init; } = [];
}

/// <summary>
/// 发起审批流结果
/// </summary>
/// <param name="InstanceId">流程实例主键</param>
/// <param name="Status">发起后的实例状态（同步链路能走多远走多远，可能已直接完成）</param>
/// <param name="Outcome">流程结果（发起即完成时有值）</param>
public sealed record WorkflowBusinessStartResult(long InstanceId, WorkflowInstanceStatus Status, string? Outcome);

/// <summary>
/// 审批进度
/// </summary>
public sealed class WorkflowBusinessProgress
{
    /// <summary>
    /// 流程实例主键
    /// </summary>
    public long InstanceId { get; init; }

    /// <summary>
    /// 实例状态
    /// </summary>
    public WorkflowInstanceStatus Status { get; init; }

    /// <summary>
    /// 流程结果（经结果结束节点完成时有值）
    /// </summary>
    public string? Outcome { get; init; }

    /// <summary>
    /// 当前待办节点名称（无待办为空）
    /// </summary>
    public IReadOnlyList<string> PendingNodeNames { get; init; } = [];

    /// <summary>
    /// 当前待办审批人
    /// </summary>
    public IReadOnlyList<WorkflowBusinessUser> PendingAssignees { get; init; } = [];

    /// <summary>
    /// 故障信息（实例故障时有值）
    /// </summary>
    public string? FaultMessage { get; init; }

    /// <summary>
    /// 开始时间
    /// </summary>
    public DateTime? StartTime { get; init; }

    /// <summary>
    /// 结束时间
    /// </summary>
    public DateTime? EndTime { get; init; }
}

/// <summary>
/// 审批相关用户（主键 + 显示名）
/// </summary>
/// <param name="UserId">用户主键</param>
/// <param name="DisplayName">显示名（真实姓名 / 昵称 / 用户名）</param>
public sealed record WorkflowBusinessUser(long UserId, string DisplayName);

/// <summary>
/// 办理前上下文
/// </summary>
public sealed class WorkflowBusinessTaskContext
{
    /// <summary>
    /// 业务编码
    /// </summary>
    public required string BusinessCode { get; init; }

    /// <summary>
    /// 业务单据标识
    /// </summary>
    public required string BusinessId { get; init; }

    /// <summary>
    /// 流程实例主键
    /// </summary>
    public required long InstanceId { get; init; }

    /// <summary>
    /// 任务标识
    /// </summary>
    public required string TaskId { get; init; }

    /// <summary>
    /// 节点标识
    /// </summary>
    public required string NodeId { get; init; }

    /// <summary>
    /// 办理人主键（当前登录用户）
    /// </summary>
    public required long ActorUserId { get; init; }

    /// <summary>
    /// 办理结果（approved / rejected 或自定义）
    /// </summary>
    public required string Outcome { get; init; }

    /// <summary>
    /// 办理意见
    /// </summary>
    public string? Comment { get; init; }
}

/// <summary>
/// 流程结论上下文
/// </summary>
public sealed class WorkflowBusinessOutcomeContext
{
    /// <summary>
    /// 业务编码
    /// </summary>
    public required string BusinessCode { get; init; }

    /// <summary>
    /// 业务单据标识
    /// </summary>
    public required string BusinessId { get; init; }

    /// <summary>
    /// 流程实例主键
    /// </summary>
    public required long InstanceId { get; init; }

    /// <summary>
    /// 流程结果（结果结束节点声明的 Outcome）
    /// </summary>
    public required string Outcome { get; init; }

    /// <summary>
    /// 发起人主键（实例未记录发起人时为空）
    /// </summary>
    public long? StarterUserId { get; init; }

    /// <summary>
    /// 实例变量（只读）
    /// </summary>
    public required IReadOnlyDictionary<string, object?> Variables { get; init; }
}

/// <summary>
/// 流程中止上下文
/// </summary>
public sealed class WorkflowBusinessAbortedContext
{
    /// <summary>
    /// 业务编码
    /// </summary>
    public required string BusinessCode { get; init; }

    /// <summary>
    /// 业务单据标识
    /// </summary>
    public required string BusinessId { get; init; }

    /// <summary>
    /// 流程实例主键
    /// </summary>
    public required long InstanceId { get; init; }

    /// <summary>
    /// 中止后的实例状态（Canceled 撤回或取消 / Terminated 终止）
    /// </summary>
    public required WorkflowInstanceStatus Status { get; init; }

    /// <summary>
    /// 操作人主键
    /// </summary>
    public long? OperatorUserId { get; init; }

    /// <summary>
    /// 中止原因
    /// </summary>
    public string? Reason { get; init; }
}
