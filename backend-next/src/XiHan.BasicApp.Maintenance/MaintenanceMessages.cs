// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.Framework.Localization.Abstractions.Messages;

namespace XiHan.BasicApp.Maintenance;

/// <summary>
/// 运维包的消息键。
/// </summary>
public static class MaintenanceMessages
{
    /// <summary>
    /// 消息目录名。
    /// </summary>
    public const string CatalogName = "basicapp.maintenance";

    /// <summary>
    /// 操作 <c>jobs.list</c> 的说明。
    /// </summary>
    public const string ListJobsDescription = "jobs.list.description";

    /// <summary>
    /// 操作 <c>jobs.get</c> 的说明。
    /// </summary>
    public const string GetJobDescription = "jobs.get.description";

    /// <summary>
    /// 操作 <c>jobs.pause</c> 的说明。
    /// </summary>
    public const string PauseJobsDescription = "jobs.pause.description";

    /// <summary>
    /// 操作 <c>jobs.resume</c> 的说明。
    /// </summary>
    public const string ResumeJobsDescription = "jobs.resume.description";

    /// <summary>
    /// 操作 <c>jobs.trigger</c> 的说明。
    /// </summary>
    public const string TriggerJobDescription = "jobs.trigger.description";

    /// <summary>
    /// 操作 <c>jobs.validate-schedule</c> 的说明。
    /// </summary>
    public const string ValidateScheduleDescription = "jobs.validate_schedule.description";

    /// <summary>
    /// 操作 <c>job-runs.list</c> 的说明。
    /// </summary>
    public const string ListJobRunsDescription = "job_runs.list.description";

    /// <summary>
    /// 操作 <c>versions.list</c> 的说明。
    /// </summary>
    public const string ListVersionsDescription = "versions.list.description";

    /// <summary>
    /// 操作 <c>versions.get</c> 的说明。
    /// </summary>
    public const string GetVersionDescription = "versions.get.description";

    /// <summary>
    /// 操作 <c>migrations.list</c> 的说明。
    /// </summary>
    public const string ListMigrationsDescription = "migrations.list.description";

    /// <summary>
    /// 操作 <c>migrations.get</c> 的说明。
    /// </summary>
    public const string GetMigrationDescription = "migrations.get.description";

    /// <summary>
    /// 只能在平台一侧调用。
    /// </summary>
    public const string PlatformOnly = "maintenance.platform_only";

    /// <summary>
    /// 筛选字段或运算符不支持。
    /// </summary>
    /// <remarks>
    /// 参数 <c>field</c>、<c>operator</c>。
    /// </remarks>
    public const string FilterUnsupported = "maintenance.query.filter_unsupported";

    /// <summary>
    /// 排序字段不支持。
    /// </summary>
    /// <remarks>
    /// 参数 <c>field</c>。
    /// </remarks>
    public const string SortUnsupported = "maintenance.query.sort_unsupported";

    /// <summary>
    /// 计划写得不对。
    /// </summary>
    /// <remarks>
    /// 参数 <c>expression</c>。
    /// </remarks>
    public const string ScheduleInvalid = "maintenance.jobs.schedule_invalid";

    /// <summary>
    /// 作业不存在。
    /// </summary>
    /// <remarks>
    /// 参数 <c>name</c>。
    /// </remarks>
    public const string JobNotFound = "maintenance.jobs.not_found";

    /// <summary>
    /// 版本台账或执行记录不存在。
    /// </summary>
    /// <remarks>
    /// 参数 <c>id</c>。
    /// </remarks>
    public const string SchemaRecordNotFound = "maintenance.schema.not_found";

    private static readonly Lazy<MessageCatalog> _catalog = new(static () => MessageCatalog.FromEmbeddedResources(CatalogName, typeof(MaintenanceMessages)));

    /// <summary>
    /// 本包的消息目录（嵌入的 10 种区域文件）。
    /// </summary>
    public static MessageCatalog Catalog => _catalog.Value;
}
