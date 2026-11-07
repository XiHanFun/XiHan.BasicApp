// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text.Json.Nodes;
using SqlSugar;
using XiHan.BasicApp.Access.Domain.Entities;
using XiHan.BasicApp.Access.Operations.PermissionRequests;
using XiHan.BasicApp.Contracts.Batch;
using XiHan.BasicApp.Contracts.Paging;
using XiHan.BasicApp.Contracts.Workflow;
using XiHan.BasicApp.Runtime.Persistence;
using XiHan.Framework.Kernel.Security;
using XiHan.Framework.Localization.Abstractions.Messages;
using XiHan.Framework.Persistence.Abstractions.Identifiers;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Access.Domain.Services;

/// <summary>
/// 授权申请的提交、重新提交、撤回、手工处理与查询（作用域）
/// </summary>
/// <remarks>
/// 提交即在同一个事务里经审批业务服务发起 <see cref="PermissionRequestStore.BusinessCode"/> 的流程；
/// 修改后重新提交撤回旧流程、按新内容重新发起；申请人撤回与管理员手工驳回 / 撤回都同时撤回进行中的流程（业务自己撤回，不回调中止）
/// Workflow 模块关掉时没有审批业务服务：列表照常、进度为空并标「审批未启用」，提交与重新提交即 <see cref="AccessErrors.ApprovalUnavailable"/>（方案 W1）
/// </remarks>
/// <param name="store">读写与校验</param>
/// <param name="ids">ID 生成</param>
/// <param name="workflows">审批业务服务（Workflow 关掉时没有）</param>
public sealed class PermissionRequestService(PermissionRequestStore store, IIdGenerator<Guid> ids, IEnumerable<IWorkflowBusinessService> workflows)
{
    private const string FieldStatus = "status";
    private const string FieldRequestUserId = "requestUserId";
    private const string FieldRoleId = "roleId";
    private const string FieldPermissionCode = "permissionCode";
    private const string FieldCreatedAt = "createdAt";

    private IWorkflowBusinessService? Workflow => workflows.FirstOrDefault();

    /// <summary>
    /// 提交
    /// </summary>
    public async Task<Result<PermissionRequestResponse>> CreateAsync(CreatePermissionRequestRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var impersonation = PermissionRequestStore.RejectImpersonation(context);
        if (impersonation.IsFailure)
        {
            return impersonation.Error!;
        }

        if (Workflow is not { } workflow)
        {
            return Error.Unavailable(AccessErrors.ApprovalUnavailable, AccessMessages.ApprovalUnavailable);
        }

        var target = await store.ValidateAsync(request.RoleId, request.PermissionCode, request.ExpectedEffectiveAt, request.ExpectedExpiresAt, context).ConfigureAwait(false);
        if (!target.TryGetValue(out var resolved))
        {
            return target.Error!;
        }

        var requester = await store.RequesterAsync(context).ConfigureAwait(false);
        if (!requester.TryGetValue(out var me))
        {
            return requester.Error!;
        }

        var side = SideRows.SideOf(context);
        var unique = await store.EnsureNoDuplicateAsync(side, me, resolved, null, context.CancellationToken).ConfigureAwait(false);
        if (unique.IsFailure)
        {
            return unique.Error!;
        }

        var row = new SysPermissionRequest
        {
            Id = ids.NewId(),
            TenantId = side,
            RequestUserId = me,
            RoleId = resolved.Role?.Id,
            PermissionCode = resolved.Permission?.Code,
            Reason = request.Reason.Trim(),
            ExpectedEffectiveAt = request.ExpectedEffectiveAt,
            ExpectedExpiresAt = request.ExpectedExpiresAt,
            Remark = Optional(request.Remark),
        };
        await store.Rows.InsertAsync(row, context.CancellationToken).ConfigureAwait(false);
        var started = await StartAsync(workflow, row.Id, resolved, me, context).ConfigureAwait(false);
        return started.IsSuccess ? await GetAsync(row.Id, context).ConfigureAwait(false) : started.Error!;
    }

