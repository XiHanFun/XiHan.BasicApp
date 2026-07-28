// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Saas.Domain.Enums;

/// <summary>
/// 编号规则解析作用域。
/// </summary>
public enum NumberingScope
{
    /// <summary>
    /// 自动解析：租户上下文优先租户私有规则，再回退到允许租户使用的全局规则；平台上下文直接使用全局规则。
    /// </summary>
    Auto = 0,

    /// <summary>
    /// 仅解析当前租户私有规则。
    /// </summary>
    Tenant = 1,

    /// <summary>
    /// 仅解析平台全局规则。
    /// </summary>
    Global = 2
}

/// <summary>
/// 编号流水重置周期。
/// </summary>
public enum NumberingResetCycle
{
    /// <summary>
    /// 永不自动重置。
    /// </summary>
    Never = 0,

    /// <summary>
    /// 按规则时区每天重置。
    /// </summary>
    Daily = 1,

    /// <summary>
    /// 按规则时区每月重置。
    /// </summary>
    Monthly = 2,

    /// <summary>
    /// 按规则时区每年重置。
    /// </summary>
    Yearly = 3
}

/// <summary>
/// 编号日期段格式白名单。
/// </summary>
public enum NumberingDateFormat
{
    /// <summary>
    /// 不输出日期段。
    /// </summary>
    None = 0,

    /// <summary>
    /// 四位年份，例如 2026。
    /// </summary>
    Yyyy = 1,

    /// <summary>
    /// 四位年份和月份，例如 202607。
    /// </summary>
    YyyyMM = 2,

    /// <summary>
    /// 四位年份、月份和日期，例如 20260727。
    /// </summary>
    YyyyMMdd = 3,

    /// <summary>
    /// 两位年份和月份，例如 2607。
    /// </summary>
    YyMM = 4,

    /// <summary>
    /// 两位年份、月份和日期，例如 260727。
    /// </summary>
    YyMMdd = 5,

    /// <summary>
    /// 月份和日期，例如 0727。
    /// </summary>
    MMdd = 6
}
