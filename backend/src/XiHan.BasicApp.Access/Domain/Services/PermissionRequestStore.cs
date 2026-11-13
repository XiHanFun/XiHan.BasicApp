// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Access.Domain.Entities;
using XiHan.BasicApp.Access.Infrastructure;
using XiHan.BasicApp.Access.Operations.PermissionRequests;
using XiHan.BasicApp.Access.Operations.Roles;
using XiHan.BasicApp.Contracts;
using XiHan.BasicApp.Contracts.Security;
using XiHan.BasicApp.Contracts.Users;
using XiHan.BasicApp.Contracts.Workflow;
using XiHan.BasicApp.Identity.Infrastructure;
using XiHan.BasicApp.Runtime.Persistence;
using XiHan.Framework.Kernel.Security;
using XiHan.Framework.Localization.Abstractions.Messages;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;
using XiHan.Framework.Security.Abstractions.Permissions;

namespace XiHan.BasicApp.Access.Domain.Services;

/// <summary>
/// 授权申请的对象：角色或权限（二选一）
/// </summary>
/// <param name="Role">申请的角色</param>
/// <param name="Permission">申请的权限</param>
public sealed record PermissionRequestTarget(SysRole? Role, PermissionDescriptor? Permission);

/// <summary>
/// 授权申请的读写与校验（作用域）
/// </summary>
/// <remarks>
/// 有效期合法且失效时间晚于现在、角色存在且启用、权限码在这一侧的目录里，同一申请人同一对象只能有一条待审批；
/// 对象二选一（用户已定）。模仿会话里对申请的写操作一律拒绝
/// 操作与审批业务处理器共用；它不依赖审批业务服务（处理器被审批业务服务构造时不成环）
/// </remarks>
/// <param name="rows">按侧读写</param>
/// <param name="roles">角色</param>
/// <param name="roleRows">角色仓储</param>
/// <param name="catalog">权限目录</param>
/// <param name="users">用户目录</param>
/// <param name="permissions">权限判定</param>
/// <param name="clock">时间源</param>
public sealed class PermissionRequestStore(SideRows rows, RoleService roles, IRepository<SysRole, Guid> roleRows, PermissionCatalog catalog, IUserDirectory users, IPermissionChecker permissions, TimeProvider clock)
{
    /// <summary>
    /// 审批业务码（也是审批流程定义名）
    /// </summary>
    public const string BusinessCode = "permission-request";

    /// <summary>
    /// 关键字按申请人匹配时，从用户目录最多取几个人
    /// </summary>
    public const int RequesterMatchLimit = 200;

    /// <summary>
    /// 按侧读写
    /// </summary>
    public SideRows Rows => rows;

    /// <summary>
    /// 模仿会话里拒绝对申请的写操作
    /// </summary>
    /// <remarks>
    /// 口径同个人中心与我的 OAuth 应用（冲突），按令牌带不带模仿者判定
    /// </remarks>
    public static Result RejectImpersonation(InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.GetSecurity().Principal.ClaimValues(IdentityClaims.Actor).Any()
            ? Error.Conflict(AccessErrors.PermissionRequestImpersonating, AccessMessages.PermissionRequestImpersonating)
            : Result.Success();
    }

    /// <summary>
    /// 关键字匹配的申请人
    /// </summary>
    /// <remarks>
    /// 经用户目录在这一侧按登录名与显示名搜（含停用的账号），按登录名取前 <see cref="RequesterMatchLimit"/> 个
    /// </remarks>
    public async Task<IReadOnlyList<Guid>> RequestersMatchingAsync(string side, string keyword, CancellationToken cancellationToken)
    {
        var found = await users.SearchInSideAsync(side, keyword, RequesterMatchLimit, cancellationToken).ConfigureAwait(false);
        return found.Select(static u => u.Id).ToList();
    }

    /// <summary>
    /// 按 ID 取这一侧的申请
    /// </summary>
    public async Task<Result<SysPermissionRequest>> FindAsync(string side, Guid id, CancellationToken cancellationToken)
    {
        var found = await rows.FindAsync<SysPermissionRequest>(side, id, cancellationToken).ConfigureAwait(false);
        return found.TryGetValue(out var request)
            ? request
            : Error.NotFound(AccessErrors.PermissionRequestNotFound, AccessMessages.PermissionRequestNotFound, MessageArguments.Of(("id", id)));
    }

