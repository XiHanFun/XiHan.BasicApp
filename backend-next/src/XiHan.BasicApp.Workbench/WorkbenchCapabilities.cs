// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts;

namespace XiHan.BasicApp.Workbench;

/// <summary>
/// 工作台模块提供的能力。
/// </summary>
public static class WorkbenchCapabilities
{
    /// <summary>
    /// 工作台能力：登录后的落点页面与各包小组件的汇总。
    /// </summary>
    public const string Workbench = AppInfo.CapabilityPrefix + "workbench";

    /// <summary>
    /// 能力版本。
    /// </summary>
    public const int Version = 1;
}
