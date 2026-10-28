// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using XiHan.BasicApp.Access.Domain.Entities;
using XiHan.BasicApp.Access.Domain.Services;
using XiHan.BasicApp.Access.Operations.Roles;
using XiHan.BasicApp.Access.Operations.UserGrants;
using XiHan.BasicApp.Contracts.Auditing;
using XiHan.BasicApp.Identity.Domain.Entities;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Access.Operations.UserDataScopes;

/// <summary>
/// <c>user-data-scopes.get</c> 的请求：一个用户（查询串 <c>userId</c>）。
/// </summary>
/// <param name="UserId">用户 ID。</param>
public sealed record UserDataScopeRequest([Required] Guid UserId);

/// <summary>
/// <c>user-data-scopes.set</c> 的请求：覆盖为一种数据范围，或清除覆盖回到按角色。
/// </summary>
/// <param name="UserId">用户 ID；注册地须是当前租户。</param>
/// <param name="DataScope">覆盖后的数据范围；为空即清除覆盖、按角色。</param>
/// <param name="Departments">自定义范围的部门（只有 <c>Custom</c> 时给，至少一个）。</param>
public sealed record SetUserDataScopeRequest([Required] Guid UserId, DataScope? DataScope = null, IReadOnlyList<DataScopeDepartmentRequest>? Departments = null);

/// <summary>
/// 一个用户的数据范围设置。
/// </summary>
/// <param name="UserId">用户 ID。</param>
/// <param name="DataScope">覆盖后的数据范围；为空即按角色。</param>
/// <param name="Departments">自定义范围的部门。</param>
public sealed record UserDataScopeResponse(Guid UserId, DataScope? DataScope, IReadOnlyList<DataScopeDepartmentRequest> Departments);

/// <summary>
/// 一个用户的数据范围覆盖：<c>GET /api/user-data-scopes?userId=</c>。只在租户侧。
/// </summary>
/// <param name="users">用户仓储。</param>
/// <param name="scopes">用户级数据范围覆盖服务。</param>
[Operation(Id, Stability = Stability.Preview, ReadOnly = true, Expose = Expose.Http, Permission = AccessPermissions.UserDataScopesRead)]
public sealed class GetUserDataScope(IRepository<SysUser, Guid> users, UserDataScopeService scopes) : IOperation<UserDataScopeRequest, UserDataScopeResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "user-data-scopes.get";

    /// <inheritdoc />
    public async ValueTask<Result<UserDataScopeResponse>> HandleAsync(UserDataScopeRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var owner = RoleService.OwnerOf(context);
        if (RoleService.IsPlatform(owner))
        {
            return Error.Validation(AccessErrors.UserDataScopeTenantOnly, AccessMessages.UserDataScopeTenantOnly);
        }

        var found = await UserGrantBatch.FindUserAsync(users, request.UserId, owner, context.CancellationToken).ConfigureAwait(false);
        if (!found.TryGetValue(out var user))
        {
            return found.Error!;
        }

        return (await scopes.FindAsync(owner, user.Id, context.CancellationToken).ConfigureAwait(false)).TryGetValue(out var view)
            ? new UserDataScopeResponse(user.Id, view.Scope.DataScope, view.Departments.Select(static d => new DataScopeDepartmentRequest(d.DepartmentId, d.IncludeChildren)).ToList())
            : new UserDataScopeResponse(user.Id, null, []);
    }
}

/// <summary>
/// 设置用户的数据范围覆盖：<c>POST /api/user-data-scopes:set</c>。覆盖完全取代该用户所有角色的数据范围（不取并集）；不带数据范围即清除覆盖。只在租户侧。
/// </summary>
/// <param name="users">用户仓储。</param>
/// <param name="scopes">用户级数据范围覆盖服务。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = AccessPermissions.UserDataScopesUpdate)]
public sealed class SetUserDataScope(IRepository<SysUser, Guid> users, UserDataScopeService scopes) : IOperation<SetUserDataScopeRequest, UserDataScopeResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "user-data-scopes.set";

    /// <inheritdoc />
    public async ValueTask<Result<UserDataScopeResponse>> HandleAsync(SetUserDataScopeRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var owner = RoleService.OwnerOf(context);
        if (RoleService.IsPlatform(owner))
        {
            return Error.Validation(AccessErrors.UserDataScopeTenantOnly, AccessMessages.UserDataScopeTenantOnly);
        }

        var found = await UserGrantBatch.FindUserAsync(users, request.UserId, owner, context.CancellationToken).ConfigureAwait(false);
        if (!found.TryGetValue(out var user))
        {
            return found.Error!;
        }

        var departments = request.Departments ?? [];
        var set = await scopes.SetAsync(owner, user.Id, request.DataScope, departments, context.CancellationToken).ConfigureAwait(false);
        if (set.IsFailure)
        {
            return set.Error!;
        }

        context.AddAuditLog(new PermissionChangeLogEntry(PermissionChangeType.UserDataScope) { TargetUserId = user.Id, TargetUserName = user.UserName, Detail = $"{request.DataScope?.ToString() ?? "roles"} ({departments.Count})" });
        return new UserDataScopeResponse(user.Id, request.DataScope, departments.GroupBy(static d => d.DepartmentId).Select(static g => g.Last()).ToList());
    }
}
