// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using XiHan.BasicApp.Contracts.Paging;
using XiHan.BasicApp.Contracts.Settings;
using XiHan.BasicApp.Settings.Domain.Entities;
using XiHan.BasicApp.Settings.Domain.Services;

namespace XiHan.BasicApp.Settings.Operations.NumberingRules;

/// <summary>
/// 编号规则的网络契约。
/// </summary>
/// <param name="Id">主键。</param>
/// <param name="OwnerTenantId">属主：<c>platform</c> 或租户 ID。</param>
/// <param name="IsPlatform">是否为平台规则。</param>
/// <param name="Code">编码。</param>
/// <param name="Name">名称。</param>
/// <param name="Prefix">前缀。</param>
/// <param name="Separator">分隔符。</param>
/// <param name="DateFormat">日期格式。</param>
/// <param name="SerialLength">流水位数。</param>
/// <param name="ResetCycle">重置周期。</param>
/// <param name="TimeZoneId">时区。</param>
/// <param name="Template">格式示意（如 <c>ORD-yyyyMMdd-0000</c>）。</param>
/// <param name="CurrentValue">当前周期最后发出的流水值。</param>
/// <param name="CurrentPeriod">当前周期键（还没有基线时为空串）。</param>
/// <param name="HasAllocated">是否发过号（发过即格式冻结）。</param>
/// <param name="AllowTenantUse">平台规则是否供租户使用。</param>
/// <param name="Status">状态。</param>
/// <param name="Order">排序号。</param>
/// <param name="Description">说明。</param>
/// <param name="CreatedAt">创建时间（UTC）。</param>
/// <param name="ModifiedAt">修改时间（UTC）。</param>
public sealed record NumberingRuleResponse(
    Guid Id,
    string OwnerTenantId,
    bool IsPlatform,
    string Code,
    string Name,
    string? Prefix,
    string Separator,
    NumberingDateFormat DateFormat,
    int SerialLength,
    NumberingResetCycle ResetCycle,
    string TimeZoneId,
    string Template,
    long CurrentValue,
    string CurrentPeriod,
    bool HasAllocated,
    bool AllowTenantUse,
    SettingStatus Status,
    int Order,
    string? Description,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ModifiedAt)
{
    /// <summary>
    /// 从实体投影。
    /// </summary>
    public static NumberingRuleResponse From(SysNumberingRule rule)
    {
        ArgumentNullException.ThrowIfNull(rule);
        return new NumberingRuleResponse(
            rule.Id,
            rule.OwnerTenantId,
            SettingsScope.IsPlatform(rule.OwnerTenantId),
            rule.Code,
            rule.Name,
            rule.Prefix,
            rule.Separator,
            rule.DateFormat,
            rule.SerialLength,
            rule.ResetCycle,
            rule.TimeZoneId,
            NumberingFormatter.Template(rule),
            rule.CurrentValue,
            rule.CurrentPeriod,
            rule.HasAllocated,
            rule.AllowTenantUse,
            rule.Status,
            rule.Order,
            rule.Description,
            rule.CreatedAt,
            rule.ModifiedAt);
    }
}

/// <summary>
/// 编号规则的文法约束。
/// </summary>
public static class NumberingPatterns
{
    /// <summary>
    /// 规则编码：字母开头，只含字母、数字、<c>.</c>、<c>_</c>、<c>-</c>，最长 100。
    /// </summary>
    public const string Code = "^[A-Za-z][A-Za-z0-9._-]{0,99}$";

    /// <summary>
    /// 前缀与分隔符：不含空白。
    /// </summary>
    public const string NoWhitespace = "^\\S*$";

    /// <summary>
    /// 流水值（字符串传，避免前端大数精度丢失）：1 到 18 位正整数。
    /// </summary>
    public const string SequenceValue = "^[1-9][0-9]{0,17}$";
}

