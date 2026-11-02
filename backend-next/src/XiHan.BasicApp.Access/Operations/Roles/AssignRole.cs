// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Access.Domain.Services;
using XiHan.BasicApp.Contracts.Batch;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Access.Operations.Roles;

/// <summary>
/// 给角色指派成员。
/// </summary>
/// <remarks>
/// <c>POST /api/roles/{id}:assign</c>。用户须与角色同一侧（注册地 = 角色所属侧）；已是成员的算成功。
/// 指派最迟在用户下一次刷新令牌时生效（角色进 <c>role</c> 声明）。新成员逐个过约束规则（静态职责分离、基数与条件），拦截类的违规记为这一人失败（<c>ACCESS_CONFLICT_012</c>）。
/// </remarks>
/// <param name="assignments">角色指派。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = AccessPermissions.RolesAssign)]
public sealed class AssignRole(RoleAssignmentService assignments) : IOperation<RoleMembersRequest, BatchResult>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "roles.assign";

    /// <inheritdoc />
    public async ValueTask<Result<BatchResult>> HandleAsync(RoleMembersRequest request, InvocationContext context)
        => await assignments.AssignAsync(request, context).ConfigureAwait(false);
}
