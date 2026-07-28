// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 业务编号规则创建 DTO；规则归属由当前上下文和作用域决定，不接受任意租户 ID。
/// </summary>
public sealed class NumberingRuleCreateDto : BasicAppCDto
{
    /// <summary>创建作用域；租户上下文使用 Tenant/Auto，平台上下文使用 Global/Auto。</summary>
    public NumberingScope Scope { get; set; } = NumberingScope.Auto;

    /// <summary>规则编码。</summary>
    [Required]
    [StringLength(100)]
    public string RuleCode { get; set; } = string.Empty;

    /// <summary>规则名称。</summary>
    [Required]
    [StringLength(100)]
    public string RuleName { get; set; } = string.Empty;

    /// <summary>可选前缀。</summary>
    [StringLength(50)]
    public string? Prefix { get; set; }

    /// <summary>分隔符。</summary>
    [StringLength(10)]
    public string Separator { get; set; } = "-";

    /// <summary>日期格式。</summary>
    public NumberingDateFormat DateFormat { get; set; } = NumberingDateFormat.YyyyMMdd;

    /// <summary>流水位数，范围 1 至 18。</summary>
    [Range(1, 18)]
    public int SerialLength { get; set; } = 4;

    /// <summary>重置周期。</summary>
    public NumberingResetCycle ResetCycle { get; set; } = NumberingResetCycle.Daily;

    /// <summary>规则时区标识。</summary>
    [Required]
    [StringLength(100)]
    public string TimeZoneId { get; set; } = "UTC";

    /// <summary>全局规则是否允许租户使用；租户私有规则忽略该值。</summary>
    public bool AllowTenantUse { get; set; }

    /// <summary>启停状态。</summary>
    public EnableStatus Status { get; set; } = EnableStatus.Enabled;

    /// <summary>显示排序。</summary>
    [Range(0, int.MaxValue)]
    public int Sort { get; set; }

    /// <summary>备注。</summary>
    [StringLength(500)]
    public string? Remark { get; set; }
}

/// <summary>
/// 业务编号规则更新 DTO；规则编码和归属不可修改。
/// </summary>
public sealed class NumberingRuleUpdateDto : BasicAppUDto
{
    /// <summary>规则作用域。</summary>
    public NumberingScope Scope { get; set; } = NumberingScope.Auto;

    /// <summary>规则名称。</summary>
    [Required]
    [StringLength(100)]
    public string RuleName { get; set; } = string.Empty;

    /// <summary>可选前缀。</summary>
    [StringLength(50)]
    public string? Prefix { get; set; }

    /// <summary>分隔符。</summary>
    [StringLength(10)]
    public string Separator { get; set; } = "-";

    /// <summary>日期格式。</summary>
    public NumberingDateFormat DateFormat { get; set; }

    /// <summary>流水位数。</summary>
    [Range(1, 18)]
    public int SerialLength { get; set; }

    /// <summary>重置周期。</summary>
    public NumberingResetCycle ResetCycle { get; set; }

    /// <summary>规则时区标识。</summary>
    [Required]
    [StringLength(100)]
    public string TimeZoneId { get; set; } = "UTC";

    /// <summary>全局规则是否允许租户使用。</summary>
    public bool AllowTenantUse { get; set; }

    /// <summary>显示排序。</summary>
    [Range(0, int.MaxValue)]
    public int Sort { get; set; }

    /// <summary>备注。</summary>
    [StringLength(500)]
    public string? Remark { get; set; }
}

/// <summary>
/// 业务编号规则状态更新 DTO。
/// </summary>
public sealed class NumberingRuleStatusUpdateDto : BasicAppUDto
{
    /// <summary>规则作用域。</summary>
    public NumberingScope Scope { get; set; } = NumberingScope.Auto;

    /// <summary>目标状态。</summary>
    public EnableStatus Status { get; set; }

    /// <summary>可选备注。</summary>
    [StringLength(500)]
    public string? Remark { get; set; }
}

/// <summary>
/// 业务编号规则安全重置 DTO。
/// </summary>
public sealed class NumberingRuleResetDto : BasicAppUDto
{
    /// <summary>规则作用域。</summary>
    public NumberingScope Scope { get; set; } = NumberingScope.Auto;

    /// <summary>重置后的下一流水值。</summary>
    [Required]
    [RegularExpression("^[1-9][0-9]{0,17}$")]
    public string NextValue { get; set; } = "1";

    /// <summary>必填重置原因。</summary>
    [Required]
    [StringLength(500)]
    public string Reason { get; set; } = string.Empty;

    /// <summary>全局规则二次确认编码；租户规则可不传。</summary>
    [StringLength(100)]
    public string? ConfirmRuleCode { get; set; }
}

/// <summary>
/// 业务编号规则列表项 DTO。
/// </summary>
public class NumberingRuleListItemDto : BasicAppDto
{
    /// <summary>规则编码。</summary>
    public string RuleCode { get; set; } = string.Empty;

    /// <summary>规则名称。</summary>
    public string RuleName { get; set; } = string.Empty;

