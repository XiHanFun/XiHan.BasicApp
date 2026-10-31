// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Workbench;

/// <summary>
/// 工作台模块的错误码。
/// </summary>
public static class WorkbenchErrors
{
    /// <summary>
    /// 没有这个小组件。
    /// </summary>
    /// <remarks>
    /// 参数 <c>key</c>。
    /// </remarks>
    public const string WidgetNotFound = "WORKBENCH_NOTFOUND_001";

    /// <summary>
    /// 没有看这个小组件的权限。
    /// </summary>
    /// <remarks>
    /// 参数 <c>key</c>。
    /// </remarks>
    public const string WidgetDenied = "WORKBENCH_DENIED_001";
}
