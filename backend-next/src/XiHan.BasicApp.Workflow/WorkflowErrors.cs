// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Workflow;

/// <summary>
/// 工作流包的错误码。
/// </summary>
/// <remarks>
/// 与框架工作流的错误码同一个前缀 <c>WORKFLOW_</c>：框架占 001 段，BasicApp 自己的各类从 101 起编（设计稿 D5），前端按码就分得清是谁报的。
/// 引擎、定义目录与任务服务的失败（<c>WORKFLOW_VALIDATION_001</c> 定义不合法等）原样返回，不另起码。
/// </remarks>
public static class WorkflowErrors
{
    /// <summary>
    /// 流程定义不存在或在别的分区。
    /// </summary>
    public const string DefinitionNotFound = "WORKFLOW_NOTFOUND_101";

    /// <summary>
    /// 流程实例不存在或在别的分区。
    /// </summary>
    public const string InstanceNotFound = "WORKFLOW_NOTFOUND_102";

    /// <summary>
    /// 审批业务的结束节点结论不合法。
    /// </summary>
    /// <remarks>
    /// 关联了审批业务的定义有带结论的结束节点，结论不在业务允许的范围里，或一个带结论的结束节点都没有。
    /// </remarks>
    public const string BusinessOutcomeInvalid = "WORKFLOW_VALIDATION_101";

    /// <summary>
    /// 审批业务的定义里有终止节点。
    /// </summary>
    public const string BusinessTerminateForbidden = "WORKFLOW_VALIDATION_102";

    /// <summary>
    /// 变量或信号载荷不是 JSON 对象。
    /// </summary>
    public const string JsonObjectRequired = "WORKFLOW_VALIDATION_103";

    /// <summary>
    /// 列表的筛选或排序字段不受支持。
    /// </summary>
    public const string QueryUnsupported = "WORKFLOW_VALIDATION_104";

    /// <summary>
    /// 组织类受理人规则的参数不合法。
    /// </summary>
    /// <remarks>
    /// 编码类规则（角色、部门、部门负责人、岗位）至少给一个编码；主管类规则（发起人主管、主管链）至多给一个 1～10 的层级。发布定义时由框架的结构校验收进问题列表。
    /// </remarks>
    public const string AssigneeRuleInvalid = "WORKFLOW_VALIDATION_105";

    /// <summary>
    /// 待办不存在、已办理或不归调用方。
    /// </summary>
    public const string TaskNotFound = "WORKFLOW_NOTFOUND_103";

    /// <summary>
    /// 转办或加签的目标不能作为受理人。
    /// </summary>
    /// <remarks>
    /// 目标不是当前一侧可见的在用账号（不存在、已停用、已删除或是运维支持成员）。
    /// </remarks>
    public const string AssigneeUnavailable = "WORKFLOW_VALIDATION_106";

    /// <summary>
    /// 审批业务没有登记。
    /// </summary>
    public const string BusinessNotRegistered = "WORKFLOW_VALIDATION_107";

    /// <summary>
    /// 子流程不能单独撤回。
    /// </summary>
    public const string WithdrawSubWorkflow = "WORKFLOW_VALIDATION_108";

    /// <summary>
    /// 审批业务的实例缺少单据 ID。
    /// </summary>
    /// <remarks>
    /// 名字等于业务码的顶层实例没有关联 ID，走到带结论的结束节点时回写不了单据，结束节点故障。
    /// </remarks>
    public const string BusinessIdMissing = "WORKFLOW_VALIDATION_109";

    /// <summary>
    /// 一次查询的单据太多。
    /// </summary>
    public const string ProgressTooMany = "WORKFLOW_VALIDATION_110";

    /// <summary>
    /// 审批发起即故障。
    /// </summary>
    /// <remarks>
    /// 首个节点就故障（如审批节点找不到人），整单失败、随业务回滚，不留半截审批。
    /// </remarks>
    public const string StartFaulted = "WORKFLOW_CONFLICT_101";

    /// <summary>
    /// 流程已结束，不能撤回。
    /// </summary>
    public const string WithdrawFinished = "WORKFLOW_CONFLICT_102";

    /// <summary>
    /// 没有查看这条审批的权限。
    /// </summary>
    /// <remarks>
    /// 详情按参与关系鉴权：发起人、当前受理人、办理过（办理、转办、加签）或被抄送过的人，或持有 <c>workflow.workflow-instances.read</c>。
    /// </remarks>
    public const string ApprovalHidden = "WORKFLOW_DENIED_101";

    /// <summary>
    /// 只能撤回自己发起的流程。
    /// </summary>
    public const string WithdrawNotStarter = "WORKFLOW_DENIED_102";
}