    /// <summary>可选前缀。</summary>
    public string? Prefix { get; set; }

    /// <summary>分隔符。</summary>
    public string Separator { get; set; } = string.Empty;

    /// <summary>日期格式。</summary>
    public NumberingDateFormat DateFormat { get; set; }

    /// <summary>流水位数。</summary>
    public int SerialLength { get; set; }

    /// <summary>重置周期。</summary>
    public NumberingResetCycle ResetCycle { get; set; }

    /// <summary>规则时区。</summary>
    public string TimeZoneId { get; set; } = string.Empty;

    /// <summary>当前周期已分配的最后流水值。</summary>
    public string CurrentValue { get; set; } = "0";

    /// <summary>当前周期键。</summary>
    public string? CurrentPeriod { get; set; }

    /// <summary>是否曾经发号。</summary>
    public bool HasAllocated { get; set; }

    /// <summary>是否允许租户使用。</summary>
    public bool AllowTenantUse { get; set; }

    /// <summary>是否为全局规则。</summary>
    public bool IsGlobal { get; set; }

    /// <summary>启停状态。</summary>
    public EnableStatus Status { get; set; }

    /// <summary>显示排序。</summary>
    public int Sort { get; set; }

    /// <summary>备注。</summary>
    public string? Remark { get; set; }
}

/// <summary>
/// 业务编号规则详情 DTO。
/// </summary>
public sealed class NumberingRuleDetailDto : NumberingRuleListItemDto
{
    /// <summary>创建时间。</summary>
    public DateTimeOffset CreatedTime { get; set; }

    /// <summary>最后修改时间。</summary>
    public DateTimeOffset? ModifiedTime { get; set; }
}

/// <summary>
/// 业务编号规则分页查询 DTO。
/// </summary>
public sealed class NumberingRulePageQueryDto : BasicAppPRDto
{
    /// <summary>查询作用域；租户用 Tenant 查看私有规则、Global 查看开放的全局规则。</summary>
    public NumberingScope Scope { get; set; } = NumberingScope.Auto;

    /// <summary>编码、名称或备注关键词。</summary>
    [StringLength(500)]
    public string? Keyword { get; set; }

    /// <summary>可选启停状态。</summary>
    public EnableStatus? Status { get; set; }
}

/// <summary>
/// 发号记录分页查询 DTO。
/// </summary>
public sealed class NumberingAllocationPageQueryDto : BasicAppPRDto
{
    /// <summary>规则主键。</summary>
    [Range(1, long.MaxValue)]
    public long RuleId { get; set; }

    /// <summary>规则作用域。</summary>
    public NumberingScope Scope { get; set; } = NumberingScope.Auto;

    /// <summary>幂等键、业务类型或业务标识关键词。</summary>
    [StringLength(500)]
    public string? Keyword { get; set; }
}

/// <summary>
/// 发号记录列表项 DTO。
/// </summary>
public sealed class NumberingAllocationListItemDto : BasicAppDto
{
    /// <summary>规则主键。</summary>
    public long RuleId { get; set; }

    /// <summary>规则编码快照。</summary>
    public string RuleCode { get; set; } = string.Empty;

    /// <summary>原始请求租户。</summary>
    public long RequestTenantId { get; set; }

    /// <summary>幂等键。</summary>
    public string IdempotencyKey { get; set; } = string.Empty;

    /// <summary>分配数量。</summary>
    public int Count { get; set; }

    /// <summary>起始流水值。</summary>
    public string StartValue { get; set; } = "0";

    /// <summary>结束流水值。</summary>
    public string EndValue { get; set; } = "0";

    /// <summary>周期键。</summary>
    public string PeriodKey { get; set; } = string.Empty;

    /// <summary>第一个完整编号。</summary>
    public string FirstNumber { get; set; } = string.Empty;

    /// <summary>最后一个完整编号。</summary>
    public string LastNumber { get; set; } = string.Empty;

    /// <summary>UTC 发号时间。</summary>
    public DateTimeOffset GeneratedAtUtc { get; set; }

    /// <summary>可选业务类型。</summary>
    public string? BusinessType { get; set; }

    /// <summary>可选业务标识。</summary>
    public string? BusinessId { get; set; }
}

/// <summary>
/// 后端运行环境支持的编号规则时区选项。
/// </summary>
public sealed class NumberingTimeZoneOptionDto
{
    /// <summary>时区标识；作为规则保存值。</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>后端运行环境提供的显示名称。</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>不考虑夏令时的 UTC 基础偏移分钟数。</summary>
    public int BaseUtcOffsetMinutes { get; set; }

    /// <summary>是否支持夏令时。</summary>
    public bool SupportsDaylightSavingTime { get; set; }
}

/// <summary>
/// 不消耗流水的格式预览 DTO。
/// </summary>
public class NumberingPreviewDto
{
    /// <summary>可选前缀。</summary>
    [StringLength(50)]
    public string? Prefix { get; set; }

