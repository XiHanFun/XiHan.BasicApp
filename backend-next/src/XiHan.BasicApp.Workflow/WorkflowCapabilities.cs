// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts;

namespace XiHan.BasicApp.Workflow;

/// <summary>
/// 工作流包提供的能力
/// </summary>
public static class WorkflowCapabilities
{
    /// <summary>
    /// 工作流的管理与审批
    /// </summary>
    public const string Workflow = AppInfo.CapabilityPrefix + "workflow";

    /// <summary>
    /// 能力版本
    /// </summary>
    public const int Version = 1;
}
