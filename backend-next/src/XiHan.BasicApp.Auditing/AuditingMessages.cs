// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.Framework.Localization.Abstractions.Messages;

namespace XiHan.BasicApp.Auditing;

/// <summary>
/// 审计包的消息键。
/// </summary>
/// <remarks>
/// 各区域文案在 <c>Localization/&lt;区域&gt;.json</c>。操作说明用约定键 <c>&lt;操作 ID&gt;.description</c>（资源段的 <c>-</c> 换成 <c>_</c>）。
/// </remarks>
public static class AuditingMessages
{
    /// <summary>
    /// 消息目录名。
    /// </summary>
    public const string CatalogName = "basicapp.auditing";

    /// <summary>
    /// 操作 <c>access-logs.list</c> 的说明。
    /// </summary>
    public const string ListAccessLogsDescription = "access_logs.list.description";

    /// <summary>
    /// 操作 <c>access-logs.get</c> 的说明。
    /// </summary>
    public const string GetAccessLogDescription = "access_logs.get.description";

    /// <summary>
    /// 操作 <c>exception-logs.list</c> 的说明。
    /// </summary>
    public const string ListExceptionLogsDescription = "exception_logs.list.description";

    /// <summary>
    /// 操作 <c>exception-logs.get</c> 的说明。
    /// </summary>
    public const string GetExceptionLogDescription = "exception_logs.get.description";

    /// <summary>
    /// 操作 <c>login-logs.list</c> 的说明。
    /// </summary>
    public const string ListLoginLogsDescription = "login_logs.list.description";

    /// <summary>
    /// 操作 <c>login-logs.get</c> 的说明。
    /// </summary>
    public const string GetLoginLogDescription = "login_logs.get.description";

    /// <summary>
    /// 操作 <c>operation-logs.list</c> 的说明。
    /// </summary>
    public const string ListOperationLogsDescription = "operation_logs.list.description";

    /// <summary>
    /// 操作 <c>operation-logs.get</c> 的说明。
    /// </summary>
    public const string GetOperationLogDescription = "operation_logs.get.description";

    /// <summary>
    /// 操作 <c>diff-logs.list</c> 的说明。
    /// </summary>
    public const string ListDiffLogsDescription = "diff_logs.list.description";

    /// <summary>
    /// 操作 <c>diff-logs.get</c> 的说明。
    /// </summary>
    public const string GetDiffLogDescription = "diff_logs.get.description";

    /// <summary>
    /// 操作 <c>permission-change-logs.list</c> 的说明。
    /// </summary>
    public const string ListPermissionChangeLogsDescription = "permission_change_logs.list.description";

    /// <summary>
    /// 操作 <c>permission-change-logs.get</c> 的说明。
    /// </summary>
    public const string GetPermissionChangeLogDescription = "permission_change_logs.get.description";

    /// <summary>
    /// 操作 <c>condition-hits.list</c> 的说明。
    /// </summary>
    public const string ListConditionHitsDescription = "condition_hits.list.description";

    /// <summary>
    /// 操作 <c>condition-hits.get</c> 的说明。
    /// </summary>
    public const string GetConditionHitDescription = "condition_hits.get.description";

    /// <summary>
    /// 操作 <c>open-api-logs.list</c> 的说明。
    /// </summary>
    public const string ListOpenApiLogsDescription = "open_api_logs.list.description";

    /// <summary>
    /// 操作 <c>open-api-logs.get</c> 的说明。
    /// </summary>
    public const string GetOpenApiLogDescription = "open_api_logs.get.description";

    /// <summary>
    /// 操作 <c>traces.timeline</c> 的说明。
    /// </summary>
    public const string TraceTimelineDescription = "traces.timeline.description";

    /// <summary>
    /// 日志不存在。
    /// </summary>
    public const string LogNotFound = "auditing.log.not_found";

    /// <summary>
    /// 不支持的筛选。
    /// </summary>
    public const string FilterUnsupported = "auditing.query.filter_unsupported";

    /// <summary>
    /// 不支持的排序。
    /// </summary>
    public const string SortUnsupported = "auditing.query.sort_unsupported";

    /// <summary>
    /// 筛选值不合法。
    /// </summary>
    public const string FilterValueInvalid = "auditing.query.value_invalid";

    /// <summary>
    /// 链路查询缺维度值。
    /// </summary>
    public const string TraceValueRequired = "auditing.trace.value_required";

    /// <summary>
    /// 链路查询的起止时间颠倒。
    /// </summary>
    public const string TraceRangeInvalid = "auditing.trace.range_invalid";

    /// <summary>
    /// 保留天数参数不合法。
    /// </summary>
    public const string RetentionInvalid = "auditing.retention.invalid";

    /// <summary>
    /// 操作 <c>my-login-logs.list</c> 的说明。
    /// </summary>
    public const string ListMyLoginLogsDescription = "my_login_logs.list.description";

    /// <summary>
    /// 操作 <c>my-activity.summary</c> 的说明。
    /// </summary>
    public const string MyActivityDescription = "my_activity.summary.description";

    /// <summary>
    /// 时区不是认得的 IANA 名称。
    /// </summary>
    /// <remarks>
    /// 参数 <c>timeZone</c>。
    /// </remarks>
    public const string TimeZoneInvalid = "auditing.time_zone.invalid";

    /// <summary>
    /// 作业 <c>auditing.retention</c> 的显示名。
    /// </summary>
    public const string RetentionJobTitle = "jobs.auditing.retention.title";

    /// <summary>
    /// 作业 <c>auditing.retention</c> 的说明。
    /// </summary>
    public const string RetentionJobDescription = "jobs.auditing.retention.description";

    private static readonly Lazy<MessageCatalog> _catalog = new(static () => MessageCatalog.FromEmbeddedResources(CatalogName, typeof(AuditingMessages)));

    /// <summary>
    /// 本包的消息目录（嵌入的 10 种区域文件）。
    /// </summary>
    public static MessageCatalog Catalog => _catalog.Value;
}
