// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.Framework.Localization.Abstractions.Messages;

namespace XiHan.BasicApp.Settings;

/// <summary>
/// 设置包的消息键；各区域文案在 <c>Localization/&lt;区域&gt;.json</c>。操作说明用约定键 <c>&lt;操作 ID&gt;.description</c>（资源段的 <c>-</c> 换成 <c>_</c>）。
/// </summary>
public static class SettingsMessages
{
    /// <summary>
    /// 消息目录名。
    /// </summary>
    public const string CatalogName = "basicapp.settings";

    /// <summary>
    /// 操作 <c>configs.list</c> 的说明。
    /// </summary>
    public const string ListConfigsDescription = "configs.list.description";

    /// <summary>
    /// 操作 <c>configs.get</c> 的说明。
    /// </summary>
    public const string GetConfigDescription = "configs.get.description";

    /// <summary>
    /// 操作 <c>configs.create</c> 的说明。
    /// </summary>
    public const string CreateConfigDescription = "configs.create.description";

    /// <summary>
    /// 操作 <c>configs.update</c> 的说明。
    /// </summary>
    public const string UpdateConfigDescription = "configs.update.description";

    /// <summary>
    /// 操作 <c>configs.delete</c> 的说明。
    /// </summary>
    public const string DeleteConfigsDescription = "configs.delete.description";

    /// <summary>
    /// 操作 <c>dicts.list</c> 的说明。
    /// </summary>
    public const string ListDictsDescription = "dicts.list.description";

    /// <summary>
    /// 操作 <c>dicts.get</c> 的说明。
    /// </summary>
    public const string GetDictDescription = "dicts.get.description";

    /// <summary>
    /// 操作 <c>dicts.create</c> 的说明。
    /// </summary>
    public const string CreateDictDescription = "dicts.create.description";

    /// <summary>
    /// 操作 <c>dicts.update</c> 的说明。
    /// </summary>
    public const string UpdateDictDescription = "dicts.update.description";

    /// <summary>
    /// 操作 <c>dicts.delete</c> 的说明。
    /// </summary>
    public const string DeleteDictsDescription = "dicts.delete.description";

    /// <summary>
    /// 操作 <c>dicts.options</c> 的说明。
    /// </summary>
    public const string DictOptionsDescription = "dicts.options.description";

    /// <summary>
    /// 操作 <c>dict-items.list</c> 的说明。
    /// </summary>
    public const string ListDictItemsDescription = "dict_items.list.description";

    /// <summary>
    /// 操作 <c>dict-items.get</c> 的说明。
    /// </summary>
    public const string GetDictItemDescription = "dict_items.get.description";

    /// <summary>
    /// 操作 <c>dict-items.create</c> 的说明。
    /// </summary>
    public const string CreateDictItemDescription = "dict_items.create.description";

    /// <summary>
    /// 操作 <c>dict-items.update</c> 的说明。
    /// </summary>
    public const string UpdateDictItemDescription = "dict_items.update.description";

    /// <summary>
    /// 操作 <c>dict-items.delete</c> 的说明。
    /// </summary>
    public const string DeleteDictItemsDescription = "dict_items.delete.description";

    /// <summary>
    /// 操作 <c>numbering-rules.list</c> 的说明。
    /// </summary>
    public const string ListNumberingRulesDescription = "numbering_rules.list.description";

    /// <summary>
    /// 操作 <c>numbering-rules.get</c> 的说明。
    /// </summary>
    public const string GetNumberingRuleDescription = "numbering_rules.get.description";

    /// <summary>
    /// 操作 <c>numbering-rules.create</c> 的说明。
    /// </summary>
    public const string CreateNumberingRuleDescription = "numbering_rules.create.description";

    /// <summary>
    /// 操作 <c>numbering-rules.update</c> 的说明。
    /// </summary>
    public const string UpdateNumberingRuleDescription = "numbering_rules.update.description";

    /// <summary>
    /// 操作 <c>numbering-rules.delete</c> 的说明。
    /// </summary>
    public const string DeleteNumberingRulesDescription = "numbering_rules.delete.description";

    /// <summary>
    /// 操作 <c>numbering-rules.reset</c> 的说明。
    /// </summary>
    public const string ResetNumberingRuleDescription = "numbering_rules.reset.description";

    /// <summary>
    /// 操作 <c>numbering-rules.preview</c> 的说明。
    /// </summary>
    public const string PreviewNumberingDescription = "numbering_rules.preview.description";

    /// <summary>
    /// 操作 <c>numbering-rules.generate</c> 的说明。
    /// </summary>
    public const string GenerateNumbersDescription = "numbering_rules.generate.description";

    /// <summary>
    /// 操作 <c>numbering-rules.allocations</c> 的说明。
    /// </summary>
    public const string ListNumberingAllocationsDescription = "numbering_rules.allocations.description";

    /// <summary>
    /// 参数不存在。
    /// </summary>
    public const string ConfigNotFound = "settings.config.not_found";

    /// <summary>
    /// 字典不存在。
    /// </summary>
    public const string DictNotFound = "settings.dict.not_found";

