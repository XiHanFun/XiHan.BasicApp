// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Pages;

namespace XiHan.BasicApp.Auditing;

/// <summary>
/// 审计包的页面登记。
/// </summary>
/// <remarks>
/// 链路追踪与八类日志，两侧都有。页面平铺登记，归到哪个目录由运营在菜单管理里定（修订（十四））。
/// </remarks>
public static class AuditingPages
{
    /// <summary>
    /// 日志链路追踪。
    /// </summary>
    public static PageDescriptor Traces { get; } = new("auditing.traces", "/auditing/traces", "auditing.traces.title", "lucide:route", 500, AuditingPermissions.TracesRead);

    /// <summary>
    /// 操作日志。
    /// </summary>
    public static PageDescriptor OperationLogs { get; } = new("auditing.operation-logs", "/auditing/operation-logs", "auditing.operation_logs.title", "lucide:clipboard-list", 510, AuditingPermissions.OperationLogsRead);

    /// <summary>
    /// 登录日志。
    /// </summary>
    public static PageDescriptor LoginLogs { get; } = new("auditing.login-logs", "/auditing/login-logs", "auditing.login_logs.title", "lucide:log-in", 520, AuditingPermissions.LoginLogsRead);

    /// <summary>
    /// 访问日志。
    /// </summary>
    public static PageDescriptor AccessLogs { get; } = new("auditing.access-logs", "/auditing/access-logs", "auditing.access_logs.title", "lucide:globe", 530, AuditingPermissions.AccessLogsRead);

    /// <summary>
    /// 异常日志。
    /// </summary>
    public static PageDescriptor ExceptionLogs { get; } = new("auditing.exception-logs", "/auditing/exception-logs", "auditing.exception_logs.title", "lucide:bug", 540, AuditingPermissions.ExceptionLogsRead);

    /// <summary>
    /// 数据变更日志。
    /// </summary>
    public static PageDescriptor DiffLogs { get; } = new("auditing.diff-logs", "/auditing/diff-logs", "auditing.diff_logs.title", "lucide:git-compare", 550, AuditingPermissions.DiffLogsRead);

    /// <summary>
    /// 权限变更日志。
    /// </summary>
    public static PageDescriptor PermissionChangeLogs { get; } = new("auditing.permission-change-logs", "/auditing/permission-change-logs", "auditing.permission_change_logs.title", "lucide:key-round", 560, AuditingPermissions.PermissionChangeLogsRead);

    /// <summary>
    /// 条件命中。
    /// </summary>
    public static PageDescriptor ConditionHits { get; } = new("auditing.condition-hits", "/auditing/condition-hits", "auditing.condition_hits.title", "lucide:filter", 570, AuditingPermissions.ConditionHitsRead);

    /// <summary>
    /// 开放接口日志。
    /// </summary>
    public static PageDescriptor OpenApiLogs { get; } = new("auditing.open-api-logs", "/auditing/open-api-logs", "auditing.open_api_logs.title", "lucide:plug", 580, AuditingPermissions.OpenApiLogsRead);

    /// <summary>
    /// 全部顶层页面。
    /// </summary>
    public static IReadOnlyList<PageDescriptor> All { get; } = [Traces, OperationLogs, LoginLogs, AccessLogs, ExceptionLogs, DiffLogs, PermissionChangeLogs, ConditionHits, OpenApiLogs];
}
