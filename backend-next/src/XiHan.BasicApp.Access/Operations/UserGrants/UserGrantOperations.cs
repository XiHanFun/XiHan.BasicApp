// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Access.Domain.Entities;
using XiHan.BasicApp.Access.Domain.Services;
using XiHan.BasicApp.Access.Infrastructure;
using XiHan.BasicApp.Access.Operations.Roles;
using XiHan.BasicApp.Contracts.Auditing;
using XiHan.BasicApp.Identity.Domain.Entities;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;
using XiHan.Framework.Security.Database.Grants;

namespace XiHan.BasicApp.Access.Operations.UserGrants;

/// <summary>
/// 一个用户在当前这一侧的直接授权：<c>GET /api/user-grants?userId=</c>。授予来自框架授予存储（含别的包直接写入的，如超管的 <c>*</c>），拒绝与有效期来自授予规则。
/// </summary>
/// <param name="users">用户仓储。</param>
/// <param name="grants">用户直接授予。</param>
/// <param name="rules">授予规则。</param>
[Operation(Id, Stability = Stability.Preview, ReadOnly = true, Expose = Expose.Http, Permission = AccessPermissions.UserGrantsRead)]
public sealed class ListUserGrants(IRepository<SysUser, Guid> users, Infrastructure.UserGrants grants, PermissionRuleWriter rules) : IOperation<UserGrantsRequest, UserGrantsResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "user-grants.list";

    /// <inheritdoc />
    public async ValueTask<Result<UserGrantsResponse>> HandleAsync(UserGrantsRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var owner = RoleService.OwnerOf(context);
        var found = await UserGrantBatch.FindUserAsync(users, request.UserId, owner, context.CancellationToken).ConfigureAwait(false);
        if (!found.TryGetValue(out var user))
        {
            return found.Error!;
        }

        var granted = await grants.ListAsync(user.Id, owner, context.CancellationToken).ConfigureAwait(false);
        var ruleRows = (await rules.ListAsync(owner, GrantSubjectKind.User, user.Id, context.CancellationToken).ConfigureAwait(false)).ToDictionary(static r => r.Permission, StringComparer.Ordinal);
        var items = granted
            .Select(p => ruleRows.TryGetValue(p, out var rule) && rule.Action == PermissionAction.Grant
                ? new UserGrantResponse(p, PermissionAction.Grant, rule.EffectiveAt, rule.ExpiresAt, rule.Reason)
                : new UserGrantResponse(p, PermissionAction.Grant, null, null, null))
            .Concat(ruleRows.Values.Where(static r => r.Action == PermissionAction.Deny).Select(static r => new UserGrantResponse(r.Permission, PermissionAction.Deny, r.EffectiveAt, r.ExpiresAt, r.Reason)))
            .OrderBy(static g => g.Permission, StringComparer.Ordinal)
            .ToList();
        return new UserGrantsResponse(user.Id, items);
    }
}

/// <summary>
/// 给用户直接授予：<c>POST /api/user-grants:grant</c>。权限码须在目录里且作用侧相符；已有的授予算成功。带有效期或原因时写 Grant 规则（原来的 Deny 一并改成授予）。
/// </summary>
/// <param name="users">用户仓储。</param>
/// <param name="catalog">权限目录。</param>
/// <param name="grants">用户直接授予。</param>
/// <param name="rules">授予规则。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = AccessPermissions.UserGrantsGrant)]
public sealed class GrantUser(IRepository<SysUser, Guid> users, PermissionCatalog catalog, Infrastructure.UserGrants grants, PermissionRuleWriter rules) : IOperation<UserPermissionsRequest, PermissionBatchResult>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "user-grants.grant";

    /// <inheritdoc />
    public async ValueTask<Result<PermissionBatchResult>> HandleAsync(UserPermissionsRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var window = GrantWindow.Validate(request.EffectiveAt, request.ExpiresAt);
        if (window.IsFailure)
        {
            return window.Error!;
        }

        return await UserGrantBatch.RunAsync(request, context, users, catalog, PermissionChangeType.UserGrant, async (user, owner, permissions, ct) =>
        {
            var outcomes = await grants.GrantAsync(user.Id, owner, permissions, ct).ConfigureAwait(false);
            var ok = outcomes.Where(static o => o.Outcome.IsSuccess).Select(static o => o.Grant.Permission).ToList();
            (await rules.SetAsync(owner, GrantSubjectKind.User, user.Id, ok, PermissionAction.Grant, request.EffectiveAt, request.ExpiresAt, request.Reason, ct).ConfigureAwait(false)).ThrowIfFailure();
            return outcomes;
        }).ConfigureAwait(false);
    }
}

