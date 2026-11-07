// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using XiHan.BasicApp.Access.Domain.Entities;
using XiHan.BasicApp.Contracts.Workflow;

namespace XiHan.BasicApp.Access.Operations.PermissionRequests;

/// <summary>
/// 一条授权申请
/// </summary>
/// <param name="Id">ID</param>
/// <param name="RequestUserId">申请人</param>
/// <param name="RequestUserName">申请人显示名</param>
/// <param name="RoleId">申请的角色</param>
/// <param name="RoleCode">角色编码</param>
/// <param name="RoleName">角色名</param>
/// <param name="RoleDescription">角色说明</param>
/// <param name="PermissionCode">申请的权限码</param>
/// <param name="PermissionName">权限名（按界面语言）</param>
/// <param name="PermissionDescription">权限说明（按界面语言）</param>
/// <param name="Status">状态</param>
/// <param name="Reason">申请原因</param>
/// <param name="ExpectedEffectiveAt">期望生效时间</param>
/// <param name="ExpectedExpiresAt">期望失效时间</param>
/// <param name="IsExpectedExpired">期望失效时间已过（算出的标记，不是状态）</param>
/// <param name="WorkflowInstanceId">当前的审批流程实例</param>
/// <param name="Workflow">审批进度（没有发起过、审批未启用时为空）</param>
/// <param name="ApprovalEnabled">审批是否启用（Workflow 模块关掉时为假，方案 W1）</param>
/// <param name="Remark">备注</param>
/// <param name="CreatedAt">提交时间</param>
/// <param name="ModifiedAt">最后修改时间</param>
public sealed record PermissionRequestResponse(
    Guid Id,
    Guid RequestUserId,
    string? RequestUserName,
    Guid? RoleId,
    string? RoleCode,
    string? RoleName,
    string? RoleDescription,
    string? PermissionCode,
    string? PermissionName,
    string? PermissionDescription,
    PermissionRequestStatus Status,
    string Reason,
    DateTimeOffset? ExpectedEffectiveAt,
    DateTimeOffset? ExpectedExpiresAt,
    bool IsExpectedExpired,
    Guid? WorkflowInstanceId,
    PermissionRequestProgress? Workflow,
    bool ApprovalEnabled,
    string? Remark,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ModifiedAt);

/// <summary>
/// 授权申请的审批进度
/// </summary>
/// <param name="InstanceId">最近一次的审批流程实例</param>
/// <param name="Status">流程状态</param>
/// <param name="Outcome">结论</param>
/// <param name="PendingNodeNames">运行中时等着办的节点</param>
/// <param name="PendingAssignees">运行中时等着办的人</param>
/// <param name="FaultMessage">故障原因</param>
/// <param name="StartedAt">发起时间</param>
/// <param name="EndedAt">结束时间</param>
public sealed record PermissionRequestProgress(
    Guid InstanceId,
    WorkflowBusinessStatus Status,
    string? Outcome,
    IReadOnlyList<string> PendingNodeNames,
    IReadOnlyList<PermissionRequestAssignee> PendingAssignees,
    string? FaultMessage,
    DateTimeOffset StartedAt,
    DateTimeOffset? EndedAt);

/// <summary>
/// 等着办的人
/// </summary>
/// <param name="UserId">用户</param>
/// <param name="DisplayName">显示名</param>
public sealed record PermissionRequestAssignee(Guid UserId, string DisplayName);

/// <summary>
/// 按 ID 取一条授权申请
/// </summary>
/// <param name="Id">ID</param>
public sealed record GetPermissionRequestRequest(Guid Id);

/// <summary>
/// 提交授权申请
/// </summary>
/// <param name="RoleId">申请的角色（与权限码二选一）</param>
/// <param name="PermissionCode">申请的权限码（与角色二选一）</param>
/// <param name="Reason">申请原因</param>
/// <param name="ExpectedEffectiveAt">期望生效时间（为空即通过后立即生效）</param>
/// <param name="ExpectedExpiresAt">期望失效时间（为空即长期有效，要晚于现在与生效时间）</param>
/// <param name="Remark">备注</param>
public sealed record CreatePermissionRequestRequest(
    Guid? RoleId,
    [property: StringLength(128)] string? PermissionCode,
    [property: Required, StringLength(1000, MinimumLength = 1)] string Reason,
    DateTimeOffset? ExpectedEffectiveAt = null,
    DateTimeOffset? ExpectedExpiresAt = null,
    [property: StringLength(500)] string? Remark = null);

/// <summary>
/// 修改并重新提交授权申请
/// </summary>
/// <param name="Id">ID</param>
/// <param name="RoleId">申请的角色（与权限码二选一）</param>
/// <param name="PermissionCode">申请的权限码（与角色二选一）</param>
/// <param name="Reason">申请原因</param>
/// <param name="ExpectedEffectiveAt">期望生效时间</param>
/// <param name="ExpectedExpiresAt">期望失效时间</param>
/// <param name="Remark">备注</param>
public sealed record UpdatePermissionRequestRequest(
    Guid Id,
    Guid? RoleId,
    [property: StringLength(128)] string? PermissionCode,
    [property: Required, StringLength(1000, MinimumLength = 1)] string Reason,
    DateTimeOffset? ExpectedEffectiveAt = null,
    DateTimeOffset? ExpectedExpiresAt = null,
    [property: StringLength(500)] string? Remark = null);

/// <summary>
/// 一批授权申请
/// </summary>
/// <param name="Ids">ID，1 到 100 个</param>
public sealed record PermissionRequestIdsRequest([property: Required, MinLength(1), MaxLength(100)] IReadOnlyList<Guid> Ids);

/// <summary>
/// 手工处理一批授权申请
/// </summary>
/// <param name="Ids">ID，1 到 100 个</param>
/// <param name="Status">置为的状态（只能是已驳回或已撤回）</param>
/// <param name="Remark">备注（也是流程的取消原因）</param>
public sealed record SetPermissionRequestStatusRequest(
    [property: Required, MinLength(1), MaxLength(100)] IReadOnlyList<Guid> Ids,
    PermissionRequestStatus Status,
    [property: StringLength(500)] string? Remark = null);