/// <summary>
/// <c>numbering-rules.create</c> 的请求。
/// </summary>
/// <param name="Code">编码（本侧唯一）。</param>
/// <param name="Name">名称。</param>
/// <param name="Prefix">前缀。</param>
/// <param name="Separator">分隔符（可为空串）。</param>
/// <param name="DateFormat">日期格式。</param>
/// <param name="SerialLength">流水位数。</param>
/// <param name="ResetCycle">重置周期。</param>
/// <param name="TimeZoneId">时区（IANA 标识）。</param>
/// <param name="AllowTenantUse">平台规则是否供租户使用（租户规则忽略）。</param>
/// <param name="Order">排序号。</param>
/// <param name="Description">说明。</param>
public sealed record CreateNumberingRuleRequest(
    [Required, StringLength(100, MinimumLength = 1), RegularExpression(NumberingPatterns.Code)] string Code,
    [Required, StringLength(100, MinimumLength = 1)] string Name,
    [StringLength(50), RegularExpression(NumberingPatterns.NoWhitespace)] string? Prefix = null,
    [StringLength(10), RegularExpression(NumberingPatterns.NoWhitespace)] string? Separator = "-",
    NumberingDateFormat DateFormat = NumberingDateFormat.YyyyMMdd,
    [Range(1, NumberingFormatter.MaxSerialLength)] int SerialLength = 4,
    NumberingResetCycle ResetCycle = NumberingResetCycle.Daily,
    [StringLength(100, MinimumLength = 1)] string TimeZoneId = "UTC",
    bool AllowTenantUse = false,
    [Range(0, 100_000)] int Order = 0,
    [StringLength(500)] string? Description = null);

/// <summary>
/// <c>numbering-rules.get</c> 的请求。
/// </summary>
/// <param name="Id">规则主键。</param>
public sealed record GetNumberingRuleRequest(Guid Id);

/// <summary>
/// <c>numbering-rules.update</c> 的请求：编码不在这里改；发过号后格式字段必须与原值相同。
/// </summary>
/// <param name="Id">规则主键。</param>
/// <param name="Name">名称。</param>
/// <param name="Prefix">前缀。</param>
/// <param name="Separator">分隔符。</param>
/// <param name="DateFormat">日期格式。</param>
/// <param name="SerialLength">流水位数。</param>
/// <param name="ResetCycle">重置周期。</param>
/// <param name="TimeZoneId">时区。</param>
/// <param name="AllowTenantUse">平台规则是否供租户使用。</param>
/// <param name="Order">排序号。</param>
/// <param name="Description">说明。</param>
/// <param name="Status">状态。</param>
public sealed record UpdateNumberingRuleRequest(
    Guid Id,
    [Required, StringLength(100, MinimumLength = 1)] string Name,
    [StringLength(50), RegularExpression(NumberingPatterns.NoWhitespace)] string? Prefix = null,
    [StringLength(10), RegularExpression(NumberingPatterns.NoWhitespace)] string? Separator = "-",
    NumberingDateFormat DateFormat = NumberingDateFormat.YyyyMMdd,
    [Range(1, NumberingFormatter.MaxSerialLength)] int SerialLength = 4,
    NumberingResetCycle ResetCycle = NumberingResetCycle.Daily,
    [StringLength(100, MinimumLength = 1)] string TimeZoneId = "UTC",
    bool AllowTenantUse = false,
    [Range(0, 100_000)] int Order = 0,
    [StringLength(500)] string? Description = null,
    SettingStatus Status = SettingStatus.Active);

/// <summary>
/// <c>numbering-rules.delete</c> 的请求：集合入参。
/// </summary>
/// <param name="Ids">规则主键，1 到 100 个。</param>
public sealed record NumberingRuleIdsRequest([Required, MinLength(1), MaxLength(100)] IReadOnlyList<Guid> Ids);

/// <summary>
/// <c>numbering-rules.reset</c> 的请求。
/// </summary>
/// <param name="Id">规则主键。</param>
/// <param name="NextValue">下一个要发出的流水值（字符串）。</param>
/// <param name="Reason">理由（进操作审计）。</param>
/// <param name="ConfirmCode">平台规则要回填的规则编码。</param>
public sealed record ResetNumberingRuleRequest(
    Guid Id,
    [Required, RegularExpression(NumberingPatterns.SequenceValue)] string NextValue,
    [Required, StringLength(500, MinimumLength = 1)] string Reason,
    [StringLength(100)] string? ConfirmCode = null);

