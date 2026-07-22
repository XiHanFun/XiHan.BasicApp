// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Workflow.Domain.Permissions;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.Data.SqlSugar.Seeders;

namespace XiHan.BasicApp.Workflow.Infrastructure.Seeders.System;

/// <summary>
/// 系统菜单种子数据
/// </summary>
/// <remarks>
/// 工作流是业务功能：顶级目录与"我的待办"不绑权限（登录即可见），
/// "流程定义/流程实例"绑定 workflow:read（建即绑，不靠后置回填）。
/// 权限种子（SysPermissionSeeder）先行；自愈仅作用于本模块菜单编码。
/// </remarks>
public class SysMenuSeeder : DataSeederBase
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public SysMenuSeeder(ISqlSugarClientResolver clientResolver, ILogger<SysMenuSeeder> logger, IServiceProvider serviceProvider)
        : base(clientResolver, logger, serviceProvider)
    {
    }

    /// <summary>
    /// 种子数据优先级（置于 SysPermissionSeeder 之后，确保建菜单时 workflow:read 已存在）
    /// </summary>
    public override int Order => 303;

    /// <summary>
    /// 种子数据名称
    /// </summary>
    public override string Name => "[Workflow]系统菜单种子数据";

    /// <summary>
    /// 种子数据实现
    /// </summary>
    protected override async Task SeedInternalAsync()
    {
        var client = DbClient;

        var readPermission = await client.Queryable<SysPermission>()
            .FirstAsync(p => p.PermissionCode == WorkflowPermissionCodes.Read);
        if (readPermission is null)
        {
            Logger.LogWarning("workflow:read 权限不存在，无法绑定工作流菜单可见性，跳过工作流菜单种子");
            return;
        }

        var readPermissionId = readPermission.BasicId;

        // 局部变量而非私有静态字段：SqlSugar 表达式解析器不允许 lambda 内访问私有字段（"Field can't be private"）
        string[] menuCodes = ["workflow", "workflow_definition", "workflow_instance", "workflow_todo"];
        var exists = await client.Queryable<SysMenu>().Where(m => menuCodes.Contains(m.MenuCode)).ToListAsync();
        var existsCodes = exists.Select(x => x.MenuCode).ToHashSet();
        var addList = new List<SysMenu>();

        // 顶级目录：不绑权限，任一子菜单可见即可进入
        if (!existsCodes.Contains("workflow"))
        {
            addList.Add(new SysMenu { ParentId = null, PermissionId = null, MenuName = "工作流", MenuCode = "workflow", MenuType = MenuType.Directory, Path = "/workflow", Component = null, RouteName = "Workflow", Icon = "lucide:workflow", Title = "工作流", I18nKey = "menu.workflow", IsExternal = false, IsCache = false, IsVisible = true, IsAffix = false, Status = EnableStatus.Enabled, Sort = 430, Remark = "工作流目录" });
        }

        if (!existsCodes.Contains("workflow_todo"))
        {
            addList.Add(new SysMenu { ParentId = null, PermissionId = null, MenuName = "我的待办", MenuCode = "workflow_todo", MenuType = MenuType.Menu, Path = "/workflow/todo", Component = "Workflow/Todo/Index", RouteName = "WorkflowTodo", Icon = "lucide:list-todo", Title = "我的待办", I18nKey = "menu.workflow_todo", IsExternal = false, IsCache = true, IsVisible = true, IsAffix = false, Status = EnableStatus.Enabled, Sort = 431, Remark = "当前用户的审批待办" });
        }

        if (!existsCodes.Contains("workflow_definition"))
        {
            addList.Add(new SysMenu { ParentId = null, PermissionId = readPermissionId, MenuName = "流程定义", MenuCode = "workflow_definition", MenuType = MenuType.Menu, Path = "/workflow/definition", Component = "Workflow/Definition/Index", RouteName = "WorkflowDefinition", Icon = "lucide:git-branch", Title = "流程定义", I18nKey = "menu.workflow_definition", IsExternal = false, IsCache = true, IsVisible = true, IsAffix = false, Status = EnableStatus.Enabled, Sort = 432, Remark = "流程定义与版本管理" });
        }

        if (!existsCodes.Contains("workflow_instance"))
        {
            addList.Add(new SysMenu { ParentId = null, PermissionId = readPermissionId, MenuName = "流程实例", MenuCode = "workflow_instance", MenuType = MenuType.Menu, Path = "/workflow/instance", Component = "Workflow/Instance/Index", RouteName = "WorkflowInstance", Icon = "lucide:activity", Title = "流程实例", I18nKey = "menu.workflow_instance", IsExternal = false, IsCache = true, IsVisible = true, IsAffix = false, Status = EnableStatus.Enabled, Sort = 433, Remark = "流程实例监控与运维" });
        }

        if (addList.Count > 0)
        {
            await BulkInsertAsync(addList);
        }

        // 子菜单归位到工作流目录
        var parentMenu = await client.Queryable<SysMenu>().FirstAsync(m => m.MenuCode == "workflow");
        if (parentMenu != null)
        {
            await client.Updateable<SysMenu>()
                .SetColumns(m => m.ParentId == parentMenu.BasicId)
                .Where(m => m.MenuCode == "workflow_definition" || m.MenuCode == "workflow_instance" || m.MenuCode == "workflow_todo")
                .ExecuteCommandAsync();
        }

        // 幂等自愈：仅对本模块菜单统一绑定权限、解除隐藏并启用
        var fixedCount = await client.Updateable<SysMenu>()
            .SetColumns(m => m.PermissionId == readPermissionId)
            .SetColumns(m => m.IsVisible == true)
            .SetColumns(m => m.Status == EnableStatus.Enabled)
            .Where(m => m.MenuCode == "workflow_definition" || m.MenuCode == "workflow_instance")
            .ExecuteCommandAsync();

        Logger.LogInformation("初始化工作流菜单：新增 {AddCount} 个，绑定权限/解除隐藏 {FixedCount} 个", addList.Count, fixedCount);
    }
}
