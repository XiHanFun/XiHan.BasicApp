// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text.Json;
using XiHan.BasicApp.Access.Domain.Entities;
using XiHan.BasicApp.Access.Domain.Services;
using XiHan.BasicApp.Access.Operations.Roles;
using XiHan.BasicApp.Access.Operations.UserGrants;
using XiHan.BasicApp.Contracts.Workflow;
using XiHan.BasicApp.Runtime.Persistence;
using XiHan.Framework.Localization.Abstractions.Messages;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Access.Infrastructure;

/// <summary>
/// 授权申请的审批业务
/// </summary>
/// <remarks>
/// 照 4.x 的 <c>PermissionRequestWorkflowHandler</c>：
/// <list type="bullet">
/// <item>同意前校验审批人：不能审批自己的申请，且要能授出申请的对象；</item>
/// <item>模仿会话里拒绝办理（同意、驳回都算）与转办、加签、撤回，以及管理员的取消、终止、重试、挂起与恢复（6.0 新加，4.x 没有）；</item>
/// <item>结论 <c>approved</c>：重核对象仍可用，按期望的有效期指派角色（经 <see cref="RoleAssignmentService"/>，过约束规则，拦截类违规即失败）与直授权限（经 <see cref="UserGrantService"/>），
/// 再置为已批准；任一步失败即返回失败，结束节点故障、申请保持待审批，处理后重试流程（4.x 同）；已批准时幂等；</item>
/// <item>结论 <c>rejected</c> 置为已驳回；中止（管理员取消、终止，发起人在审批中心撤回）把待审批的置为已撤回；</item>
/// <item>单据组件 <c>access/permission-request-approval/index</c>（设计稿 D4），数据是与 <c>permission-requests.get</c> 同一形态的响应</item>
/// </list>
/// 缺省定义（当前分区第一次提交时建并发布）：管理员角色 <c>admin</c> 审批，不含申请人自己（4.x 的租户所有者与超管在 6.0 合成两侧同名的内建 <c>admin</c>）
/// </remarks>
/// <param name="store">授权申请的读写与校验</param>
/// <param name="assignments">角色指派</param>
/// <param name="grants">直接授予</param>
public sealed class PermissionRequestApprovalHandler(PermissionRequestStore store, RoleAssignmentService assignments, UserGrantService grants) : IWorkflowBusinessHandler
{
    /// <summary>
    /// 单据组件
    /// </summary>
    public const string FormComponent = "access/permission-request-approval/index";

    private const string DefaultDefinition = """
        {
          "name": "permission-request",
          "displayName": "Permission request",
          "description": "Default approval: an administrator other than the requester approves; add a new version to change it.",
          "category": "Access",
          "nodes": [
            { "id": "start", "activityType": "Start" },
            { "id": "admin-approve", "activityType": "UserTask", "displayName": "Administrator approval", "properties": { "AssigneeRules": [{ "type": "Role", "values": ["admin"] }], "ExcludeStarter": true } },
            { "id": "end-approved", "activityType": "End", "displayName": "Approved", "properties": { "Outcome": "approved" } },
            { "id": "end-rejected", "activityType": "End", "displayName": "Rejected", "properties": { "Outcome": "rejected" } }
          ],
          "transitions": [
            { "id": "t-start", "sourceNodeId": "start", "targetNodeId": "admin-approve" },
            { "id": "t-approved", "sourceNodeId": "admin-approve", "targetNodeId": "end-approved", "condition": "outcome == 'approved'" },
            { "id": "t-rejected", "sourceNodeId": "admin-approve", "targetNodeId": "end-rejected", "condition": "outcome == 'rejected'" }
          ]
        }
        """;

    /// <inheritdoc />
    public WorkflowBusinessDescriptor Descriptor { get; } = new(
        PermissionRequestStore.BusinessCode,
        "access.permission_requests.business_name",
        FormComponent,
        [
            new("requestUserId", "access.permission_requests.variable_request_user", "user"),
            new("roleId", "access.permission_requests.variable_role", "string"),
            new("roleCode", "access.permission_requests.variable_role_code", "string"),
            new("permissionCode", "access.permission_requests.variable_permission_code", "string"),
        ],
        [],
        DefaultDefinition);

