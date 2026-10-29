// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Settings.Domain.Entities;

/// <summary>
/// 参数、字典、字典项与编号规则的启停状态。
/// </summary>
/// <remarks>
/// 停用的参数视为不存在，停用的字典不再给出选项，停用的规则不再发号。
/// </remarks>
public enum SettingStatus
{
    /// <summary>
    /// 启用。
    /// </summary>
    Active = 0,

    /// <summary>
    /// 停用。
    /// </summary>
    Disabled = 1,
}
