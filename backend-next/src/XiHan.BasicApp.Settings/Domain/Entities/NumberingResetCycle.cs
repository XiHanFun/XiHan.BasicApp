// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Settings.Domain.Entities;

/// <summary>
/// 流水的自动重置周期（按规则时区）：新周期从 1 开始；日期段必须能区分周期边界，否则不同周期会出同号。
/// </summary>
public enum NumberingResetCycle
{
    /// <summary>
    /// 永不重置。
    /// </summary>
    Never = 0,

    /// <summary>
    /// 每天。
    /// </summary>
    Daily = 1,

    /// <summary>
    /// 每月。
    /// </summary>
    Monthly = 2,

    /// <summary>
    /// 每年。
    /// </summary>
    Yearly = 3,
}
