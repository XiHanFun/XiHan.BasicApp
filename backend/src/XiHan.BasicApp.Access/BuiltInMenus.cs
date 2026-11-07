// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Access.Domain.Entities;
using XiHan.BasicApp.Contracts.Pages;

namespace XiHan.BasicApp.Access;

/// <summary>
/// 内置的目录与外链节点
/// </summary>
/// <param name="Code">节点编码（<c>custom.&lt;名&gt;</c>）</param>
/// <param name="Kind">种类：目录或外链</param>
/// <param name="TitleKey">标题文案键（运营没改标题时按语言取）</param>
/// <param name="Icon">图标</param>
/// <param name="Order">排序号</param>
/// <param name="ParentCode">所属目录的节点编码，顶层为空</param>
/// <param name="ExternalUrl">外链地址，目录为空</param>
public sealed record BuiltInMenu(string Code, MenuKind Kind, string TitleKey, string Icon, int Order, string? ParentCode = null, string? ExternalUrl = null);

/// <summary>
/// 内置的运营节点：各包页面登记指到的目录（见 <see cref="MenuDirectories"/>）与帮助中心的三个外链
/// </summary>
/// <remarks>
/// 种类与运营建的一样（可改、可停用、可删），种子 <c>access.menus</c> 只首写：写过一次之后不再碰，删掉的也不补。上级目录排在它的下级之前，种子按顺序写才找得到上级
/// </remarks>
public static class BuiltInMenus
{
    /// <summary>
    /// 全部内置节点（目录排在它的外链之前）
    /// </summary>
    public static IReadOnlyList<BuiltInMenu> All { get; } =
    [
        new(MenuDirectories.AiCenter, MenuKind.Directory, "menu.ai_center", "lucide:brain-circuit", 100),
        new(MenuDirectories.WorkflowCenter, MenuKind.Directory, "menu.workflow_center", "lucide:workflow", 200),
        new(MenuDirectories.IntegrationCenter, MenuKind.Directory, "menu.integration_center", "lucide:plug-zap", 300),
        new(MenuDirectories.PushService, MenuKind.Directory, "menu.push_service", "lucide:bell-ring", 600, MenuDirectories.IntegrationCenter),
        new(MenuDirectories.OpenApi, MenuKind.Directory, "menu.open_api", "lucide:plug", 610, MenuDirectories.IntegrationCenter),
        new(MenuDirectories.MessageCenter, MenuKind.Directory, "menu.message_center", "lucide:mails", 400),
        new(MenuDirectories.TenantManagement, MenuKind.Directory, "menu.tenant_management", "lucide:building-2", 500),
        new(MenuDirectories.SystemManagement, MenuKind.Directory, "menu.system_management", "lucide:settings", 600),
        new(MenuDirectories.SystemPermission, MenuKind.Directory, "menu.system_permission", "lucide:shield-check", 100, MenuDirectories.SystemManagement),
        new(MenuDirectories.SystemSettings, MenuKind.Directory, "menu.system_settings", "lucide:sliders-horizontal", 200, MenuDirectories.SystemManagement),
        new(MenuDirectories.SystemServices, MenuKind.Directory, "menu.system_services", "lucide:server-cog", 300, MenuDirectories.SystemManagement),
        new(MenuDirectories.SystemMonitor, MenuKind.Directory, "menu.system_monitor", "lucide:activity", 400, MenuDirectories.SystemManagement),
        new(MenuDirectories.SystemLogs, MenuKind.Directory, "menu.system_logs", "lucide:scroll-text", 500, MenuDirectories.SystemManagement),
        new(MenuDirectories.FileServices, MenuKind.Directory, "menu.file_services", "lucide:folder-cog", 600, MenuDirectories.SystemManagement),
        new(MenuDirectories.OnlineDev, MenuKind.Directory, "menu.online_dev", "lucide:code", 700, MenuDirectories.SystemManagement),
        new(MenuDirectories.About, MenuKind.Directory, "menu.about", "lucide:info", 1100),
        new("custom.about-docs", MenuKind.Link, "menu.about_docs", "lucide:book-open-text", 1120, MenuDirectories.About, "https://docs.xihanfun.com"),
        new("custom.about-github", MenuKind.Link, "menu.about_github", "lucide:github", 1130, MenuDirectories.About, "https://github.com/XiHanFun/XiHan.BasicApp"),
        new("custom.about-gitee", MenuKind.Link, "menu.about_gitee", "lucide:git-branch", 1140, MenuDirectories.About, "https://gitee.com/XiHanFun/XiHan.BasicApp"),
    ];
}
