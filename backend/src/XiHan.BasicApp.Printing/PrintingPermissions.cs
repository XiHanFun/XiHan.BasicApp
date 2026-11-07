// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Security;

namespace XiHan.BasicApp.Printing;

/// <summary>
/// 打印模块的权限码
/// </summary>
/// <remarks>
/// 7 个码：前六个两侧都有，全局管理只在平台一侧
/// 平台一侧维护全局模板要同时持有对应的写权限与 <see cref="PrintTemplatesGlobalManage"/>，由操作判定
/// 数据源目录（<c>print-data-sources.list</c>）不另设权限码，持有读取或使用其一即可（<see cref="PrintingPolicies.DataSourceCatalog"/>）
/// 使用（解析）是只读操作，不进操作日志（读操作不标 <c>audited</c>）
/// </remarks>
public static class PrintingPermissions
{
    /// <summary>
    /// 查看模板
    /// </summary>
    public const string PrintTemplatesRead = "printing.print-templates.read";

    /// <summary>
    /// 新建模板
    /// </summary>
    public const string PrintTemplatesCreate = "printing.print-templates.create";

    /// <summary>
    /// 修改模板（元数据与设计）
    /// </summary>
    public const string PrintTemplatesUpdate = "printing.print-templates.update";

    /// <summary>
    /// 启停模板
    /// </summary>
    public const string PrintTemplatesStatus = "printing.print-templates.status";

    /// <summary>
    /// 删除模板
    /// </summary>
    public const string PrintTemplatesDelete = "printing.print-templates.delete";

    /// <summary>
    /// 按编码解析模板以预览与打印
    /// </summary>
    public const string PrintTemplatesUse = "printing.print-templates.use";

    /// <summary>
    /// 维护平台全局模板
    /// </summary>
    public const string PrintTemplatesGlobalManage = "printing.print-templates.global-manage";

    /// <summary>
    /// 全部权限描述
    /// </summary>
    public static IReadOnlyList<PermissionDescriptor> All { get; } =
    [
        new(PrintTemplatesRead, "Read print templates", PermissionScope.Both, description: "View the caller's own templates and the global templates open to tenants; the platform sees every global template."),
        new(PrintTemplatesCreate, "Create print templates", PermissionScope.Both, audited: true, description: "Create templates on the caller's side; global templates also need global management."),
        new(PrintTemplatesUpdate, "Update print templates", PermissionScope.Both, audited: true, description: "Edit the metadata and the design of templates on the caller's side."),
        new(PrintTemplatesStatus, "Enable or disable print templates", PermissionScope.Both, audited: true, description: "Enable or disable templates on the caller's side."),
        new(PrintTemplatesDelete, "Delete print templates", PermissionScope.Both, audited: true, description: "Delete disabled templates on the caller's side."),
        new(PrintTemplatesUse, "Use print templates", PermissionScope.Both, description: "Resolve templates by code to preview and print them, and read the data source catalog."),
        new(PrintTemplatesGlobalManage, "Manage global print templates", PermissionScope.Platform, audited: true, description: "Maintain the platform's global templates, together with the matching create, update, status or delete permission."),
    ];
}
