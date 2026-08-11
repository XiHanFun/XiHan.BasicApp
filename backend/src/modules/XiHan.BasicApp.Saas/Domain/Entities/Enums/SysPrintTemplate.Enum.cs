// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Saas.Domain.Enums;

/// <summary>
/// 打印模板解析作用域。
/// </summary>
public enum PrintTemplateScope
{
    /// <summary>
    /// 自动解析：租户优先使用私有模板，不可用时回退到已开放的全局模板；平台上下文只使用全局模板。
    /// </summary>
    Auto = 0,

    /// <summary>
    /// 仅使用当前租户私有模板，不跨作用域回退。
    /// </summary>
    Tenant = 1,

    /// <summary>
    /// 仅使用平台全局模板，不跨作用域回退。
    /// </summary>
    Global = 2
}