    /// <summary>
    /// 修改并重新提交
    /// </summary>
    public async Task<Result<PermissionRequestResponse>> UpdateAsync(UpdatePermissionRequestRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var impersonation = PermissionRequestStore.RejectImpersonation(context);
        if (impersonation.IsFailure)
        {
            return impersonation.Error!;
        }

        if (Workflow is not { } workflow)
        {
            return Error.Unavailable(AccessErrors.ApprovalUnavailable, AccessMessages.ApprovalUnavailable);
        }

        var side = SideRows.SideOf(context);
        var found = await store.FindAsync(side, request.Id, context.CancellationToken).ConfigureAwait(false);
        if (!found.TryGetValue(out var row))
        {
            return found.Error!;
        }

        var requester = await store.RequesterAsync(context).ConfigureAwait(false);
        if (!requester.TryGetValue(out var me))
        {
            return requester.Error!;
        }

        var maintainable = OwnPending(row, me);
        if (maintainable.IsFailure)
        {
            return maintainable.Error!;
        }

        var target = await store.ValidateAsync(request.RoleId, request.PermissionCode, request.ExpectedEffectiveAt, request.ExpectedExpiresAt, context).ConfigureAwait(false);
        if (!target.TryGetValue(out var resolved))
        {
            return target.Error!;
        }

        var unique = await store.EnsureNoDuplicateAsync(side, me, resolved, row.Id, context.CancellationToken).ConfigureAwait(false);
        if (unique.IsFailure)
        {
            return unique.Error!;
        }

        row.RoleId = resolved.Role?.Id;
        row.PermissionCode = resolved.Permission?.Code;
        row.Reason = request.Reason.Trim();
        row.ExpectedEffectiveAt = request.ExpectedEffectiveAt;
        row.ExpectedExpiresAt = request.ExpectedExpiresAt;
        row.Remark = Optional(request.Remark);
        await store.Rows.UpdateAsync(row, context.CancellationToken).ConfigureAwait(false);

        // 申请内容变了，审批人此前看到的不再是同一份申请：撤回旧流程、按新内容重新发起
        var withdrawn = await workflow.WithdrawAsync(PermissionRequestStore.BusinessCode, row.Id.ToString(), context.Localize(AccessMessages.PermissionRequestResubmitted), context).ConfigureAwait(false);
        if (withdrawn.IsFailure)
        {
            return withdrawn.Error!;
        }

        var started = await StartAsync(workflow, row.Id, resolved, me, context).ConfigureAwait(false);
        return started.IsSuccess ? await GetAsync(row.Id, context).ConfigureAwait(false) : started.Error!;
    }

    /// <summary>
    /// 申请人撤回自己待审批的申请
    /// </summary>
    public async Task<Result<BatchResult>> WithdrawAsync(PermissionRequestIdsRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var impersonation = PermissionRequestStore.RejectImpersonation(context);
        if (impersonation.IsFailure)
        {
            return impersonation.Error!;
        }

        var requester = await store.RequesterAsync(context).ConfigureAwait(false);
        if (!requester.TryGetValue(out var me))
        {
            return requester.Error!;
        }

        var reason = context.Localize(AccessMessages.PermissionRequestWithdrawnByRequester);
        return await EachAsync(request.Ids, context, row => Task.FromResult(OwnPending(row, me)), PermissionRequestStatus.Withdrawn, null, reason).ConfigureAwait(false);
    }

    /// <summary>
    /// 管理员手工驳回或撤回别人待审批的申请
    /// </summary>
    public async Task<Result<BatchResult>> SetStatusAsync(SetPermissionRequestStatusRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var impersonation = PermissionRequestStore.RejectImpersonation(context);
        if (impersonation.IsFailure)
        {
            return impersonation.Error!;
        }

        if (request.Status is not (PermissionRequestStatus.Rejected or PermissionRequestStatus.Withdrawn))
        {
            return Error.Validation(AccessErrors.PermissionRequestStatusInvalid, AccessMessages.PermissionRequestStatusInvalid, MessageArguments.Of(("status", request.Status)));
        }

        var operatorId = Guid.TryParse(context.GetSecurity().Principal.Id, out var me) ? me : Guid.Empty;
        var remark = Optional(request.Remark);
        var reason = remark ?? context.Localize(AccessMessages.PermissionRequestClosedByAdmin);
        return await EachAsync(request.Ids, context, row => row.RequestUserId == operatorId
            ? Task.FromResult<Result>(Error.Denied(AccessErrors.PermissionRequestSelfHandled, AccessMessages.PermissionRequestSelfHandled))
            : store.CanGrantAsync(row, context), request.Status, remark, reason, allowSame: true).ConfigureAwait(false);
    }

