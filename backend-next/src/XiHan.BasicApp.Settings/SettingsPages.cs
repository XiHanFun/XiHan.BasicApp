// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Pages;

namespace XiHan.BasicApp.Settings;

/// <summary>
/// 设置包的页面登记。
/// </summary>
/// <remarks>
/// 参数、字典（字典项在字典页里维护）、业务编号。两侧都有。
/// </remarks>
public static class SettingsPages
{
    /// <summary>
    /// 参数页。
    /// </summary>
    public static PageDescriptor Configs { get; } = new("settings.configs", "/settings/configs", "settings.configs.title", "lucide:sliders-horizontal", 400, SettingsPermissions.ConfigsRead);

    /// <summary>
    /// 字典页（字典与字典项）。
    /// </summary>
    public static PageDescriptor Dicts { get; } = new("settings.dicts", "/settings/dicts", "settings.dicts.title", "lucide:book-open", 410, SettingsPermissions.DictsRead);

    /// <summary>
    /// 业务编号页。
    /// </summary>
    public static PageDescriptor NumberingRules { get; } = new("settings.numbering-rules", "/settings/numbering-rules", "settings.numbering_rules.title", "lucide:hash", 420, SettingsPermissions.NumberingRulesRead);

    /// <summary>
    /// 全部顶层页面。
    /// </summary>
    public static IReadOnlyList<PageDescriptor> All { get; } = [Configs, Dicts, NumberingRules];
}
