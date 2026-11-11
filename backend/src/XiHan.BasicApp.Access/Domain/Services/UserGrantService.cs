// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Access.Domain.Entities;
using XiHan.BasicApp.Access.Infrastructure;
using XiHan.BasicApp.Access.Operations.Roles;
using XiHan.BasicApp.Access.Operations.UserGrants;
using XiHan.BasicApp.Contracts.Auditing;
using XiHan.BasicApp.Identity.Domain.Services;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Access.Domain.Services;

/// <summary>
/// 给用户直接授予
/// </summary>
/// <remarks>
/// 直授的规则（权限目录与作用侧、约束规则的互斥筛查、有效期与理由、去掉同码的禁止、权限变更日志）集中在这里：
/// <c>user-grants.grant</c> 与审批通过后的授予（授权申请）都经它，规则只有一份
/// </remarks>
/// <param name="users">这一侧的用户</param>
/// <param name="catalog">权限目录</param>
/// <param name="grants">用户授予</param>
/// <param name="rules">授予规则（有效期与理由）</param>
/// <param name="conditions">权限条件</param>
/// <param name="enforcer">约束规则求值</param>
public sealed class UserGrantService(SideUsers users, PermissionCatalog catalog, Infrastructure.UserGrants grants, PermissionRuleWriter rules, PermissionConditionService conditions, ConstraintEnforcer enforcer)
{
    /// <summary>
    /// 给用户直接授予
    /// </summary>
    /// <param name="request">用户、权限码、有效期与理由</param>
    /// <param name="context">调用上下文</param>
    /// <param name="approvedBy">批准这次授予的授权申请（审批通过后执行时给出，需审批的约束规则随之放行）</param>
    /// <returns>逐个权限码的结果与其中要求审批的权限码；用户不存在或有效期不合法时整体失败</returns>
    public async Task<Result<UserGrantResult>> GrantAsync(UserPermissionsRequest request, InvocationContext context, Guid? approvedBy = null)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var window = GrantWindow.Validate(request.EffectiveAt, request.ExpiresAt);
        if (window.IsFailure)
        {
            return window.Error!;
        }

        var needApproval = new List<ApprovalNeeded>();
        var granted = await UserGrantBatch.RunAsync(request, context, users, catalog, PermissionChangeType.UserGrant, async (user, owner, permissions, ct) =>
        {
            var outcomes = await grants.GrantAsync(user.Id, owner, permissions, ct).ConfigureAwait(false);
            var ok = outcomes.Where(static o => o.Outcome.IsSuccess).Select(static o => o.Grant.Permission).ToList();
            (await rules.SetAsync(owner, GrantSubjectKind.User, user.Id, ok, PermissionAction.Grant, request.EffectiveAt, request.ExpiresAt, request.Reason, ct).ConfigureAwait(false)).ThrowIfFailure();
            (await conditions.RemoveBindingsAsync(owner, GrantSubjectKind.User, user.Id, ok, PermissionAction.Deny, ct).ConfigureAwait(false)).ThrowIfFailure();
            return outcomes;
        }, async (user, owner, accepted, ct) =>
        {
            var blocked = await enforcer.ScreenPermissionsAsync(owner, [.. await grants.ListAsync(user.Id, owner, ct).ConfigureAwait(false)], accepted, context, userId: user.Id, approvedBy: approvedBy).ConfigureAwait(false);
            needApproval.AddRange(blocked.Where(static b => b.Reason.Code == AccessErrors.ConstraintApprovalRequired).Select(b => new ApprovalNeeded(user.Id, b.Permission, b.Reason)));
            return blocked;
        }).ConfigureAwait(false);
        return granted.TryGetValue(out var batch) ? new UserGrantResult(batch, needApproval) : granted.Error!;
    }
}
