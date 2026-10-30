// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Security;

namespace XiHan.BasicApp.Maintenance;

/// <summary>
/// 运维包的权限码。
/// </summary>
/// <remarks>
/// 作业与运行历史是全局的（框架的调度表不分租户），版本台账在平台库，都只在平台一侧。
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
    /// 改作业计划与恢复代码定义。
    /// </summary>
    public const string JobsReschedule = "maintenance.jobs.reschedule";

    /// <summary>
    /// 查看缓存。
    /// </summary>
    public const string CachesRead = "maintenance.caches.read";

    /// <summary>
    /// 改写缓存值。
    /// </summary>
    public const string CachesUpdate = "maintenance.caches.update";

    /// <summary>
    /// 删除缓存。
    /// </summary>
    public const string CachesDelete = "maintenance.caches.delete";

    /// <summary>
    /// 查看服务监控。
    /// </summary>
    public const string ServerRead = "maintenance.server.read";

    /// <summary>
    /// 查看作业的运行历史。
    /// </summary>
    public const string JobRunsRead = "maintenance.job-runs.read";

    /// <summary>
    /// 查看版本台账。
    /// </summary>
    public const string VersionsRead = "maintenance.versions.read";

    /// <summary>
    /// 查看升级记录。
    /// </summary>
    public const string MigrationsRead = "maintenance.migrations.read";

    /// <summary>
    /// 本包全部权限码。
    /// </summary>
    public static IReadOnlyList<PermissionDescriptor> All { get; } =
    [
        new(JobsRead, "Read jobs", PermissionScope.Platform, description: "View the scheduled jobs, their schedules, state and last outcome."),
        new(JobsUpdate, "Pause and resume jobs", PermissionScope.Platform, audited: true, description: "Pause and resume scheduled jobs."),
        new(JobsTrigger, "Run jobs now", PermissionScope.Platform, audited: true, description: "Run a scheduled job now on any node."),
        new(JobsReschedule, "Reschedule jobs", PermissionScope.Platform, audited: true, description: "Change a job's schedule or restore a code job to its code definition."),
        new(CachesRead, "Read caches", PermissionScope.Platform, description: "Browse the cache keys of the application namespaces, their size, expiry and text value."),
        new(CachesUpdate, "Update caches", PermissionScope.Platform, audited: true, description: "Rewrite the value of a cache entry in an editable namespace, keeping its expiry."),
        new(CachesDelete, "Delete caches", PermissionScope.Platform, audited: true, description: "Delete cache entries by key or by prefix within the application namespaces."),
        new(ServerRead, "Read server monitor", PermissionScope.Platform, description: "View the host runtime, processor, memory, disks, network, GPUs and board, and the health checks of the providers."),
        new(JobRunsRead, "Read job runs", PermissionScope.Platform, description: "View the run history of scheduled jobs."),
        new(VersionsRead, "Read versions", PermissionScope.Platform, description: "View the database version ledger."),
        new(MigrationsRead, "Read upgrade records", PermissionScope.Platform, description: "View the upgrade script records."),
    ];
}