    /// <summary>
    /// 分页列出这一侧的申请
    /// </summary>
    /// <remarks>
    /// 持有查看权限即看这一侧全部的申请；关键字匹配申请人（经用户目录按登录名与显示名，见 <see cref="PermissionRequestStore.RequestersMatchingAsync"/>）、申请原因与备注，
    /// 筛选申请人、角色、权限码与状态，缺省按提交时间降序
    /// </remarks>
    public async Task<Result<PageResult<PermissionRequestResponse>>> ListAsync(PageRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var side = SideRows.SideOf(context);
        var query = store.Rows.Query<SysPermissionRequest>(side);
        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            var keyword = request.Keyword.Trim();
            var requesters = await store.RequestersMatchingAsync(side, keyword, context.CancellationToken).ConfigureAwait(false);
            query = requesters.Count == 0
                ? query.Where(r => r.Reason.Contains(keyword) || (r.Remark != null && r.Remark.Contains(keyword)))
                : query.Where(r => r.Reason.Contains(keyword) || (r.Remark != null && r.Remark.Contains(keyword)) || requesters.Contains(r.RequestUserId));
        }

        foreach (var filter in request.Filters)
        {
            var value = filter.Value ?? string.Empty;
            ISugarQueryable<SysPermissionRequest>? filtered = (filter.Field, filter.Operator) switch
            {
                (FieldStatus, "eq" or "in") when EnumsOf<PermissionRequestStatus>(value) is { } statuses => query.Where(r => statuses.Contains(r.Status)),
                (FieldRequestUserId, "eq") when Guid.TryParse(value, out var user) => query.Where(r => r.RequestUserId == user),
                (FieldRoleId, "eq") when Guid.TryParse(value, out var role) => query.Where(r => r.RoleId == role),
                (FieldPermissionCode, "eq") => query.Where(r => r.PermissionCode == value),
                _ => null,
            };
            if (filtered is null)
            {
                return Error.Validation(AccessErrors.QueryUnsupported, AccessMessages.FilterUnsupported, MessageArguments.Of(("field", filter.Field), ("operator", filter.Operator)));
            }

            query = filtered;
        }

        if (request.Sorts.Count == 0)
        {
            query = query.OrderBy(static r => r.CreatedAt, OrderByType.Desc);
        }

        foreach (var sort in request.Sorts)
        {
            var direction = sort.Descending ? OrderByType.Desc : OrderByType.Asc;
            var sorted = sort.Field switch
            {
                FieldCreatedAt => query.OrderBy(static r => r.CreatedAt, direction),
                FieldStatus => query.OrderBy(static r => r.Status, direction),
                _ => null,
            };
            if (sorted is null)
            {
                return Error.Validation(AccessErrors.QueryUnsupported, AccessMessages.SortUnsupported, MessageArguments.Of(("field", sort.Field)));
            }

            query = sorted;
        }

        var total = new RefAsync<int>();
        var page = await query.ToPageListAsync(request.Page.Index, request.Page.Size, total, context.CancellationToken).ConfigureAwait(false);
        var progress = await ProgressAsync(page, context).ConfigureAwait(false);
        if (!progress.TryGetValue(out var map))
        {
            return progress.Error!;
        }

