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
}
