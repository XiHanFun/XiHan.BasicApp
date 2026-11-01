// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Printing.Domain.Entities;

/// <summary>
/// 打印模板的作用域。
/// </summary>
/// <remarks>
/// 解析与列表按它取模板（照 4.x）：平台一侧只有全局模板，<see cref="Auto"/> 与 <see cref="Global"/> 相同、不要求开放，用 <see cref="Tenant"/> 即校验失败；
/// 租户一侧 <see cref="Auto"/> 先取本租户启用的模板、没有再取启用且开放的全局模板，<see cref="Tenant"/> 与 <see cref="Global"/> 只取各自那一侧、不回退。
/// </remarks>
public enum PrintTemplateScope
{
    /// <summary>
    /// 自动：租户优先、回退到开放的全局模板。
    /// </summary>
    Auto = 0,

    /// <summary>
    /// 只用本租户的模板。
    /// </summary>
    Tenant = 1,

    /// <summary>
    /// 只用平台的全局模板。
    /// </summary>
    Global = 2,
}
