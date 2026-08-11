// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Printing.Application.Caching;

/// <summary>
/// 打印模块业务缓存名称常量。
/// </summary>
public static class PrintingCacheNames
{
    /// <summary>
    /// 打印模板解析缓存（请求租户 × 请求作用域 × 模板编码）。
    /// </summary>
    public const string PrintTemplate = "basicapp:printing:print:template";
}
