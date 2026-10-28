// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Settings.Domain.Entities;

/// <summary>
/// 参数值的数据类型：保存时按它校验值与缺省值。
/// </summary>
public enum ConfigDataType
{
    /// <summary>
    /// 任意文本。
    /// </summary>
    Text = 0,

    /// <summary>
    /// 十进制数（不变区域性格式）。
    /// </summary>
    Number = 1,

    /// <summary>
    /// <c>true</c> 或 <c>false</c>。
    /// </summary>
    Boolean = 2,

    /// <summary>
    /// JSON 对象。
    /// </summary>
    Json = 3,

    /// <summary>
    /// JSON 数组。
    /// </summary>
    Array = 4,
}