    /// <summary>
    /// 按编码找不到字典。
    /// </summary>
    public const string DictCodeNotFound = "settings.dict.code_not_found";

    /// <summary>
    /// 字典项不存在。
    /// </summary>
    public const string DictItemNotFound = "settings.dict_item.not_found";

    /// <summary>
    /// 编号规则不存在。
    /// </summary>
    public const string NumberingRuleNotFound = "settings.numbering_rule.not_found";

    /// <summary>
    /// 按编码找不到可用的编号规则。
    /// </summary>
    public const string NumberingRuleCodeNotFound = "settings.numbering_rule.code_not_found";

    /// <summary>
    /// 参数键已被占用。
    /// </summary>
    public const string ConfigKeyTaken = "settings.config.key_taken";

    /// <summary>
    /// 字典编码已被占用。
    /// </summary>
    public const string DictCodeTaken = "settings.dict.code_taken";

    /// <summary>
    /// 字典项编码已被占用。
    /// </summary>
    public const string DictItemCodeTaken = "settings.dict_item.code_taken";

    /// <summary>
    /// 字典还有字典项。
    /// </summary>
    public const string DictHasItems = "settings.dict.has_items";

    /// <summary>
    /// 字典项还有下级。
    /// </summary>
    public const string DictItemHasChildren = "settings.dict_item.has_children";

    /// <summary>
    /// 内建数据受保护。
    /// </summary>
    public const string BuiltInProtected = "settings.built_in";

    /// <summary>
    /// 字典已停用。
    /// </summary>
    public const string DictDisabled = "settings.dict.disabled";

    /// <summary>
    /// 编号规则编码已被占用。
    /// </summary>
    public const string NumberingRuleCodeTaken = "settings.numbering_rule.code_taken";

    /// <summary>
    /// 编号规则已发过号。
    /// </summary>
    public const string NumberingRuleFrozen = "settings.numbering_rule.frozen";

    /// <summary>
    /// 编号规则已停用。
    /// </summary>
    public const string NumberingRuleDisabled = "settings.numbering_rule.disabled";

    /// <summary>
    /// 流水用尽。
    /// </summary>
    public const string NumberingExhausted = "settings.numbering_rule.exhausted";

    /// <summary>
    /// 时钟落后。
    /// </summary>
    public const string NumberingClockBehind = "settings.numbering_rule.clock_behind";

    /// <summary>
    /// 幂等键同键异参。
    /// </summary>
    public const string IdempotencyMismatch = "settings.numbering.idempotency_mismatch";

    /// <summary>
    /// 不支持的筛选。
    /// </summary>
    public const string FilterUnsupported = "settings.query.filter_unsupported";

    /// <summary>
    /// 不支持的排序。
    /// </summary>
    public const string SortUnsupported = "settings.query.sort_unsupported";

    /// <summary>
    /// 值与数据类型不符。
    /// </summary>
    public const string ValueTypeMismatch = "settings.config.value_type";

    /// <summary>
    /// 元数据不是 JSON。
    /// </summary>
    public const string MetadataNotJson = "settings.dict_item.metadata_not_json";

    /// <summary>
    /// 上级项不合法。
    /// </summary>
    public const string DictItemParentInvalid = "settings.dict_item.parent_invalid";

    /// <summary>
    /// 日期格式区分不了重置周期。
    /// </summary>
    public const string NumberingFormatUnsafe = "settings.numbering_rule.format_unsafe";

    /// <summary>
    /// 时区无效。
    /// </summary>
    public const string TimeZoneInvalid = "settings.numbering_rule.time_zone_invalid";

    /// <summary>
    /// 流水值超出位数范围。
    /// </summary>
    public const string NumberingCapacity = "settings.numbering_rule.capacity";

    /// <summary>
    /// 重置平台规则要回填编码。
    /// </summary>
    public const string NumberingResetConfirmation = "settings.numbering_rule.reset_confirmation";

    /// <summary>
    /// 平台上下文不能按租户范围发号。
    /// </summary>
    public const string NumberingScopeInvalid = "settings.numbering.scope_invalid";

    /// <summary>
    /// 重置值没有大于已发出的最大值。
    /// </summary>
    public const string NumberingResetBelowIssued = "settings.numbering_rule.reset_below_issued";

    /// <summary>
    /// 数据属于另一侧。
    /// </summary>
    public const string OtherSide = "settings.other_side";

    /// <summary>
    /// 平台参数不允许租户覆盖。
    /// </summary>
    public const string TenantOverrideForbidden = "settings.config.override_forbidden";

    /// <summary>
    /// 加密参数解不开。
    /// </summary>
    public const string ConfigUndecryptable = "settings.config.undecryptable";

    private static readonly Lazy<MessageCatalog> _catalog = new(static () => MessageCatalog.FromEmbeddedResources(CatalogName, typeof(SettingsMessages)));

    /// <summary>
    /// 本包的消息目录（嵌入的 10 种区域文件）。
    /// </summary>
    public static MessageCatalog Catalog => _catalog.Value;
}
