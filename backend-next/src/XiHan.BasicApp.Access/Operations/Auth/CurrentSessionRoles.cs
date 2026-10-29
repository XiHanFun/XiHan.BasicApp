// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Access.Domain.Entities;
using XiHan.BasicApp.Contracts.Security;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Kernel.Security;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;
using XiHan.Framework.Security.Abstractions;
using SecurityPolicies = XiHan.BasicApp.Contracts.Security.Policies;

namespace XiHan.BasicApp.Access.Operations.Auth;

/// <summary>
/// <c>auth.session-roles</c> 的请求（无参数）。
/// </summary>
public sealed record SessionRolesRequest;

/// <summary>
/// 当前会话可激活的一个角色。
/// </summary>
/// <param name="RoleId">角色 ID。</param>
/// <param name="Code">编码。</param>
/// <param name="Name">名称。</param>
/// <param name="IsActive">本会话是否激活着它（在令牌里）。</param>
public sealed record SessionRoleItemResponse(Guid RoleId, string Code, string Name, bool IsActive);

/// <summary>
/// <c>auth.session-roles</c> 的响应。
/// </summary>
/// <param name="Items">此刻持有的角色，按排序号与编码。</param>
public sealed record SessionRolesResponse(IReadOnlyList<SessionRoleItemResponse> Items);

/// <summary>
/// 列出当前会话可激活的角色。
/// </summary>
/// <remarks>
/// 自助操作，只要求已认证。
/// <c>GET /api/auth:session-roles</c>。此刻持有的角色与本会话激活着哪些（令牌里的），用户菜单的「切换角色」用它，换激活集走 <c>auth.activate-roles</c>。
/// </remarks>
/// <param name="resolver">持有的角色。</param>
/// <param name="roles">角色仓储。</param>
/// <param name="options">安全选项（角色声明名）。</param>
[Operation(Id, Stability = Stability.Preview, ReadOnly = true, Expose = Expose.Http, Policy = SecurityPolicies.Authenticated)]
public sealed class CurrentSessionRoles(IUserRoleResolver resolver, IRepository<SysRole, Guid> roles, SecurityOptions options) : IOperation<SessionRolesRequest, SessionRolesResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "auth.session-roles";

    /// <inheritdoc />
    public async ValueTask<Result<SessionRolesResponse>> HandleAsync(SessionRolesRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var security = context.GetSecurity();
        if (!Guid.TryParse(security.Principal.Id, out var userId))
        {
            return new SessionRolesResponse([]);
        }

        var held = (await resolver.ResolveAsync(userId, security.Tenant?.Id, context.CancellationToken).ConfigureAwait(false))
            .Select(static v => Guid.TryParse(v, out var id) ? id : (Guid?)null).OfType<Guid>().Distinct().ToList();
        var active = security.Principal.ClaimValues(options.RoleClaim).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var items = held.Count == 0
            ? []
            : (await roles.FindManyAsync(held, context.CancellationToken).ConfigureAwait(false))
                .OrderBy(static r => r.Order).ThenBy(static r => r.Code, StringComparer.Ordinal)
                .Select(r => new SessionRoleItemResponse(r.Id, r.Code, r.Name, active.Contains(r.Id.ToString("D"))))
                .ToList();
        return new SessionRolesResponse(items);
    }
}