/// <summary>
/// 给用户直接拒绝：<c>POST /api/user-grants:deny</c>。用户上的 Deny 压过一切授予（角色、直接授予）；同码的直接授予撤掉，换成 Deny 规则（可带有效期与原因）。
/// </summary>
/// <param name="users">用户仓储。</param>
/// <param name="catalog">权限目录。</param>
/// <param name="grants">用户直接授予。</param>
/// <param name="rules">授予规则。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = AccessPermissions.UserGrantsGrant)]
public sealed class DenyUser(IRepository<SysUser, Guid> users, PermissionCatalog catalog, Infrastructure.UserGrants grants, PermissionRuleWriter rules) : IOperation<UserPermissionsRequest, PermissionBatchResult>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "user-grants.deny";

    /// <inheritdoc />
    public async ValueTask<Result<PermissionBatchResult>> HandleAsync(UserPermissionsRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var window = GrantWindow.Validate(request.EffectiveAt, request.ExpiresAt);
        if (window.IsFailure)
        {
            return window.Error!;
        }

        return await UserGrantBatch.RunAsync(request, context, users, catalog, PermissionChangeType.UserDeny, async (user, owner, permissions, ct) =>
        {
            await grants.RevokeAsync(user.Id, owner, permissions, ct).ConfigureAwait(false);
            (await rules.SetAsync(owner, GrantSubjectKind.User, user.Id, permissions, PermissionAction.Deny, request.EffectiveAt, request.ExpiresAt, request.Reason, ct).ConfigureAwait(false)).ThrowIfFailure();
            return permissions.Select(p => new PermissionGrantResult(new XiHan.Framework.Security.Abstractions.Permissions.PermissionGrant(Infrastructure.UserGrants.SubjectOf(user.Id), p, Infrastructure.UserGrants.TenantOf(owner)), Result.Success())).ToList();
        }).ConfigureAwait(false);
    }
}

/// <summary>
/// 撤销用户的直接授予或拒绝：<c>POST /api/user-grants:revoke</c>。连同这些权限码上的规则一起删；只有 Deny 的撤销即解除拒绝，算成功；两者都没有的是 NotFound。
/// </summary>
/// <param name="users">用户仓储。</param>
/// <param name="catalog">权限目录。</param>
/// <param name="grants">用户直接授予。</param>
/// <param name="rules">授予规则。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = AccessPermissions.UserGrantsGrant)]
public sealed class RevokeUser(IRepository<SysUser, Guid> users, PermissionCatalog catalog, Infrastructure.UserGrants grants, PermissionRuleWriter rules) : IOperation<UserPermissionsRequest, PermissionBatchResult>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "user-grants.revoke";

    /// <inheritdoc />
    public ValueTask<Result<PermissionBatchResult>> HandleAsync(UserPermissionsRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        return UserGrantBatch.RunAsync(request, context, users, catalog, PermissionChangeType.UserRevoke, async (user, owner, permissions, ct) =>
        {
            var outcomes = await grants.RevokeAsync(user.Id, owner, permissions, ct).ConfigureAwait(false);
            var removed = await rules.RemoveAsync(owner, GrantSubjectKind.User, user.Id, permissions, ct).ConfigureAwait(false);
            var cleared = removed.ValueOrThrow();
            return outcomes.Select(o => o.Outcome.IsFailure && o.Outcome.Error!.Kind == "NOTFOUND" && cleared.Contains(o.Grant.Permission) ? o with { Outcome = Result.Success() } : o).ToList();
        });
    }
}