    /// <inheritdoc />
    public async ValueTask<Result> OnTaskCompletingAsync(WorkflowBusinessTask task, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(task);
        ArgumentNullException.ThrowIfNull(context);
        var impersonation = PermissionRequestStore.RejectImpersonation(context);
        if (impersonation.IsFailure)
        {
            return impersonation.Error!;
        }

        if (!string.Equals(task.Outcome, "approved", StringComparison.Ordinal))
        {
            return Result.Success();
        }

        var found = await FindAsync(task.BusinessId, context).ConfigureAwait(false);
        if (!found.TryGetValue(out var request))
        {
            return found.Error!;
        }

        return request.RequestUserId == task.ActorId
            ? Error.Denied(AccessErrors.PermissionRequestSelfHandled, AccessMessages.PermissionRequestSelfHandled)
            : await store.CanGrantAsync(request, context).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public ValueTask<Result> OnChangingAsync(WorkflowBusinessChange change, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(change);
        return ValueTask.FromResult(PermissionRequestStore.RejectImpersonation(context));
    }

    /// <inheritdoc />
    public async ValueTask<Result> OnOutcomeAsync(WorkflowBusinessOutcome outcome, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(outcome);
        ArgumentNullException.ThrowIfNull(context);
        var found = await FindAsync(outcome.BusinessId, context).ConfigureAwait(false);
        if (!found.TryGetValue(out var request))
        {
            return found.Error!;
        }

        var target = outcome.Outcome == "approved" ? PermissionRequestStatus.Approved : PermissionRequestStatus.Rejected;
        if (request.Status == target)
        {
            return Result.Success();
        }

        if (request.Status != PermissionRequestStatus.Pending)
        {
            return Error.Conflict(AccessErrors.PermissionRequestNotPending, AccessMessages.PermissionRequestNotPending, MessageArguments.Of(("id", request.Id), ("status", request.Status)));
        }

        if (target == PermissionRequestStatus.Approved)
        {
            var granted = await GrantAsync(request, context).ConfigureAwait(false);
            if (granted.IsFailure)
            {
                return granted;
            }
        }

        request.Status = target;
        await store.Rows.UpdateAsync(request, context.CancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    /// <inheritdoc />
    public async ValueTask<Result> OnAbortedAsync(WorkflowBusinessAborted aborted, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(aborted);
        ArgumentNullException.ThrowIfNull(context);
        var found = await FindAsync(aborted.BusinessId, context).ConfigureAwait(false);
        if (!found.TryGetValue(out var request))
        {
            return found.Error!;
        }

        if (request.Status != PermissionRequestStatus.Pending)
        {
            return Result.Success();
        }

        request.Status = PermissionRequestStatus.Withdrawn;
        request.Remark = string.IsNullOrWhiteSpace(aborted.Reason) ? context.Localize(AccessMessages.PermissionRequestAborted) : Clip(aborted.Reason.Trim(), 500);
        await store.Rows.UpdateAsync(request, context.CancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    /// <inheritdoc />
    public async ValueTask<Result<string>> GetFormDataAsync(string businessId, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var found = await FindAsync(businessId, context).ConfigureAwait(false);
        if (!found.TryGetValue(out var request))
        {
            return found.Error!;
        }

        // 审批详情另有审批轨迹，单据里不再带进度（审批当然是启用的）
        var projected = await store.ProjectAsync([request], new Dictionary<string, WorkflowBusinessProgress>(), context).ConfigureAwait(false);
        return JsonSerializer.Serialize(projected[0], AccessJsonContext.Default.PermissionRequestResponse);
    }

    /// <summary>
    /// 指派角色与直授权限（沿用期望的有效期）
    /// </summary>
    private async Task<Result> GrantAsync(SysPermissionRequest request, InvocationContext context)
    {
        // 审批期间角色可能停用、权限可能下线：重核一遍
        var target = await store.TargetAsync(request.RoleId, request.PermissionCode, context).ConfigureAwait(false);
        if (!target.TryGetValue(out var resolved))
        {
            return target.Error!;
        }

        if (resolved.Role is { } role)
        {
            var assigned = await assignments.AssignAsync(new RoleMembersRequest(role.Id, [request.RequestUserId], request.ExpectedEffectiveAt, request.ExpectedExpiresAt), context).ConfigureAwait(false);
            if (!assigned.TryGetValue(out var batch))
            {
                return assigned.Error!;
            }

            if (batch.Failed.Count > 0)
            {
                return GrantFailed(batch.Failed[0].Code);
            }
        }

        if (resolved.Permission is { } permission)
        {
            var reason = context.Localize(AccessMessages.PermissionRequestGrantReason, MessageArguments.Of(("id", request.Id)));
            var granted = await grants.GrantAsync(new UserPermissionsRequest(request.RequestUserId, [permission.Code], request.ExpectedEffectiveAt, request.ExpectedExpiresAt, Clip(reason, 512)), context).ConfigureAwait(false);
            if (!granted.TryGetValue(out var batch))
            {
                return granted.Error!;
            }

            if (batch.Failed.Count > 0)
            {
                return GrantFailed(batch.Failed[0].Code);
            }
        }

        return Result.Success();
    }

    private async Task<Result<SysPermissionRequest>> FindAsync(string businessId, InvocationContext context)
        => Guid.TryParse(businessId, out var id)
            ? await store.FindAsync(SideRows.SideOf(context), id, context.CancellationToken).ConfigureAwait(false)
            : Error.NotFound(AccessErrors.PermissionRequestNotFound, AccessMessages.PermissionRequestNotFound, MessageArguments.Of(("id", businessId)));

    private static Error GrantFailed(string code)
        => Error.Conflict(AccessErrors.PermissionRequestGrantFailed, AccessMessages.PermissionRequestGrantFailed, MessageArguments.Of(("code", code)));

    private static string Clip(string value, int maxLength) => value.Length <= maxLength ? value : value[..maxLength];
}
