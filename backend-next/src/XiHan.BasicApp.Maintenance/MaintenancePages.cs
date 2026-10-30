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
    /// 缓存管理。
    /// </summary>
    public static PageDescriptor Caches { get; } = new("maintenance.caches", "/maintenance/caches", "maintenance.caches.title", "lucide:database", 650, MaintenancePermissions.CachesRead);

    /// <summary>
    /// 版本管理。
    /// </summary>
    public static PageDescriptor Versions { get; } = new("maintenance.versions", "/maintenance/versions", "maintenance.versions.title", "lucide:git-branch", 670, MaintenancePermissions.VersionsRead);

    /// <summary>
    /// 升级记录。
    /// </summary>
    public static PageDescriptor Migrations { get; } = new("maintenance.migrations", "/maintenance/migrations", "maintenance.migrations.title", "lucide:database-zap", 680, MaintenancePermissions.MigrationsRead);

    /// <summary>
    /// 本包全部页面。
    /// </summary>
    public static IReadOnlyList<PageDescriptor> All { get; } = [Jobs, Caches, Versions, Migrations];
}
