// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Contracts.Settings;

/// <summary>
/// 发号时找规则的范围。
/// </summary>
public enum NumberingScope
{
    /// <summary>
    /// 租户里先找本租户的规则，没有再找供租户使用的平台规则；平台上只找平台规则。
    /// </summary>
    Auto = 0,

    /// <summary>
    /// 只找本租户的规则（平台上下文不能用）。
    /// </summary>
    Tenant = 1,

    /// <summary>
    /// 只找平台规则（租户只能用供租户使用的）。
    /// </summary>
    Platform = 2,
}
