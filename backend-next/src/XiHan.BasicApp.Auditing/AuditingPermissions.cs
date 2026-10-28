// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Security;

namespace XiHan.BasicApp.Auditing;

/// <summary>
/// 审计包的权限码：只有查看，两侧都有。日志严格按上下文隔离：平台主体看平台一侧的日志，租户主体看本租户的；要看某个租户的日志先切换到它。
/// </summary>
public static class AuditingPermissions
{
    /// <summary>
    /// 查看访问日志。
    /// </summary>
    public const string AccessLogsRead = "auditing.access-logs.read";

    /// <summary>
    /// 查看异常日志。
    /// </summary>
    public const string ExceptionLogsRead = "auditing.exception-logs.read";

    /// <summary>
    /// 查看登录日志。
    /// </summary>
    public const string LoginLogsRead = "auditing.login-logs.read";

    /// <summary>
    /// 查看操作日志。
    /// </summary>
    public const string OperationLogsRead = "auditing.operation-logs.read";

    /// <summary>
    /// 查看数据变更日志。
    /// </summary>
    public const string DiffLogsRead = "auditing.diff-logs.read";

    /// <summary>
    /// 查看权限变更日志。
    /// </summary>
    public const string PermissionChangeLogsRead = "auditing.permission-change-logs.read";

    /// <summary>
    /// 查看条件命中记录。
    /// </summary>
    public const string ConditionHitsRead = "auditing.condition-hits.read";

    /// <summary>
    /// 查看开放接口日志。
    /// </summary>
    public const string OpenApiLogsRead = "auditing.open-api-logs.read";

    /// <summary>
    /// 按链路聚合各类日志。
    /// </summary>
    public const string TracesRead = "auditing.traces.read";

    /// <summary>
    /// 全部权限描述。
    /// </summary>
    public static IReadOnlyList<PermissionDescriptor> All { get; } =
    [
        new(AccessLogsRead, "Read access logs", PermissionScope.Both, description: "View every operation call of the caller's side with its result, duration and client."),
        new(ExceptionLogsRead, "Read exception logs", PermissionScope.Both, description: "View unhandled exceptions of the caller's side, including stack traces."),
        new(LoginLogsRead, "Read sign-in logs", PermissionScope.Both, description: "View sign-in, refresh, sign-out, tenant switch, impersonation and password change events of the caller's side."),
        new(OperationLogsRead, "Read operation logs", PermissionScope.Both, description: "View audited business operations of the caller's side with their results."),
        new(DiffLogsRead, "Read data change logs", PermissionScope.Both, description: "View entity inserts, updates and deletes of the caller's side with field-level before and after values."),
        new(PermissionChangeLogsRead, "Read permission change logs", PermissionScope.Both, description: "View grants, revocations, role membership, data scope and edition changes of the caller's side."),
        new(ConditionHitsRead, "Read condition hits", PermissionScope.Both, description: "View access policy and permission condition hits of the caller's side."),
        new(OpenApiLogsRead, "Read open API logs", PermissionScope.Both, description: "View signed open API calls of the caller's side with their signature checks."),
        new(TracesRead, "Read traces", PermissionScope.Both, description: "Aggregate the logs of the caller's side by trace, user, session or client address into one timeline."),
    ];
}
