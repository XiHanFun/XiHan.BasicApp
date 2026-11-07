// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Pages;

namespace XiHan.BasicApp.Printing;

/// <summary>
/// 打印模块的页面登记
/// </summary>
/// <remarks>
/// 只有打印模板一页，两侧都有：平台管全局模板，租户管本租户模板并能看可用的全局模板
/// </remarks>
public static class PrintingPages
{
    /// <summary>
    /// 打印模板页
    /// </summary>
    public static PageDescriptor PrintTemplates { get; } = new("printing.print-templates", "/printing/print-templates", "printing.print_templates.title", "lucide:printer", 820, PrintingPermissions.PrintTemplatesRead, directory: MenuDirectories.OnlineDev);

    /// <summary>
    /// 全部顶层页面
    /// </summary>
    public static IReadOnlyList<PageDescriptor> All { get; } = [PrintTemplates];
}