    /// <summary>
    /// 校验申请的对象与有效期
    /// </summary>
    public async Task<Result<PermissionRequestTarget>> ValidateAsync(Guid? roleId, string? permissionCode, DateTimeOffset? effectiveAt, DateTimeOffset? expiresAt, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (roleId is null && string.IsNullOrWhiteSpace(permissionCode))
        {
            return Error.Validation(AccessErrors.PermissionRequestTargetRequired, AccessMessages.PermissionRequestTargetRequired);
        }

        if (roleId is not null && !string.IsNullOrWhiteSpace(permissionCode))
        {
            return Error.Validation(AccessErrors.PermissionRequestTargetExclusive, AccessMessages.PermissionRequestTargetExclusive);
        }

        var window = GrantWindow.Validate(effectiveAt, expiresAt);
        if (window.IsFailure)
        {
            return window.Error!;
        }

        if (expiresAt is { } expires && expires <= clock.GetUtcNow())
        {
            return Error.Validation(AccessErrors.PermissionRequestExpiryPassed, AccessMessages.PermissionRequestExpiryPassed);
        }

        return await TargetAsync(roleId, permissionCode, context).ConfigureAwait(false);
    }

    /// <summary>
    /// 申请的对象此刻是否还可用（提交时与审批通过时都核对）
    /// </summary>
    public async Task<Result<PermissionRequestTarget>> TargetAsync(Guid? roleId, string? permissionCode, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var owner = RoleService.OwnerOf(context);
        SysRole? role = null;
        if (roleId is { } id)
        {
            if (!(await roles.FindAsync(id, owner, context.CancellationToken).ConfigureAwait(false)).TryGetValue(out var found))
            {
                return Error.NotFound(AccessErrors.RoleNotFound, AccessMessages.RoleNotFound, MessageArguments.Of(("id", id)));
            }

            if (found.Status != RoleStatus.Active)
            {
                return Error.Validation(AccessErrors.PermissionRequestRoleDisabled, AccessMessages.PermissionRequestRoleDisabled, MessageArguments.Of(("id", id), ("name", found.Name)));
            }

            role = found;
        }

        PermissionDescriptor? permission = null;
        if (!string.IsNullOrWhiteSpace(permissionCode))
        {
            var code = permissionCode.Trim();
            permission = catalog.Find(code);
            if (permission is null || !PermissionCatalog.AllowsSide(permission, string.Equals(owner, TenantIds.Platform, StringComparison.Ordinal)))
            {
                return Error.Validation(AccessErrors.PermissionRequestPermissionUnknown, AccessMessages.PermissionRequestPermissionUnknown, MessageArguments.Of(("code", code)));
            }
        }

        return new PermissionRequestTarget(role, permission);
    }

    /// <summary>
    /// 申请人要是这一侧的在用账号
    /// </summary>
    public async Task<Result<Guid>> RequesterAsync(InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (Guid.TryParse(context.GetSecurity().Principal.Id, out var me)
            && (await users.ActiveInSideAsync(SideRows.SideOf(context), [me], context.CancellationToken).ConfigureAwait(false)).Contains(me))
        {
            return me;
        }

        return Error.Denied(AccessErrors.PermissionRequestRequesterInactive, AccessMessages.PermissionRequestRequesterInactive);
    }

    /// <summary>
    /// 同一申请人同一对象只能有一条待审批的申请
    /// </summary>
    public async Task<Result> EnsureNoDuplicateAsync(string side, Guid requester, PermissionRequestTarget target, Guid? except, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(target);
        var roleId = target.Role?.Id;
        var code = target.Permission?.Code;
        var pending = await rows.Query<SysPermissionRequest>(side)
            .Where(r => r.RequestUserId == requester && r.Status == PermissionRequestStatus.Pending && r.RoleId == roleId && r.PermissionCode == code)
            .Select(static r => r.Id)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        return pending.Any(id => id != except)
            ? Error.Conflict(AccessErrors.PermissionRequestDuplicate, AccessMessages.PermissionRequestDuplicate)
            : Result.Success();
    }

    /// <summary>
    /// 处理人能否授出申请的对象
    /// </summary>
    /// <remarks>
    /// 角色要能指派（<c>access.roles.assign</c>），权限要能直授（<c>access.user-grants.grant</c>）
    /// </remarks>
    public async Task<Result> CanGrantAsync(SysPermissionRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var required = new List<string>(2);
        if (request.RoleId is not null)
        {
            required.Add(AccessPermissions.RolesAssign);
        }

        if (request.PermissionCode is not null)
        {
            required.Add(AccessPermissions.UserGrantsGrant);
        }

        foreach (var permission in required)
        {
            var held = await permissions.HasPermissionAsync(context.GetSecurity(), permission, context.CancellationToken).ConfigureAwait(false);
            if (!held.TryGetValue(out var granted))
            {
                return held.Error!;
            }

            if (!granted)
            {
                return Error.Denied(AccessErrors.PermissionRequestCannotGrant, AccessMessages.PermissionRequestCannotGrant, MessageArguments.Of(("permission", permission)));
            }
        }

        return Result.Success();
    }

