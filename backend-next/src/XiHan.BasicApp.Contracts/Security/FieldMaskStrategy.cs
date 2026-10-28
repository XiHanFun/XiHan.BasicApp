// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Contracts.Security;

/// <summary>
/// 字段的读取方式（沿用 4.x 的六种与取值）；按泄露的信息量从多到少是 None、PartialMask、Hash、FullMask、Redact、Hidden，多条规则命中同一字段取最严的一种。
/// </summary>
public enum FieldMaskStrategy
{
    /// <summary>
    /// 明文：原值返回，只能与只读搭配。
    /// </summary>
    None = 0,

    /// <summary>
    /// 隐藏：返回空值（只用于响应里可空的字段）。
    /// </summary>
    Hidden = 1,

    /// <summary>
    /// 全部星号：按原长度替换为 <c>*</c>。
    /// </summary>
    FullMask = 2,

    /// <summary>
    /// 部分脱敏：保留前几位、后几位，中间替换为 <c>*</c>；保留位数不小于原长时整体打码。
    /// </summary>
    PartialMask = 3,

    /// <summary>
    /// 哈希：SHA-256 前 16 位小写十六进制，可比较是否相同但看不到原值。
    /// </summary>
    Hash = 4,

    /// <summary>
    /// 固定文本：有值时一律显示规则里的文字。
    /// </summary>
    Redact = 5,
}
