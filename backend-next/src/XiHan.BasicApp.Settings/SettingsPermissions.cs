// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Security;

namespace XiHan.BasicApp.Settings;

/// <summary>
/// 设置包的权限码。
/// </summary>
/// <remarks>
/// 全部两侧都有（平台维护平台数据，租户维护自己的覆盖与数据；只能写本侧，由操作按属主判定）。
/// 字典选项（<c>dicts.options</c>）只要求已登录，不在这里。
/// </remarks>
public static class SettingsPermissions
{
    /// <summary>
    /// 查看参数。
    /// </summary>
    public const string ConfigsRead = "settings.configs.read";

    /// <summary>
    /// 新建参数（含租户覆盖平台参数）。
    /// </summary>
    public const string ConfigsCreate = "settings.configs.create";

    /// <summary>
    /// 修改参数。
    /// </summary>
    public const string ConfigsUpdate = "settings.configs.update";

    /// <summary>
    /// 删除参数。
    /// </summary>
    public const string ConfigsDelete = "settings.configs.delete";

    /// <summary>
    /// 查看字典。
    /// </summary>
    public const string DictsRead = "settings.dicts.read";

    /// <summary>
    /// 新建字典。
    /// </summary>
    public const string DictsCreate = "settings.dicts.create";

    /// <summary>
    /// 修改字典。
    /// </summary>
    public const string DictsUpdate = "settings.dicts.update";

    /// <summary>
    /// 删除字典。
    /// </summary>
    public const string DictsDelete = "settings.dicts.delete";

    /// <summary>
    /// 查看字典项。
    /// </summary>
    public const string DictItemsRead = "settings.dict-items.read";

    /// <summary>
    /// 新建字典项。
    /// </summary>
    public const string DictItemsCreate = "settings.dict-items.create";

    /// <summary>
    /// 修改字典项。
    /// </summary>
    public const string DictItemsUpdate = "settings.dict-items.update";

    /// <summary>
    /// 删除字典项。
    /// </summary>
    public const string DictItemsDelete = "settings.dict-items.delete";

    /// <summary>
    /// 查看编号规则与发号记录。
    /// </summary>
    public const string NumberingRulesRead = "settings.numbering-rules.read";

    /// <summary>
    /// 新建编号规则。
    /// </summary>
    public const string NumberingRulesCreate = "settings.numbering-rules.create";

    /// <summary>
    /// 修改编号规则。
    /// </summary>
    public const string NumberingRulesUpdate = "settings.numbering-rules.update";

    /// <summary>
    /// 删除编号规则。
    /// </summary>
    public const string NumberingRulesDelete = "settings.numbering-rules.delete";

    /// <summary>
    /// 重置编号规则的流水。
    /// </summary>
    public const string NumberingRulesReset = "settings.numbering-rules.reset";

    /// <summary>
    /// 按规则发号。
    /// </summary>
    public const string NumberingRulesGenerate = "settings.numbering-rules.generate";

    /// <summary>
    /// 全部权限描述。
    /// </summary>
    public static IReadOnlyList<PermissionDescriptor> All { get; } =
    [
        new(ConfigsRead, "Read configuration", PermissionScope.Both, description: "View platform configuration entries and the caller's own; encrypted values are never shown."),
        new(ConfigsCreate, "Create configuration", PermissionScope.Both, audited: true, description: "Create configuration entries on the caller's side, including tenant overrides of platform entries."),
        new(ConfigsUpdate, "Update configuration", PermissionScope.Both, audited: true, description: "Edit, encrypt and disable configuration entries on the caller's side."),
        new(ConfigsDelete, "Delete configuration", PermissionScope.Both, audited: true, description: "Delete configuration entries on the caller's side that are not built in."),
        new(DictsRead, "Read dictionaries", PermissionScope.Both, description: "View platform dictionaries and the caller's own."),
        new(DictsCreate, "Create dictionaries", PermissionScope.Both, audited: true, description: "Create dictionaries on the caller's side."),
        new(DictsUpdate, "Update dictionaries", PermissionScope.Both, audited: true, description: "Edit and disable dictionaries on the caller's side."),
        new(DictsDelete, "Delete dictionaries", PermissionScope.Both, audited: true, description: "Delete empty dictionaries on the caller's side that are not built in."),
        new(DictItemsRead, "Read dictionary items", PermissionScope.Both, description: "View the items of visible dictionaries."),
        new(DictItemsCreate, "Create dictionary items", PermissionScope.Both, audited: true, description: "Add items to enabled dictionaries on the caller's side."),
        new(DictItemsUpdate, "Update dictionary items", PermissionScope.Both, audited: true, description: "Edit, move and disable dictionary items on the caller's side."),
        new(DictItemsDelete, "Delete dictionary items", PermissionScope.Both, audited: true, description: "Delete dictionary items without children on the caller's side."),
        new(NumberingRulesRead, "Read numbering rules", PermissionScope.Both, description: "View numbering rules, preview formats and read issue records."),
        new(NumberingRulesCreate, "Create numbering rules", PermissionScope.Both, audited: true, description: "Create numbering rules on the caller's side."),
        new(NumberingRulesUpdate, "Update numbering rules", PermissionScope.Both, audited: true, description: "Edit numbering rules on the caller's side; the format freezes after the first issue."),
        new(NumberingRulesDelete, "Delete numbering rules", PermissionScope.Both, audited: true, description: "Delete numbering rules on the caller's side that never issued a number."),
        new(NumberingRulesReset, "Reset numbering rules", PermissionScope.Both, audited: true, description: "Move a rule's sequence forward past every value issued in the current period."),
        new(NumberingRulesGenerate, "Issue numbers", PermissionScope.Both, audited: true, description: "Issue numbers from the caller's rules or from platform rules shared with tenants."),
    ];
}
