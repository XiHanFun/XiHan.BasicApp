// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Identity.Domain.Entities;

/// <summary>
/// 性别（个人资料，4.x 同）。JSON 上是字符串枚举。
/// </summary>
public enum UserGender
{
    /// <summary>
    /// 未设置。
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// 男。
    /// </summary>
    Male = 1,

    /// <summary>
    /// 女。
    /// </summary>
    Female = 2,
}
