// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Printing.Domain.Permissions;
using XiHan.BasicApp.Saas.Application.Pages;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Printing.Application.Pages;

/// <summary>
/// 打印模块页面登记表 — 本模块页面的单一事实源，菜单种子数据从此处生成
/// </summary>
/// <remarks>
/// 沿用 Saas 侧 <see cref="PageDescriptor"/> 的一致性约定（Component 对应 src/views 目录、
/// I18nKey 为 menu.{Code 中 . 与 - 替换为 _} 并在前端 menu.ts 维护双语文案）。
/// 打印模板页挂在 Saas 持有的 setting（系统管理）目录下：目录归 Saas、本模块只挂页面，按 ParentCode 引用。
/// </remarks>
public static class PageRegistry
{
    /// <summary>
    /// 所有已登记页面（页面挂靠 Saas 的 setting 目录，无本模块目录）
    /// </summary>
    public static IReadOnlyList<PageDescriptor> All { get; } =
    [
        new("setting.print-template", "打印模板", "menu.setting_print_template", MenuType.Menu, "/setting/print-template", "SettingPrintTemplate", "setting/print-template/index", "setting", PrintingPermissionCodes.Read, "lucide:printer", 737),
    ];

    /// <summary>
    /// 页面内按钮
    /// </summary>
    public static IReadOnlyList<ButtonDescriptor> Buttons { get; } =
    [
        new("setting.print-template.create", "新增", "setting.print-template", PrintingPermissionCodes.Create, 1),
        new("setting.print-template.update", "设计编辑", "setting.print-template", PrintingPermissionCodes.Update, 2),
        new("setting.print-template.status", "启停", "setting.print-template", PrintingPermissionCodes.Status, 3),
        new("setting.print-template.delete", "删除", "setting.print-template", PrintingPermissionCodes.Delete, 4),
        new("setting.print-template.use", "预览与打印", "setting.print-template", PrintingPermissionCodes.Use, 5),
        new("setting.print-template.global-manage", "全局管理", "setting.print-template", PrintingPermissionCodes.GlobalManage, 6),
    ];
}
