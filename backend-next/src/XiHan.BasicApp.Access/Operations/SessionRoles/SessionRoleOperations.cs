// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using XiHan.BasicApp.Access.Domain.Entities;
using XiHan.BasicApp.Access.Domain.Services;
using XiHan.BasicApp.Access.Queries;
using XiHan.BasicApp.Contracts.Paging;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Access.Operations.SessionRoles;

/// <summary>
/// 一行会话角色。
/// </summary>
/// <param name="Id">记录 ID。</param>
/// <param name="SessionId">会话。</param>
/// <param name="UserId">用户。</param>
/// <param name="UserName">登录名。</param>
/// <param name="RoleId">角色。</param>
/// <param name="RoleName">角色名称；角色已删除为空。</param>
/// <param name="RoleCode">角色编码。</param>
/// <param name="Status">状态。</param>
/// <param name="ActivatedAt">激活时间。</param>
/// <param name="DeactivatedAt">停用或到期的时间。</param>
/// <param name="Reason">原因。</param>
public sealed record SessionRoleResponse(Guid Id, Guid SessionId, Guid UserId, string? UserName, Guid RoleId, string? RoleName, string? RoleCode, SessionRoleStatus Status, DateTimeOffset ActivatedAt, DateTimeOffset? DeactivatedAt, string? Reason);

/// <summary>
/// <c>session-roles.get</c> 的请求。
/// </summary>
/// <param name="Id">记录 ID。</param>
public sealed record GetSessionRoleRequest([Required] Guid Id);

/// <summary>
/// 分页列出当前一侧的会话角色。
/// </summary>
/// <remarks>
/// <c>POST /api/session-roles:list</c>（4.x 的 SessionRoleQueryService，只读）。缺省按激活时间倒序。
/// </remarks>
/// <param name="queries">会话角色查询。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = AccessPermissions.SessionRolesRead)]
public sealed class ListSessionRoles(SessionRoleQueries queries) : IOperation<PageRequest, PageResult<SessionRoleResponse>>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "session-roles.list";

    /// <inheritdoc />
    public ValueTask<Result<PageResult<SessionRoleResponse>>> HandleAsync(PageRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        return queries.ListAsync(request, context);
    }
}

/// <summary>
/// 按 ID 取当前一侧的一行会话角色。
/// </summary>
/// <remarks>
/// <c>GET /api/session-roles/{id}</c>。
/// </remarks>
/// <param name="queries">会话角色查询。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = AccessPermissions.SessionRolesRead)]
public sealed class GetSessionRole(SessionRoleQueries queries) : IOperation<GetSessionRoleRequest, SessionRoleResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "session-roles.get";

    /// <inheritdoc />
    public async ValueTask<Result<SessionRoleResponse>> HandleAsync(GetSessionRoleRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        return await queries.GetAsync(request.Id, RoleService.OwnerOf(context), context.CancellationToken).ConfigureAwait(false) is { } found
            ? found
            : Error.NotFound(AccessErrors.SessionRoleNotFound, AccessMessages.SessionRoleNotFound, MessageArguments.Of(("id", request.Id)));
    }
}