        return new PageResult<PermissionRequestResponse>(await store.ProjectAsync(page, map.TryGetValue(out var known) ? known : null, context).ConfigureAwait(false), total.Value, request.Page);
    }

    /// <summary>
    /// 取一条
    /// </summary>
    public async Task<Result<PermissionRequestResponse>> GetAsync(Guid id, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var found = await store.FindAsync(SideRows.SideOf(context), id, context.CancellationToken).ConfigureAwait(false);
        if (!found.TryGetValue(out var row))
        {
            return found.Error!;
        }

        var progress = await ProgressAsync([row], context).ConfigureAwait(false);
        if (!progress.TryGetValue(out var map))
        {
            return progress.Error!;
        }

        return (await store.ProjectAsync([row], map.TryGetValue(out var known) ? known : null, context).ConfigureAwait(false))[0];
    }

    /// <summary>
    /// 发起审批流程并回填实例
    /// </summary>
    /// <remarks>
    /// 发起时可能就走完了（结论回写已改了状态）：先重读一遍再回填，免得盖掉
    /// </remarks>
    private async Task<Result> StartAsync(IWorkflowBusinessService workflow, Guid id, PermissionRequestTarget target, Guid requester, InvocationContext context)
    {
        var variables = new JsonObject
        {
            ["requestUserId"] = requester.ToString(),
            ["roleId"] = target.Role?.Id.ToString(),
            ["roleCode"] = target.Role?.Code,
            ["permissionCode"] = target.Permission?.Code,
        };
        var started = await workflow.StartAsync(new WorkflowBusinessStartRequest(PermissionRequestStore.BusinessCode, id.ToString(), PermissionRequestStore.TitleOf(target, context), variables), context).ConfigureAwait(false);
        if (!started.TryGetValue(out var instance))
        {
            return started.Error!;
        }

        var reloaded = await store.FindAsync(SideRows.SideOf(context), id, context.CancellationToken).ConfigureAwait(false);
        if (!reloaded.TryGetValue(out var row))
        {
            return reloaded.Error!;
        }

        row.WorkflowInstanceId = instance.InstanceId;
        await store.Rows.UpdateAsync(row, context.CancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    /// <summary>
    /// 逐条置为完结状态，并撤回进行中的流程
    /// </summary>
    private async Task<Result<BatchResult>> EachAsync(IReadOnlyList<Guid> ids, InvocationContext context, Func<SysPermissionRequest, Task<Result>> check, PermissionRequestStatus status, string? remark, string reason, bool allowSame = false)
    {
        var side = SideRows.SideOf(context);
        var succeeded = new List<Guid>();
        var failed = new List<BatchFailure>();
        foreach (var id in ids.Distinct())
        {
            var found = await store.FindAsync(side, id, context.CancellationToken).ConfigureAwait(false);
            if (!found.TryGetValue(out var row))
            {
                failed.Add(BatchFailure.From(id, found.Error!));
                continue;
            }

            var allowed = await check(row).ConfigureAwait(false);
            if (allowed.IsFailure)
            {
                failed.Add(BatchFailure.From(id, allowed.Error!));
                continue;
            }

            if (allowSame && row.Status == status)
            {
                succeeded.Add(id);
                continue;
            }

            if (row.Status != PermissionRequestStatus.Pending)
            {
                failed.Add(new BatchFailure(id, AccessErrors.PermissionRequestNotPending, AccessMessages.PermissionRequestNotPending.L(("id", id), ("status", row.Status))));
                continue;
            }

            row.Status = status;
            row.Remark = remark ?? row.Remark;
            await store.Rows.UpdateAsync(row, context.CancellationToken).ConfigureAwait(false);
            if (Workflow is { } workflow)
            {
                var withdrawn = await workflow.WithdrawAsync(PermissionRequestStore.BusinessCode, id.ToString(), reason, context).ConfigureAwait(false);
                if (withdrawn.IsFailure)
                {
                    return withdrawn.Error!;
                }
            }

            succeeded.Add(id);
        }

        return new BatchResult(succeeded, failed);
    }

    private static Result OwnPending(SysPermissionRequest row, Guid me)
        => row.RequestUserId != me
            ? Result.Failure(Error.Denied(AccessErrors.PermissionRequestNotOwner, AccessMessages.PermissionRequestNotOwner))
            : row.Status != PermissionRequestStatus.Pending
                ? Result.Failure(Error.Conflict(AccessErrors.PermissionRequestNotPending, AccessMessages.PermissionRequestNotPending, MessageArguments.Of(("id", row.Id), ("status", row.Status))))
                : Result.Success();

    private async Task<Result<Maybe<IReadOnlyDictionary<string, WorkflowBusinessProgress>>>> ProgressAsync(IReadOnlyList<SysPermissionRequest> rows, InvocationContext context)
    {
        if (Workflow is not { } workflow)
        {
            return Maybe.None<IReadOnlyDictionary<string, WorkflowBusinessProgress>>();
        }

        var progress = await workflow.ProgressAsync(PermissionRequestStore.BusinessCode, [.. rows.Select(static r => r.Id.ToString())], context).ConfigureAwait(false);
        return progress.TryGetValue(out var map) ? Maybe.Some(map) : progress.Error!;
    }

    private static string? Optional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    /// <summary>
    /// 解析逗号分隔的枚举名
    /// </summary>
    /// <returns>枚举值；有一个认不出即为空（交给不支持的筛选报错）</returns>
    private static List<TEnum>? EnumsOf<TEnum>(string value) where TEnum : struct, Enum
    {
        var parts = value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var parsed = new List<TEnum>(parts.Length);
        foreach (var part in parts)
        {
            if (!Enum.TryParse<TEnum>(part, ignoreCase: true, out var item) || !Enum.IsDefined(item))
            {
                return null;
            }

            parsed.Add(item);
        }

        return parsed.Count == 0 ? null : parsed;
    }
}
