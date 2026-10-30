// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Pages;

namespace XiHan.BasicApp.Maintenance;

/// <summary>
/// 运维包登记的页面。
/// </summary>
public static class MaintenancePages
{
    /// <summary>
    /// 任务调度。
    /// </summary>
    public static PageDescriptor Jobs { get; } = new("maintenance.jobs", "/maintenance/jobs", "maintenance.jobs.title", "lucide:timer", 640, MaintenancePermissions.JobsRead);

    /// <summary>
    /// 本包全部页面。
    /// </summary>
    public static IReadOnlyList<PageDescriptor> All { get; } = [Jobs];
}
