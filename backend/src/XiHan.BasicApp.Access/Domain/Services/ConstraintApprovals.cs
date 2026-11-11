// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Access.Operations.Roles;
using XiHan.BasicApp.Access.Operations.UserGrants;
using XiHan.BasicApp.Contracts.Batch;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Access.Domain.Services;

/// <summary>
/// 因约束规则要求审批而没做的一项
/// </summary>
/// <param name="UserId">用户</param>
/// <param name="PermissionCode">直授的权限码；指派角色时为空</param>
/// <param name="Reason"><c>ACCESS_CONFLICT_018</c>，参数里是规则的编码与名称</param>
public sealed record ApprovalNeeded(Guid UserId, string? PermissionCode, Error Reason);

/// <summary>
/// 给角色指派成员的结果
/// </summary>
/// <param name="Batch">逐个成员的结果；要求审批的成员按 <c>ACCESS_CONFLICT_018</c> 记在失败里</param>
/// <param name="NeedApproval">其中要求审批的成员</param>
public sealed record RoleAssignmentResult(BatchResult Batch, IReadOnlyList<ApprovalNeeded> NeedApproval);

/// <summary>
/// 给用户直授的结果
/// </summary>
/// <param name="Batch">逐个权限码的结果；要求审批的权限码按 <c>ACCESS_CONFLICT_018</c> 记在失败里</param>
/// <param name="NeedApproval">其中要求审批的权限码</param>
public sealed record UserGrantResult(PermissionBatchResult Batch, IReadOnlyList<ApprovalNeeded> NeedApproval);

/// <summary>
/// 把要求审批的指派与直授转为授权申请
/// </summary>
/// <remarks>
/// 管理员直接给用户指派角色或直授权限、触犯「需审批」的约束规则时，替这个用户发起一条授权申请：这一项在批量结果里换成
/// <c>ACCESS_CONFLICT_019</c>（已转审批，审批通过后按原内容生效）；发起不了（审批未启用、已有同样的待审批申请、对象已不可申请）时换成对应的失败
/// 放在操作层而不进指派与直授的领域服务：审批通过后的授予也经那两个服务，它们再依赖授权申请会让审批业务的依赖成环
/// </remarks>
/// <param name="requests">授权申请</param>
public sealed class ConstraintApprovalRouter(PermissionRequestService requests)
{
    /// <summary>
    /// 指派角色里要求审批的成员转为授权申请
    /// </summary>
    public async Task<BatchResult> RouteAsync(RoleMembersRequest request, RoleAssignmentResult result, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(result);
        if (result.NeedApproval.Count == 0)
        {
            return result.Batch;
        }

        var routed = result.NeedApproval.Select(static n => n.UserId).ToHashSet();
        var failed = result.Batch.Failed.Where(f => !routed.Contains(f.Id)).ToList();
        foreach (var item in result.NeedApproval)
        {
            var submitted = await requests.SubmitForAsync(item.UserId, request.Id, null, request.EffectiveAt, request.ExpiresAt, item.Reason, context).ConfigureAwait(false);
            failed.Add(BatchFailure.From(item.UserId, Outcome(submitted, item.Reason)));
        }

        return result.Batch with { Failed = failed };
    }

    /// <summary>
    /// 直授里要求审批的权限码转为授权申请
    /// </summary>
    public async Task<PermissionBatchResult> RouteAsync(UserPermissionsRequest request, UserGrantResult result, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(result);
        if (result.NeedApproval.Count == 0)
        {
            return result.Batch;
        }

        var routed = result.NeedApproval.Select(static n => n.PermissionCode).ToHashSet(StringComparer.Ordinal);
        var failed = result.Batch.Failed.Where(f => !routed.Contains(f.Permission)).ToList();
        foreach (var item in result.NeedApproval)
        {
            var submitted = await requests.SubmitForAsync(item.UserId, null, item.PermissionCode, request.EffectiveAt, request.ExpiresAt, item.Reason, context).ConfigureAwait(false);
            failed.Add(PermissionFailure.From(item.PermissionCode!, Outcome(submitted, item.Reason)));
        }

        return result.Batch with { Failed = failed };
    }

    // 发起了即「已转审批」；审批未启用时说明是哪条规则要求审批；其余失败照原样
    private static Error Outcome(Result<Guid> submitted, Error reason)
    {
        if (submitted.TryGetValue(out var requestId))
        {
            return Error.Conflict(AccessErrors.ConstraintApprovalSubmitted, AccessMessages.ConstraintApprovalSubmitted, MessageArguments.Of(("code", reason.Details?["code"]), ("name", reason.Details?["name"]), ("requestId", requestId)));
        }

        return submitted.Error!.Code == AccessErrors.ApprovalUnavailable
            ? Error.Unavailable(AccessErrors.ApprovalUnavailable, AccessMessages.ConstraintApprovalUnavailable, reason.Details)
            : submitted.Error!;
    }
}
