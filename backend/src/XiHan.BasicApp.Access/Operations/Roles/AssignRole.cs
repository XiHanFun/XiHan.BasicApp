// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Access.Domain.Services;
using XiHan.BasicApp.Contracts.Batch;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Access.Operations.Roles;

/// <summary>
/// 给角色指派成员
/// </summary>
/// <remarks>
/// <c>POST /api/roles/{id}:assign</c>。用户须与角色同一侧（注册地 = 角色所属侧）；已是成员的算成功
/// 指派最迟在用户下一次刷新令牌时生效（角色进 <c>role</c> 声明）。新成员逐个过约束规则（静态职责分离、基数与条件）：触犯拒绝类的记为这一人失败（<c>ACCESS_CONFLICT_012</c>）；
/// 触犯需审批类的不指派，替这一人发起授权申请，记为 <c>ACCESS_CONFLICT_019</c>（已转审批，审批通过后按这次的有效期生效）
/// </remarks>
/// <param name="assignments">角色指派</param>
/// <param name="approvals">转审批</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = AccessPermissions.RolesAssign)]
public sealed class AssignRole(RoleAssignmentService assignments, ConstraintApprovalRouter approvals) : IOperation<RoleMembersRequest, BatchResult>
{
    /// <summary>
    /// 操作 ID
    /// </summary>
    public const string Id = "roles.assign";

    /// <inheritdoc />
    public async ValueTask<Result<BatchResult>> HandleAsync(RoleMembersRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        var assigned = await assignments.AssignAsync(request, context).ConfigureAwait(false);
        return assigned.TryGetValue(out var result) ? await approvals.RouteAsync(request, result, context).ConfigureAwait(false) : assigned.Error!;
    }
}