    /// <summary>分隔符。</summary>
    [StringLength(10)]
    public string Separator { get; set; } = "-";

    /// <summary>日期格式。</summary>
    public NumberingDateFormat DateFormat { get; set; } = NumberingDateFormat.YyyyMMdd;

    /// <summary>流水位数。</summary>
    [Range(1, 18)]
    public int SerialLength { get; set; } = 4;

    /// <summary>重置周期。</summary>
    public NumberingResetCycle ResetCycle { get; set; } = NumberingResetCycle.Daily;

    /// <summary>规则时区。</summary>
    [Required]
    [StringLength(100)]
    public string TimeZoneId { get; set; } = "UTC";

    /// <summary>预览流水值，默认 1。</summary>
    [Required]
    [RegularExpression("^[1-9][0-9]{0,17}$")]
    public string SampleValue { get; set; } = "1";
}

/// <summary>
/// 格式预览结果 DTO。
/// </summary>
public sealed class NumberingPreviewResultDto
{
    /// <summary>预览编号。</summary>
    public string Number { get; set; } = string.Empty;

    /// <summary>当前规则本地时间。</summary>
    public DateTimeOffset RuleLocalTime { get; set; }

    /// <summary>预览周期键。</summary>
    public string PeriodKey { get; set; } = string.Empty;
}

/// <summary>
/// 不消耗流水的连续批量格式预览 DTO。
/// </summary>
/// <remarks>继承单个预览的格式参数，只额外声明连续预览数量；批量预览不会读取或推进规则当前流水。</remarks>
public sealed class NumberingBatchPreviewDto : NumberingPreviewDto
{
    /// <summary>
    /// 单次批量预览允许返回的最大编号数量。
    /// </summary>
    public const int MaximumCount = 50;

    /// <summary>
    /// 从示例流水值开始连续预览的数量，范围为 1 至 50。
    /// </summary>
    [Range(1, MaximumCount)]
    public int Count { get; set; } = 10;
}

/// <summary>
/// 连续批量格式预览结果 DTO。
/// </summary>
public sealed class NumberingBatchPreviewResultDto
{
    /// <summary>预览区间的起始流水值，使用字符串避免 JavaScript 18 位整数精度损失。</summary>
    public string StartValue { get; set; } = "0";

    /// <summary>预览区间的结束流水值，使用字符串避免 JavaScript 18 位整数精度损失。</summary>
    public string EndValue { get; set; } = "0";

    /// <summary>按流水升序生成的连续编号列表，最多包含 50 个元素。</summary>
    public IReadOnlyList<string> Numbers { get; set; } = [];

    /// <summary>执行预览时的规则时区本地时间。</summary>
    public DateTimeOffset RuleLocalTime { get; set; }

    /// <summary>执行预览时根据规则重置周期计算的周期键。</summary>
    public string PeriodKey { get; set; } = string.Empty;
}

/// <summary>
/// 单号生成 API DTO。
/// </summary>
public class NumberGenerateDto
{
    /// <summary>规则编码。</summary>
    [Required]
    [StringLength(100)]
    public string RuleCode { get; set; } = string.Empty;

    /// <summary>规则解析作用域。</summary>
    public NumberingScope Scope { get; set; } = NumberingScope.Auto;

    /// <summary>必填幂等键。</summary>
    [Required]
    [StringLength(100)]
    public string IdempotencyKey { get; set; } = string.Empty;

    /// <summary>可选业务类型。</summary>
    [StringLength(100)]
    public string? BusinessType { get; set; }

    /// <summary>可选业务标识。</summary>
    [StringLength(100)]
    public string? BusinessId { get; set; }
}

/// <summary>
/// 批量编号生成 API DTO。
/// </summary>
public sealed class NumberBatchGenerateDto : NumberGenerateDto
{
    /// <summary>生成数量，范围 1 至 1000。</summary>
    [Range(1, 1000)]
    public int Count { get; set; }
}

/// <summary>
/// 编号生成 API 结果 DTO。
/// </summary>
public sealed class NumberGenerationResultDto
{
    /// <summary>实际规则主键。</summary>
    public long RuleId { get; set; }

    /// <summary>实际规则编码。</summary>
    public string RuleCode { get; set; } = string.Empty;

    /// <summary>实际解析作用域。</summary>
    public NumberingScope ResolvedScope { get; set; }

    /// <summary>幂等键。</summary>
    public string IdempotencyKey { get; set; } = string.Empty;

    /// <summary>周期键。</summary>
    public string PeriodKey { get; set; } = string.Empty;

    /// <summary>起始流水值。</summary>
    public string StartValue { get; set; } = "0";

    /// <summary>结束流水值。</summary>
    public string EndValue { get; set; } = "0";

    /// <summary>生成编号列表。</summary>
    public IReadOnlyList<string> Numbers { get; set; } = [];

    /// <summary>首次分配 UTC 时间。</summary>
    public DateTimeOffset GeneratedAtUtc { get; set; }

    /// <summary>是否为幂等重放。</summary>
    public bool IsIdempotentReplay { get; set; }
}
