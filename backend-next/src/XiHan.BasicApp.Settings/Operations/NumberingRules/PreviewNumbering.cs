// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Globalization;
using XiHan.BasicApp.Settings.Domain.Services;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Settings.Operations.NumberingRules;

/// <summary>
/// 预览号码。
/// </summary>
/// <remarks>
/// <c>GET /api/numbering-rules:preview</c>（声明只读，ADR-0041）。按给定格式在当前时刻拼出号码，不读库、不发号，最多 50 个；
/// 格式组合不安全 <c>SETTINGS_VALIDATION_005</c>，时区无效 <c>SETTINGS_VALIDATION_006</c>，区间越出位数 <c>SETTINGS_VALIDATION_007</c>。
/// </remarks>
/// <param name="clock">时间源。</param>
[Operation(Id, Stability = Stability.Preview, ReadOnly = true, Expose = Expose.Http, Permission = SettingsPermissions.NumberingRulesRead)]
public sealed class PreviewNumbering(TimeProvider clock) : IOperation<PreviewNumberingRequest, NumberingPreviewResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "numbering-rules.preview";

    /// <inheritdoc />
    public ValueTask<Result<NumberingPreviewResponse>> HandleAsync(PreviewNumberingRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        return ValueTask.FromResult(Preview(request));
    }

    private Result<NumberingPreviewResponse> Preview(PreviewNumberingRequest request)
    {
        var timeZoneId = request.TimeZoneId.Trim();
        var valid = NumberingFormatter.Validate(request.DateFormat, request.ResetCycle, timeZoneId);
        if (valid.IsFailure)
        {
            return valid.Error!;
        }

        var start = long.Parse(request.StartValue, NumberStyles.None, CultureInfo.InvariantCulture);
        var end = start + request.Count - 1;
        var capacity = NumberingRuleService.CheckCapacity(end, request.SerialLength);
        if (capacity.IsFailure)
        {
            return capacity.Error!;
        }

        NumberingFormatter.TryResolveTimeZone(timeZoneId, out var timeZone);
        var local = NumberingFormatter.ToRuleTime(clock.GetUtcNow(), timeZone!);
        var dateText = NumberingFormatter.DateText(local, request.DateFormat);
        var prefix = string.IsNullOrEmpty(request.Prefix) ? null : request.Prefix;
        var numbers = NumberingFormatter.FormatRange(prefix, request.Separator ?? string.Empty, dateText, request.SerialLength, start, end);
        return new NumberingPreviewResponse(numbers, NumberingFormatter.PeriodKey(local, request.ResetCycle), dateText);
    }
}
