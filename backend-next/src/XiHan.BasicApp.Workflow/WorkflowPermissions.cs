// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Security;

namespace XiHan.BasicApp.Workflow;

/// <summary>
/// 工作流的权限码。
/// </summary>
/// <remarks>
/// 照 4.x 的分法：定义的读、建（含新版本）、改（改草稿、发布、停用、归档）、删；实例的读、执行（发起、取消、终止、重试、信号）、改（挂起与恢复）；
/// 选角色、岗位与部门要受理人目录的读。两侧都有（W3）：平台管理平台自己的定义与实例。待办、审批中心与选用户只要登录。
/// </remarks>
public static class WorkflowPermissions
{
    /// <summary>
    /// 查看流程定义。
    /// </summary>
    public const string DefinitionsRead = "workflow.workflow-definitions.read";

    /// <summary>
    /// 新建流程定义（含新版本）。
    /// </summary>
    public const string DefinitionsCreate = "workflow.workflow-definitions.create";

    /// <summary>
    /// 修改流程定义（改草稿、发布、停用、归档）。
    /// </summary>
    public const string DefinitionsUpdate = "workflow.workflow-definitions.update";

    /// <summary>
    /// 删除流程定义草稿。
    /// </summary>
    public const string DefinitionsDelete = "workflow.workflow-definitions.delete";

    /// <summary>
    /// 查看流程实例。
    /// </summary>
    public const string InstancesRead = "workflow.workflow-instances.read";

    /// <summary>
    /// 执行流程实例（发起、取消、终止、重试、信号）。
    /// </summary>
    public const string InstancesExecute = "workflow.workflow-instances.execute";

    /// <summary>
    /// 挂起与恢复流程实例。
    /// </summary>
    public const string InstancesUpdate = "workflow.workflow-instances.update";

    /// <summary>
    /// 查看受理人目录（角色、岗位、部门）。
    /// </summary>
    public const string AssigneesRead = "workflow.workflow-assignees.read";

    /// <summary>
    /// 全部权限描述。
    /// </summary>
    public static IReadOnlyList<PermissionDescriptor> All { get; } =
    [
        new(DefinitionsRead, "Read workflow definitions", PermissionScope.Both, description: "View the workflow definitions of the caller's side and their documents."),
        new(DefinitionsCreate, "Create workflow definitions", PermissionScope.Both, audited: true, description: "Create drafts and new versions of workflow definitions."),
        new(DefinitionsUpdate, "Update workflow definitions", PermissionScope.Both, audited: true, description: "Edit drafts, publish, disable and archive workflow definitions."),
        new(DefinitionsDelete, "Delete workflow definitions", PermissionScope.Both, audited: true, description: "Delete draft workflow definitions."),
        new(InstancesRead, "Read workflow instances", PermissionScope.Both, description: "View the workflow instances of the caller's side, their history and waits, and any approval detail."),
        new(InstancesExecute, "Execute workflow instances", PermissionScope.Both, audited: true, description: "Start, cancel, terminate and retry instances and publish signals."),
        new(InstancesUpdate, "Suspend and resume workflow instances", PermissionScope.Both, audited: true, description: "Suspend and resume running workflow instances."),
        new(AssigneesRead, "Read the assignee directory", PermissionScope.Both, description: "List roles, positions and departments when choosing assignees."),
    ];
}
