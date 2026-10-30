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
}
