// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.Framework.Localization.Abstractions.Messages;

namespace XiHan.BasicApp.Workflow;

/// <summary>
/// 工作流包的消息键与目录。
/// </summary>
/// <remarks>
/// 主题避开框架 <c>workflow</c> 目录用过的单数主题（<c>workflow.definition</c>、<c>workflow.instance</c> 等），两个目录不撞键。
/// </remarks>
public static class WorkflowMessages
{
    /// <summary>
    /// 目录名。
    /// </summary>
    public const string CatalogName = "basicapp.workflow";

    /// <summary>
    /// 定义列表的说明。
    /// </summary>
    public const string ListDefinitionsDescription = "workflow_definitions.list.description";

    /// <summary>
    /// 定义详情的说明。
    /// </summary>
    public const string GetDefinitionDescription = "workflow_definitions.get.description";

    /// <summary>
    /// 新建定义的说明。
    /// </summary>
    public const string CreateDefinitionDescription = "workflow_definitions.create.description";

    /// <summary>
    /// 改草稿的说明。
    /// </summary>
    public const string UpdateDefinitionDescription = "workflow_definitions.update.description";

    /// <summary>
    /// 发布定义的说明。
    /// </summary>
    public const string PublishDefinitionDescription = "workflow_definitions.publish.description";

    /// <summary>
    /// 新版本的说明。
    /// </summary>
    public const string NewDefinitionVersionDescription = "workflow_definitions.new_version.description";

    /// <summary>
    /// 停用定义的说明。
    /// </summary>
    public const string DisableDefinitionsDescription = "workflow_definitions.disable.description";

    /// <summary>
    /// 归档定义的说明。
    /// </summary>
    public const string ArchiveDefinitionsDescription = "workflow_definitions.archive.description";

    /// <summary>
    /// 删除草稿的说明。
    /// </summary>
    public const string DeleteDefinitionsDescription = "workflow_definitions.delete.description";

    /// <summary>
    /// 审批业务目录的说明。
    /// </summary>
    public const string BusinessTypesDescription = "workflow_definitions.business_types.description";

    /// <summary>
    /// 活动类型目录的说明。
    /// </summary>
    public const string ActivityTypesDescription = "workflow_definitions.activity_types.description";

    /// <summary>
    /// 实例列表的说明。
    /// </summary>
    public const string ListInstancesDescription = "workflow_instances.list.description";

    /// <summary>
    /// 实例详情的说明。
    /// </summary>
    public const string GetInstanceDescription = "workflow_instances.get.description";

    /// <summary>
    /// 发起流程的说明。
    /// </summary>
    public const string StartInstanceDescription = "workflow_instances.start.description";

    /// <summary>
    /// 取消实例的说明。
    /// </summary>
    public const string CancelInstancesDescription = "workflow_instances.cancel.description";

    /// <summary>
    /// 终止实例的说明。
    /// </summary>
    public const string TerminateInstancesDescription = "workflow_instances.terminate.description";

    /// <summary>
    /// 重试实例的说明。
    /// </summary>
    public const string RetryInstancesDescription = "workflow_instances.retry.description";

    /// <summary>
    /// 挂起实例的说明。
    /// </summary>
    public const string SuspendInstancesDescription = "workflow_instances.suspend.description";

    /// <summary>
    /// 恢复实例的说明。
    /// </summary>
    public const string ResumeInstancesDescription = "workflow_instances.resume.description";

    /// <summary>
    /// 发信号的说明。
    /// </summary>
    public const string SignalInstancesDescription = "workflow_instances.signal.description";

    /// <summary>
    /// 选用户的说明。
    /// </summary>
    public const string AssigneeUsersDescription = "workflow_assignees.users.description";

    /// <summary>
    /// 选角色的说明。
    /// </summary>
    public const string AssigneeRolesDescription = "workflow_assignees.roles.description";

    /// <summary>
    /// 选岗位的说明。
    /// </summary>
    public const string AssigneePositionsDescription = "workflow_assignees.positions.description";

    /// <summary>
    /// 选部门的说明。
    /// </summary>
    public const string AssigneeDepartmentsDescription = "workflow_assignees.departments.description";

    /// <summary>
    /// 流程定义不存在。
    /// </summary>
    /// <remarks>
    /// 参数 <c>id</c>。
    /// </remarks>
    public const string DefinitionNotFound = "workflow.definitions.not_found";

    /// <summary>
    /// 流程实例不存在。
    /// </summary>
    /// <remarks>
    /// 参数 <c>id</c>。
    /// </remarks>
    public const string InstanceNotFound = "workflow.instances.not_found";

    /// <summary>
    /// 结束节点的结论不合法。
    /// </summary>
    /// <remarks>
    /// 参数 <c>node</c>、<c>outcome</c>、<c>allowed</c>。
    /// </remarks>
    public const string BusinessOutcomeInvalid = "workflow.business.outcome_invalid";

    /// <summary>
    /// 审批业务的定义里不许有终止节点。
    /// </summary>
    /// <remarks>
    /// 参数 <c>node</c>。
    /// </remarks>
    public const string BusinessTerminateForbidden = "workflow.business.terminate_forbidden";

    /// <summary>
    /// 变量或载荷要是 JSON 对象。
    /// </summary>
    /// <remarks>
    /// 参数 <c>field</c>。
    /// </remarks>
    public const string JsonObjectRequired = "workflow.variables.object_required";

    /// <summary>
    /// 不支持的筛选字段。
    /// </summary>
    /// <remarks>
    /// 参数 <c>field</c>。
    /// </remarks>
    public const string FilterUnsupported = "workflow.query.filter_unsupported";

    /// <summary>
    /// 不支持的排序字段。
    /// </summary>
    /// <remarks>
    /// 参数 <c>field</c>。
    /// </remarks>
    public const string SortUnsupported = "workflow.query.sort_unsupported";

    /// <summary>
    /// 编码类受理人规则没给编码。
    /// </summary>
    /// <remarks>
    /// 参数 <c>type</c>。
    /// </remarks>
    public const string AssigneeCodesRequired = "workflow.assignees.codes_required";

    /// <summary>
    /// 主管类受理人规则的层级不合法。
    /// </summary>
    /// <remarks>
    /// 参数 <c>type</c>、<c>value</c>、<c>max</c>。
    /// </remarks>
    public const string AssigneeLevelInvalid = "workflow.assignees.level_invalid";

    private static readonly Lazy<MessageCatalog> _catalog = new(static () => MessageCatalog.FromEmbeddedResources(CatalogName, typeof(WorkflowMessages)));

    /// <summary>
    /// 本包的消息目录。
    /// </summary>
    public static MessageCatalog Catalog => _catalog.Value;
}