    /// <summary>
    /// 审批流程的标题
    /// </summary>
    /// <remarks>
    /// 本人提交时只写申请对象：申请人就是发起人，待办与审批中心另有发起人一栏；替别人发起时发起人不是申请人，标题里带上申请人
    /// </remarks>
    /// <param name="target">申请的对象</param>
    /// <param name="context">调用上下文</param>
    /// <param name="applicant">替别人发起时写进标题的申请人显示名；本人提交为空</param>
    public static string TitleOf(PermissionRequestTarget target, InvocationContext context, string? applicant = null)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(context);
        var parts = new List<string>(2);
        if (target.Role is { } role)
        {
            parts.Add(context.Localize(AccessMessages.PermissionRequestTargetRole, MessageArguments.Of(("name", role.Name))));
        }

        if (target.Permission is { } permission)
        {
            parts.Add(context.Localize(AccessMessages.PermissionRequestTargetPermission, MessageArguments.Of(("name", PermissionTexts.NameOf(permission, context)))));
        }

        var targets = string.Join(context.Localize(AccessMessages.PermissionRequestTargetSeparator), parts);
        return applicant is null
            ? context.Localize(AccessMessages.PermissionRequestWorkflowTitle, MessageArguments.Of(("targets", targets)))
            : context.Localize(AccessMessages.PermissionRequestWorkflowTitleFor, MessageArguments.Of(("user", applicant), ("targets", targets)));
    }

    /// <summary>
    /// 用户的显示名（没有显示名用登录名）
    /// </summary>
    public async Task<string> DisplayNameAsync(Guid userId, CancellationToken cancellationToken)
    {
        var found = await users.FindAsync([userId], cancellationToken).ConfigureAwait(false);
        return found.TryGetValue(userId, out var user)
            ? (string.IsNullOrWhiteSpace(user.DisplayName) ? user.UserName : user.DisplayName)
            : userId.ToString("D");
    }

    /// <summary>
    /// 投影成响应：补申请人、角色与权限的名字，接上审批进度
    /// </summary>
    public async Task<IReadOnlyList<PermissionRequestResponse>> ProjectAsync(IReadOnlyList<SysPermissionRequest> items, IReadOnlyDictionary<string, WorkflowBusinessProgress>? progress, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(context);
        var names = await users.FindAsync([.. items.Select(static r => r.RequestUserId).Distinct()], context.CancellationToken).ConfigureAwait(false);
        var roleIds = items.Select(static r => r.RoleId).OfType<Guid>().Distinct().ToList();
        var roleMap = roleIds.Count == 0 ? new Dictionary<Guid, SysRole>() : (await roleRows.FindManyAsync(roleIds, context.CancellationToken).ConfigureAwait(false)).ToDictionary(static r => r.Id);
        var now = clock.GetUtcNow();
        return items.Select(r =>
        {
            var role = r.RoleId is { } roleId ? roleMap.GetValueOrDefault(roleId) : null;
            var permission = r.PermissionCode is { } code ? catalog.Find(code) : null;
            var workflow = progress?.GetValueOrDefault(r.Id.ToString());
            return new PermissionRequestResponse(
                r.Id,
                r.RequestUserId,
                names.GetValueOrDefault(r.RequestUserId)?.DisplayName,
                r.RoleId,
                role?.Code,
                role?.Name,
                role?.Description,
                r.PermissionCode,
                permission is null ? null : PermissionTexts.NameOf(permission, context),
                permission is null ? null : PermissionTexts.DescriptionOf(permission, context),
                r.Status,
                r.Reason,
                r.ExpectedEffectiveAt,
                r.ExpectedExpiresAt,
                r.ExpectedExpiresAt is { } expires && expires <= now,
                r.WorkflowInstanceId,
                workflow is null ? null : new PermissionRequestProgress(workflow.InstanceId, workflow.Status, workflow.Outcome, workflow.PendingNodeNames, [.. workflow.PendingAssignees.Select(static a => new PermissionRequestAssignee(a.UserId, a.DisplayName))], workflow.FaultMessage, workflow.StartedAt, workflow.EndedAt),
                progress is not null,
                r.Remark,
                r.CreatedAt,
                r.ModifiedAt);
        }).ToList();
    }
}
