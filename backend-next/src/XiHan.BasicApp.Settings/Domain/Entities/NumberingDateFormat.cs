// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Settings.Domain.Entities;

/// <summary>
/// 编号日期段的格式（按规则时区取当地日期）。
/// </summary>
public enum NumberingDateFormat
{
    /// <summary>
    /// 不输出日期段。
    /// </summary>
    None = 0,

    /// <summary>
    /// 四位年份，如 2026。
    /// </summary>
    Yyyy = 1,

    /// <summary>
    /// 四位年份与月份，如 202610。
    /// </summary>
    YyyyMM = 2,

    /// <summary>
    /// 四位年份、月份与日期，如 20261028。
    /// </summary>
    YyyyMMdd = 3,

    /// <summary>
    /// 两位年份与月份，如 2610。
    /// </summary>
    YyMM = 4,

    /// <summary>
    /// 两位年份、月份与日期，如 261028。
    /// </summary>
    YyMMdd = 5,

    /// <summary>
    /// 月份与日期，如 1028。
    /// </summary>
    MMdd = 6,
}
