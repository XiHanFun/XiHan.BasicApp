// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Maintenance;

/// <summary>
/// 运维包的错误码。
/// </summary>
public static class MaintenanceErrors
{
    /// <summary>
    /// 只能在平台一侧调用。
    /// </summary>
    public const string PlatformOnly = "MAINTENANCE_DENIED_001";

    /// <summary>
    /// 查询的筛选或排序字段不支持。
    /// </summary>
    /// <remarks>
    /// 参数 <c>field</c>，筛选时另有 <c>operator</c>。
    /// </remarks>
    public const string QueryUnsupported = "MAINTENANCE_VALIDATION_001";

    /// <summary>
    /// 计划写得不对。
    /// </summary>
    /// <remarks>
    /// 参数 <c>expression</c>。
    /// </remarks>
    public const string ScheduleInvalid = "MAINTENANCE_VALIDATION_002";

    /// <summary>
    /// 作业不存在。
    /// </summary>
    /// <remarks>
    /// 参数 <c>name</c>。
    /// </remarks>
    public const string JobNotFound = "MAINTENANCE_NOTFOUND_001";

    /// <summary>
    /// 版本台账或执行记录不存在。
    /// </summary>
    /// <remarks>
    /// 参数 <c>id</c>。
    /// </remarks>
    public const string SchemaRecordNotFound = "MAINTENANCE_NOTFOUND_002";

    /// <summary>
    /// 作业的类型不在作业类型登记里，不能改计划。
    /// </summary>
    public const string JobTypeUnknown = "MAINTENANCE_CONFLICT_001";

    /// <summary>
    /// 作业有多个触发器，不能整体改计划。
    /// </summary>
    public const string JobTriggersMany = "MAINTENANCE_CONFLICT_002";

    /// <summary>
    /// 键或前缀不在开放的缓存命名空间里。
    /// </summary>
    public const string CacheNamespaceDenied = "MAINTENANCE_DENIED_002";

    /// <summary>
    /// 缓存命名空间只读，不能改写。
    /// </summary>
    public const string CacheReadOnly = "MAINTENANCE_DENIED_003";

    /// <summary>
    /// 缓存项不存在。
    /// </summary>
    public const string CacheEntryNotFound = "MAINTENANCE_NOTFOUND_003";

    /// <summary>
    /// 缓存项的值不是文本，不能改写。
    /// </summary>
    public const string CacheValueNotText = "MAINTENANCE_VALIDATION_003";

    /// <summary>
    /// 缓存存储不支持键管理。
    /// </summary>
    public const string CacheInspectorUnavailable = "MAINTENANCE_UNAVAILABLE_001";

    /// <summary>
    /// 没有装运行信息（IHostMonitor）。
    /// </summary>
    public const string HostMonitorUnavailable = "MAINTENANCE_UNAVAILABLE_002";

    /// <summary>
    /// 同名作业已存在。
    /// </summary>
    /// <remarks>
    /// 参数 <c>id</c>。
    /// </remarks>
    public const string JobExists = "MAINTENANCE_CONFLICT_003";

    /// <summary>
    /// 代码登记的作业不能删除。
    /// </summary>
    /// <remarks>
    /// 运行时改过计划的也算代码登记，可用 <c>jobs.restore</c> 改回代码定义。
    /// </remarks>
    public const string JobFromCode = "MAINTENANCE_CONFLICT_004";

    /// <summary>
    /// 作业名不合规。
    /// </summary>
    /// <remarks>
    /// 参数 <c>name</c>。
    /// </remarks>
    public const string JobNameInvalid = "MAINTENANCE_VALIDATION_004";

    /// <summary>
    /// 作业参数不是 JSON 对象。
    /// </summary>
    public const string JobDataInvalid = "MAINTENANCE_VALIDATION_005";
}
