// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Security;

namespace XiHan.BasicApp.Maintenance;

/// <summary>
/// 运维包的权限码。
/// </summary>
/// <remarks>
/// 作业与运行历史是全局的（框架的调度表不分租户），只在平台一侧。
/// </remarks>
public static class MaintenancePermissions
{
    /// <summary>
    /// 查看作业。
    /// </summary>
    public const string JobsRead = "maintenance.jobs.read";

    /// <summary>
    /// 暂停与恢复作业。
    /// </summary>
    public const string JobsUpdate = "maintenance.jobs.update";

    /// <summary>
    /// 立即执行作业。
    /// </summary>
    public const string JobsTrigger = "maintenance.jobs.trigger";

    /// <summary>
    /// 查看作业的运行历史。
    /// </summary>
    public const string JobRunsRead = "maintenance.job-runs.read";

    /// <summary>
    /// 本包全部权限码。
    /// </summary>
    public static IReadOnlyList<PermissionDescriptor> All { get; } =
    [
        new(JobsRead, "Read jobs", PermissionScope.Platform, description: "View the scheduled jobs, their schedules, state and last outcome."),
        new(JobsUpdate, "Pause and resume jobs", PermissionScope.Platform, audited: true, description: "Pause and resume scheduled jobs."),
        new(JobsTrigger, "Run jobs now", PermissionScope.Platform, audited: true, description: "Run a scheduled job now on any node."),
        new(JobRunsRead, "Read job runs", PermissionScope.Platform, description: "View the run history of scheduled jobs."),
    ];
}
