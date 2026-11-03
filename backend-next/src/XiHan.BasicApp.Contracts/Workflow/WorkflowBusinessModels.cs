// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text.Json.Nodes;

namespace XiHan.BasicApp.Contracts.Workflow;

/// <summary>
/// 审批业务的描述
/// </summary>
/// <param name="Code">业务码，也是流程定义名（小写字母、数字、点、下划线与连字符）</param>
/// <param name="NameKey">显示名的文案键（前端按它翻译）</param>
/// <param name="FormComponent">审批详情里加载的单据组件路径（前端视图的组件路径，如 <c>access/permission-request-approval/index</c>）</param>
/// <param name="Variables">发起时带的变量（设计器提示用）</param>
/// <param name="Outcomes">允许的结论（带结论的结束节点只能用这些），缺省 approved / rejected</param>
/// <param name="DefaultDefinition">缺省定义（WorkflowJson 文本）：分区里还没有这个名字的定义时，首次发起按它建并发布；为空则必须先手工建</param>
public sealed record WorkflowBusinessDescriptor(
    string Code,
    string NameKey,
    string FormComponent,
    IReadOnlyList<WorkflowBusinessVariable> Variables,
    IReadOnlyList<string> Outcomes,
    string? DefaultDefinition);

/// <summary>
/// 发起时带的一个变量
/// </summary>
/// <param name="Name">变量名</param>
/// <param name="LabelKey">显示名的文案键</param>
/// <param name="ValueType">值类型：<c>string</c>、<c>number</c>、<c>boolean</c> 或 <c>user</c></param>
public sealed record WorkflowBusinessVariable(string Name, string LabelKey, string ValueType);

/// <summary>
/// 办理前校验的任务
/// </summary>
/// <param name="BusinessCode">业务码</param>
/// <param name="BusinessId">单据 ID</param>
/// <param name="InstanceId">实例 ID</param>
/// <param name="TaskId">任务 ID</param>
/// <param name="NodeId">节点 ID</param>
/// <param name="Outcome">办理结论</param>
/// <param name="ActorId">办理人</param>
public sealed record WorkflowBusinessTask(string BusinessCode, string BusinessId, Guid InstanceId, Guid TaskId, string NodeId, string Outcome, Guid ActorId);

/// <summary>
/// 审批结论
/// </summary>
/// <param name="BusinessCode">业务码</param>
/// <param name="BusinessId">单据 ID</param>
/// <param name="InstanceId">实例 ID</param>
/// <param name="Outcome">结论</param>
public sealed record WorkflowBusinessOutcome(string BusinessCode, string BusinessId, Guid InstanceId, string Outcome);

/// <summary>
/// 办理之外的改动
/// </summary>
/// <param name="BusinessCode">业务码</param>
/// <param name="BusinessId">单据 ID</param>
/// <param name="InstanceId">实例 ID</param>
/// <param name="Kind">改动的种类</param>
public sealed record WorkflowBusinessChange(string BusinessCode, string BusinessId, Guid InstanceId, WorkflowBusinessChangeKind Kind);

/// <summary>
/// 中止的审批
/// </summary>
/// <param name="BusinessCode">业务码</param>
/// <param name="BusinessId">单据 ID</param>
/// <param name="InstanceId">实例 ID</param>
/// <param name="Status">中止后的状态（取消或终止）</param>
/// <param name="Reason">原因</param>
public sealed record WorkflowBusinessAborted(string BusinessCode, string BusinessId, Guid InstanceId, WorkflowBusinessStatus Status, string? Reason);

/// <summary>
/// 发起审批的请求
/// </summary>
/// <param name="BusinessCode">业务码</param>
/// <param name="BusinessId">单据 ID（写进实例的关联 ID）</param>
/// <param name="Title">实例标题</param>
/// <param name="Variables">变量</param>
public sealed record WorkflowBusinessStartRequest(string BusinessCode, string BusinessId, string Title, JsonObject? Variables);

/// <summary>
/// 发起结果
/// </summary>
/// <param name="InstanceId">实例 ID</param>
/// <param name="Status">状态</param>
/// <param name="Outcome">已经走完时的结论</param>
public sealed record WorkflowBusinessStartResult(Guid InstanceId, WorkflowBusinessStatus Status, string? Outcome);

/// <summary>
/// 一张单据的审批进度
/// </summary>
/// <param name="InstanceId">实例 ID</param>
/// <param name="Status">状态</param>
/// <param name="Outcome">结论</param>
/// <param name="PendingNodeNames">进行中的节点名</param>
/// <param name="PendingAssignees">当前待办人</param>
/// <param name="FaultMessage">故障信息</param>
/// <param name="StartedAt">发起时间</param>
/// <param name="EndedAt">结束时间</param>
public sealed record WorkflowBusinessProgress(
    Guid InstanceId,
    WorkflowBusinessStatus Status,
    string? Outcome,
    IReadOnlyList<string> PendingNodeNames,
    IReadOnlyList<WorkflowBusinessAssignee> PendingAssignees,
    string? FaultMessage,
    DateTimeOffset StartedAt,
    DateTimeOffset? EndedAt);

/// <summary>
/// 待办人
/// </summary>
/// <param name="UserId">用户 ID</param>
/// <param name="DisplayName">显示名</param>
public sealed record WorkflowBusinessAssignee(Guid UserId, string DisplayName);

/// <summary>
/// 办理之外的改动的种类
/// </summary>
public enum WorkflowBusinessChangeKind
{
    /// <summary>
    /// 转办
    /// </summary>
    Transfer = 1,

    /// <summary>
    /// 加签
    /// </summary>
    AddAssignees = 2,

    /// <summary>
    /// 发起人在审批中心撤回
    /// </summary>
    Withdraw = 3,

    /// <summary>
    /// 管理员取消
    /// </summary>
    Cancel = 4,

    /// <summary>
    /// 管理员终止
    /// </summary>
    Terminate = 5,

    /// <summary>
    /// 管理员重试
    /// </summary>
    Retry = 6,

    /// <summary>
    /// 管理员挂起
    /// </summary>
    Suspend = 7,

    /// <summary>
    /// 管理员恢复
    /// </summary>
    Resume = 8,
}

/// <summary>
/// 审批实例的状态
/// </summary>
public enum WorkflowBusinessStatus
{
    /// <summary>
    /// 进行中
    /// </summary>
    Running = 1,

    /// <summary>
    /// 已挂起
    /// </summary>
    Suspended = 2,

    /// <summary>
    /// 已结束
    /// </summary>
    Completed = 3,

    /// <summary>
    /// 已取消（撤回）
    /// </summary>
    Canceled = 4,

    /// <summary>
    /// 故障
    /// </summary>
    Faulted = 5,

    /// <summary>
    /// 已终止
    /// </summary>
    Terminated = 6,
}
