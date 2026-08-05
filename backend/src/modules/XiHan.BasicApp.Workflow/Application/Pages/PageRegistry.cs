// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Application.Pages;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Workflow.Domain.Permissions;

namespace XiHan.BasicApp.Workflow.Application.Pages;

/// <summary>
/// 工作流模块页面登记表 — 本模块页面的单一事实源，菜单种子数据从此处生成
/// </summary>
/// <remarks>
/// 沿用 Saas 侧 <see cref="PageDescriptor"/> 的一致性约定（Component 对应 src/views 目录、
/// I18nKey 为 menu.{Code 中 . 与 - 替换为 _} 并在前端 menu.ts 维护双语文案）。
/// 工作流是业务功能：顶级目录与「我的待办」不绑权限（登录即可见），
/// 「流程定义/流程实例」绑 workflow:read。
/// </remarks>
public static class PageRegistry
{
    /// <summary>
    /// 工作流目录码
    /// </summary>
    public const string WorkflowDirectoryCode = "workflow";

    /// <summary>
    /// 工作台目录码（Saas 模块登记的父级，我的待办挂其下）
    /// </summary>
    public const string WorkbenchDirectoryCode = "workbench";

    /// <summary>
    /// 所有已登记页面（父目录必须排在子项之前，种子依顺序解析 ParentId）
    /// </summary>
    public static IReadOnlyList<PageDescriptor> All { get; } =
    [
        // 与 Saas 模块登记同一份定义，两边必须保持一致
        new(WorkflowDirectoryCode, "工作流", "menu.workflow", MenuType.Directory, "/workflow", "Workflow", null, null, null, "lucide:workflow", 400),
        // 我的待办属于「与当前登录用户相关」，归工作台；工作流目录聚焦流程管理侧
        new("workflow_todo", "我的待办", "menu.workflow_todo", MenuType.Menu, "/workflow/todo", "WorkflowTodo", "Workflow/Todo/Index", WorkbenchDirectoryCode, null, "lucide:list-todo", 14),
        new("workflow_definition", "流程定义", "menu.workflow_definition", MenuType.Menu, "/workflow/definition", "WorkflowDefinition", "Workflow/Definition/Index", WorkflowDirectoryCode, WorkflowPermissionCodes.Read, "lucide:git-branch", 410),
        new("workflow_instance", "流程实例", "menu.workflow_instance", MenuType.Menu, "/workflow/instance", "WorkflowInstance", "Workflow/Instance/Index", WorkflowDirectoryCode, WorkflowPermissionCodes.Read, "lucide:activity", 420),
    ];

    /// <summary>
    /// 页面内按钮（本模块暂无独立按钮级权限）
    /// </summary>
    public static IReadOnlyList<ButtonDescriptor> Buttons { get; } = [];
}
