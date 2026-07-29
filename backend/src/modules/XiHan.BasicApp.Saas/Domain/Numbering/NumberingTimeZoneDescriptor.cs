// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Saas.Domain.Numbering;

/// <summary>
/// 编号规则可用时区描述；数据来自后端运行环境的 <see cref="TimeZoneInfo" /> 目录，并归一为可跨平台保存的标识。
/// </summary>
/// <param name="Id">可提交给编号规则的 IANA 时区标识；UTC 保留为通用标识。</param>
/// <param name="DisplayName">后端运行环境提供的时区显示名称。</param>
/// <param name="BaseUtcOffsetMinutes">不考虑夏令时的 UTC 基础偏移分钟数。</param>
/// <param name="SupportsDaylightSavingTime">是否支持夏令时。</param>
public sealed record NumberingTimeZoneDescriptor(
    string Id,
    string DisplayName,
    int BaseUtcOffsetMinutes,
    bool SupportsDaylightSavingTime);
