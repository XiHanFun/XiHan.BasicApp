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
    /// 操作 <c>jobs.reschedule</c> 的说明。
    /// </summary>
    public const string RescheduleJobDescription = "jobs.reschedule.description";

    /// <summary>
    /// 操作 <c>jobs.restore</c> 的说明。
    /// </summary>
    public const string RestoreJobsDescription = "jobs.restore.description";

    /// <summary>
    /// 操作 <c>jobs.types</c> 的说明。
    /// </summary>
    public const string ListJobTypesDescription = "jobs.types.description";

    /// <summary>
    /// 操作 <c>caches.namespaces</c> 的说明。
    /// </summary>
    public const string ListCacheNamespacesDescription = "caches.namespaces.description";

    /// <summary>
    /// 操作 <c>caches.keys</c> 的说明。
    /// </summary>
    public const string ListCacheKeysDescription = "caches.keys.description";

    /// <summary>
    /// 操作 <c>caches.inspect</c> 的说明。
    /// </summary>
    public const string InspectCacheEntryDescription = "caches.inspect.description";

    /// <summary>
    /// 操作 <c>caches.update</c> 的说明。
    /// </summary>
    public const string UpdateCacheEntryDescription = "caches.update.description";

    /// <summary>
    /// 操作 <c>caches.delete</c> 的说明。
    /// </summary>
    public const string DeleteCacheEntriesDescription = "caches.delete.description";

    /// <summary>
    /// 操作 <c>caches.delete-by-prefix</c> 的说明。
    /// </summary>
    public const string DeleteCacheByPrefixDescription = "caches.delete_by_prefix.description";

    /// <summary>
    /// 操作 <c>server.get</c> 的说明。
    /// </summary>
    public const string GetServerInfoDescription = "server.get.description";

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
    /// 作业类型没登记。
    /// </summary>
    /// <remarks>
    /// 参数 <c>id</c>、<c>type</c>。
    /// </remarks>
    public const string JobTypeUnknown = "maintenance.jobs.type_unknown";

    /// <summary>
    /// 作业有多个触发器。
    /// </summary>
    /// <remarks>
    /// 参数 <c>id</c>、<c>count</c>。
    /// </remarks>
    public const string JobTriggersMany = "maintenance.jobs.triggers_many";

    /// <summary>
    /// 不在开放的缓存命名空间里。
    /// </summary>
    /// <remarks>
    /// 参数 <c>key</c>。
    /// </remarks>
    public const string CacheNamespaceDenied = "maintenance.caches.namespace_denied";

    /// <summary>
    /// 缓存命名空间只读。
    /// </summary>
    /// <remarks>
    /// 参数 <c>prefix</c>。
    /// </remarks>
    public const string CacheReadOnly = "maintenance.caches.read_only";

    /// <summary>
    /// 缓存项不存在。
    /// </summary>
    /// <remarks>
    /// 参数 <c>key</c>。
    /// </remarks>
    public const string CacheEntryNotFound = "maintenance.caches.not_found";

    /// <summary>
    /// 缓存项的值不是文本。
    /// </summary>
    /// <remarks>
    /// 参数 <c>key</c>。
    /// </remarks>
    public const string CacheValueNotText = "maintenance.caches.not_text";

    /// <summary>
    /// 缓存存储不支持键管理。
    /// </summary>
    public const string CacheInspectorUnavailable = "maintenance.caches.inspector_unavailable";

    /// <summary>
    /// 没有装运行信息。
    /// </summary>
    public const string HostMonitorUnavailable = "maintenance.server.monitor_unavailable";

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
