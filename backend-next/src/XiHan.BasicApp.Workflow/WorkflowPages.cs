// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Pages;

namespace XiHan.BasicApp.Workflow;

/// <summary>
/// 工作流的页面登记。
/// </summary>
public static class WorkflowPages
{
    /// <summary>
    /// 全部页面。
    /// </summary>
    public static IReadOnlyList<PageDescriptor> All { get; } =
    [
        new("workflow.approvals", "/workflow/approvals", "workflow.approvals.title", "lucide:list-checks", 730),
        new("workflow.definitions", "/workflow/definitions", "workflow.definitions.title", "lucide:git-branch", 740, WorkflowPermissions.DefinitionsRead),
        new("workflow.instances", "/workflow/instances", "workflow.instances.title", "lucide:activity", 750, WorkflowPermissions.InstancesRead),
    ];
}
