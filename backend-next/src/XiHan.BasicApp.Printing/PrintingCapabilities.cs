// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts;

namespace XiHan.BasicApp.Printing;

/// <summary>
/// 打印模块提供的能力。
/// </summary>
public static class PrintingCapabilities
{
    /// <summary>
    /// 打印能力：模板管理、按编码解析与数据源目录。
    /// </summary>
    public const string Printing = AppInfo.CapabilityPrefix + "printing";

    /// <summary>
    /// 能力版本。
    /// </summary>
    public const int Version = 1;
}
