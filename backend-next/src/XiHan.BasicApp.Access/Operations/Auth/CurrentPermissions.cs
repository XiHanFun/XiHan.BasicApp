// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Access.Domain.Entities;
using XiHan.BasicApp.Access.Domain.Services;
using XiHan.BasicApp.Access.Infrastructure;
using XiHan.BasicApp.Contracts.Security;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Kernel.Security;
using XiHan.Framework.Persistence.Abstractions.Queries;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;
using XiHan.Framework.Security.Abstractions;

namespace XiHan.BasicApp.Access.Operations.Auth;

/// <summary>
/// 当前主体的权限 <c>auth.permissions</c>（自助操作，只要求已认证）：有效权限码、角色、按权限裁剪的菜单树与页面码。
/// </summary>
/// <param name="effective">有效权限解析。</param>
/// <param name="roles">角色仓储。</param>
/// <param name="menus">菜单仓储。</param>
/// <param name="options">安全选项（角色声明名）。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Policy = Policies.Authenticated)]
public sealed class CurrentPermissions(EffectivePermissions effective, IRepository<SysRole, Guid> roles, IRepository<SysMenu, Guid> menus, SecurityOptions options) : IOperation<PermissionsRequest, PermissionsResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "auth.permissions";

    /// <inheritdoc />
    public async ValueTask<Result<PermissionsResponse>> HandleAsync(PermissionsRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var security = context.GetSecurity();
        var permissions = await effective.ResolveAsync(security, context.CancellationToken).ConfigureAwait(false);

        var roleIds = security.Principal.ClaimValues(options.RoleClaim).Select(static value => Guid.TryParse(value, out var id) ? id : (Guid?)null).OfType<Guid>().Distinct().ToList();
        var summaries = roleIds.Count == 0
            ? []
            : (await roles.FindManyAsync(roleIds, context.CancellationToken).ConfigureAwait(false))
                .OrderBy(static r => r.Order).ThenBy(static r => r.Code, StringComparer.Ordinal)
                .Select(static r => new RoleSummary(r.Id, r.Code, r.Name))
                .ToList();

        var rows = await menus.ListAsync(EntityQuery.All<SysMenu>(), context.CancellationToken).ConfigureAwait(false);
        var tree = MenuTree.Prune(MenuTree.Build(rows), permissions);
        return new PermissionsResponse(permissions.Order(StringComparer.Ordinal).ToList(), summaries, tree, MenuTree.Codes(tree), security.Tenant is null);
    }
}