/// <summary>
/// <c>numbering-rules.preview</c> 的请求：按给定格式在当前时刻预览，不发号。
/// </summary>
/// <param name="Prefix">前缀。</param>
/// <param name="Separator">分隔符。</param>
/// <param name="DateFormat">日期格式。</param>
/// <param name="SerialLength">流水位数。</param>
/// <param name="ResetCycle">重置周期。</param>
/// <param name="TimeZoneId">时区。</param>
/// <param name="StartValue">起始流水值（字符串）。</param>
/// <param name="Count">个数（1 到 50）。</param>
public sealed record PreviewNumberingRequest(
    [StringLength(50), RegularExpression(NumberingPatterns.NoWhitespace)] string? Prefix = null,
    [StringLength(10), RegularExpression(NumberingPatterns.NoWhitespace)] string? Separator = "-",
    NumberingDateFormat DateFormat = NumberingDateFormat.YyyyMMdd,
    [Range(1, NumberingFormatter.MaxSerialLength)] int SerialLength = 4,
    NumberingResetCycle ResetCycle = NumberingResetCycle.Daily,
    [StringLength(100, MinimumLength = 1)] string TimeZoneId = "UTC",
    [RegularExpression(NumberingPatterns.SequenceValue)] string StartValue = "1",
    [Range(1, NumberingFormatter.MaxPreview)] int Count = 1);

/// <summary>
/// <c>numbering-rules.preview</c> 的响应。
/// </summary>
/// <param name="Numbers">号码。</param>
/// <param name="PeriodKey">当前时刻的周期键。</param>
/// <param name="DateText">当前时刻的日期段。</param>
public sealed record NumberingPreviewResponse(IReadOnlyList<string> Numbers, string PeriodKey, string? DateText);

/// <summary>
/// <c>numbering-rules.generate</c> 的请求。
/// </summary>
/// <param name="Code">规则编码。</param>
/// <param name="IdempotencyKey">幂等键（同一侧同一规则只发一次）。</param>
/// <param name="Count">个数（1 到 1000）。</param>
/// <param name="Scope">找规则的范围。</param>
/// <param name="BusinessType">业务类型。</param>
/// <param name="BusinessId">业务单据主键。</param>
public sealed record GenerateNumbersRequest(
    [Required, StringLength(100, MinimumLength = 1), RegularExpression(NumberingPatterns.Code)] string Code,
    [Required, StringLength(100, MinimumLength = 1)] string IdempotencyKey,
    [Range(1, NumberingFormatter.MaxBatch)] int Count = 1,
    NumberingScope Scope = NumberingScope.Auto,
    [StringLength(100)] string? BusinessType = null,
    [StringLength(100)] string? BusinessId = null);

/// <summary>
/// <c>numbering-rules.allocations</c> 的请求。
/// </summary>
/// <param name="Id">规则主键。</param>
/// <param name="Page">分页。</param>
/// <param name="Keyword">关键字（匹配幂等键、业务类型与业务单据）。</param>
public sealed record NumberingAllocationsRequest(Guid Id, PageInfo? Page = null, [StringLength(100)] string? Keyword = null);

/// <summary>
/// 一条发号记录。
/// </summary>
/// <param name="Id">主键。</param>
/// <param name="RequestTenantId">发起调用的一侧。</param>
/// <param name="IdempotencyKey">幂等键。</param>
/// <param name="Count">个数。</param>
/// <param name="StartValue">起始流水值。</param>
/// <param name="EndValue">结束流水值。</param>
/// <param name="PeriodKey">周期键。</param>
/// <param name="FirstNumber">第一个号码。</param>
/// <param name="LastNumber">最后一个号码。</param>
/// <param name="BusinessType">业务类型。</param>
/// <param name="BusinessId">业务单据主键。</param>
/// <param name="GeneratedAt">发号时刻（UTC）。</param>
public sealed record NumberingAllocationResponse(
    Guid Id,
    string RequestTenantId,
    string IdempotencyKey,
    int Count,
    long StartValue,
    long EndValue,
    string PeriodKey,
    string FirstNumber,
    string LastNumber,
    string? BusinessType,
    string? BusinessId,
    DateTimeOffset GeneratedAt)
{
    /// <summary>
    /// 从发号记录投影（首末号码按格式快照拼出）。
    /// </summary>
    public static NumberingAllocationResponse From(SysNumberingAllocation record)
    {
        ArgumentNullException.ThrowIfNull(record);
        var first = NumberingFormatter.Format(record.PrefixSnapshot, record.SeparatorSnapshot, record.DateTextSnapshot, record.SerialLengthSnapshot, record.StartValue);
        var last = NumberingFormatter.Format(record.PrefixSnapshot, record.SeparatorSnapshot, record.DateTextSnapshot, record.SerialLengthSnapshot, record.EndValue);
        return new NumberingAllocationResponse(record.Id, record.RequestTenantId, record.IdempotencyKey, record.Count, record.StartValue, record.EndValue, record.PeriodKey, first, last, record.BusinessType, record.BusinessId, record.GeneratedAt);
    }
}
