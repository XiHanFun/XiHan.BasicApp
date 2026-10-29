// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Settings;

/// <summary>
/// 设置包的错误码。
/// </summary>
/// <remarks>
/// 格式为 <c>SETTINGS_&lt;KIND&gt;_&lt;NNN&gt;</c>。
/// </remarks>
public static class SettingsErrors
{
    /// <summary>
    /// 参数不存在（或不在当前可见范围）。
    /// </summary>
    public const string ConfigNotFound = "SETTINGS_NOTFOUND_001";

    /// <summary>
    /// 字典不存在（或不在当前可见范围）。
    /// </summary>
    public const string DictNotFound = "SETTINGS_NOTFOUND_002";

    /// <summary>
    /// 字典项不存在。
    /// </summary>
    public const string DictItemNotFound = "SETTINGS_NOTFOUND_003";

    /// <summary>
    /// 编号规则不存在（或当前一侧不能用）。
    /// </summary>
    public const string NumberingRuleNotFound = "SETTINGS_NOTFOUND_004";

    /// <summary>
    /// 参数键在本侧已被占用。
    /// </summary>
    public const string ConfigKeyTaken = "SETTINGS_CONFLICT_001";

    /// <summary>
    /// 字典编码已被占用（字典不覆盖：平台与租户之间也不能同码）。
    /// </summary>
    public const string DictCodeTaken = "SETTINGS_CONFLICT_002";

    /// <summary>
    /// 字典里已有同码的项。
    /// </summary>
    public const string DictItemCodeTaken = "SETTINGS_CONFLICT_003";

    /// <summary>
    /// 字典还有字典项，不能删除。
    /// </summary>
    public const string DictHasItems = "SETTINGS_CONFLICT_004";

    /// <summary>
    /// 字典项还有下级，不能删除。
    /// </summary>
    public const string DictItemHasChildren = "SETTINGS_CONFLICT_005";

    /// <summary>
    /// 内建数据不能删除，内建参数不能改数据类型。
    /// </summary>
    public const string BuiltInProtected = "SETTINGS_CONFLICT_006";

    /// <summary>
    /// 字典已停用，不能添加字典项。
    /// </summary>
    public const string DictDisabled = "SETTINGS_CONFLICT_007";

    /// <summary>
    /// 编号规则编码在本侧已被占用。
    /// </summary>
    public const string NumberingRuleCodeTaken = "SETTINGS_CONFLICT_008";

    /// <summary>
    /// 编号规则已经发过号：格式冻结，不能删除。
    /// </summary>
    public const string NumberingRuleFrozen = "SETTINGS_CONFLICT_009";

    /// <summary>
    /// 编号规则已停用，不能发号。
    /// </summary>
    public const string NumberingRuleDisabled = "SETTINGS_CONFLICT_010";

    /// <summary>
    /// 本周期的流水已用尽。
    /// </summary>
    public const string NumberingExhausted = "SETTINGS_CONFLICT_011";

    /// <summary>
    /// 规则已进入更晚的周期，本机时钟落后，拒绝发号。
    /// </summary>
    public const string NumberingClockBehind = "SETTINGS_CONFLICT_012";

    /// <summary>
    /// 幂等键已用于参数不同的发号请求。
    /// </summary>
    public const string IdempotencyMismatch = "SETTINGS_CONFLICT_013";

    /// <summary>
    /// 列表不支持该筛选或排序。
    /// </summary>
    public const string QueryUnsupported = "SETTINGS_VALIDATION_001";

    /// <summary>
    /// 参数值或缺省值与数据类型不符。
    /// </summary>
    public const string ValueTypeMismatch = "SETTINGS_VALIDATION_002";

    /// <summary>
    /// 字典项元数据不是 JSON。
    /// </summary>
    public const string MetadataNotJson = "SETTINGS_VALIDATION_003";

    /// <summary>
    /// 上级项不在同一字典，或是自己 / 自己的下级。
    /// </summary>
    public const string DictItemParentInvalid = "SETTINGS_VALIDATION_004";

    /// <summary>
    /// 日期格式区分不了重置周期，会出重复号。
    /// </summary>
    public const string NumberingFormatUnsafe = "SETTINGS_VALIDATION_005";

    /// <summary>
    /// 时区标识无效。
    /// </summary>
    public const string TimeZoneInvalid = "SETTINGS_VALIDATION_006";

    /// <summary>
    /// 流水值超出流水位数能表示的范围。
    /// </summary>
    public const string NumberingCapacity = "SETTINGS_VALIDATION_007";

    /// <summary>
    /// 重置平台规则没有回填规则编码确认。
    /// </summary>
    public const string NumberingResetConfirmation = "SETTINGS_VALIDATION_008";

    /// <summary>
    /// 平台上下文不能按租户范围发号。
    /// </summary>
    public const string NumberingScopeInvalid = "SETTINGS_VALIDATION_009";

    /// <summary>
    /// 重置的下一个流水值没有大于本周期已发出的最大值。
    /// </summary>
    public const string NumberingResetBelowIssued = "SETTINGS_VALIDATION_010";

    /// <summary>
    /// 数据属于另一侧：平台数据只能在平台维护，租户要不同的值请新建自己的。
    /// </summary>
    public const string OtherSide = "SETTINGS_DENIED_001";

    /// <summary>
    /// 平台参数不允许租户覆盖。
    /// </summary>
    public const string TenantOverrideForbidden = "SETTINGS_DENIED_002";

    /// <summary>
    /// 加密参数的密文解不开（数据保护密钥环可能已更换）。
    /// </summary>
    public const string ConfigUndecryptable = "SETTINGS_UNEXPECTED_001";
}
