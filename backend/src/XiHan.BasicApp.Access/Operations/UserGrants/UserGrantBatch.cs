// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Access.Domain.Services;
using XiHan.BasicApp.Access.Infrastructure;
using XiHan.BasicApp.Access.Operations.Roles;
using XiHan.BasicApp.Contracts.Auditing;
using XiHan.BasicApp.Identity.Domain.Entities;
using XiHan.BasicApp.Identity.Domain.Services;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Access.Operations.UserGrants;

/// <summary>
/// 用户直接授权的共同骨架
/// </summary>
/// <remarks>
/// 用户须在这一侧（注册在这一侧，或这个租户的外部成员），权限码须在目录里且作用侧相符，通过的交给写入委托，逐项结果合并；真正生效的权限码登记一条权限变更日志
/// </remarks>
internal static class UserGrantBatch
{
    /// <summary>
    /// 找到这一侧的用户
    /// </summary>
    public static async Task<Result<SysUser>> FindUserAsync(SideUsers users, Guid userId, string owner, CancellationToken cancellationToken)
        => (await users.FindAsync(userId, owner, cancellationToken).ConfigureAwait(false)).TryGetValue(out var user)
            ? user
            : Error.NotFound(AccessErrors.MemberNotFound, AccessMessages.MemberNotFound, MessageArguments.Of(("id", userId)));

    /// <summary>
    /// 执行一批
    /// </summary>
    public static async ValueTask<Result<PermissionBatchResult>> RunAsync(
        UserPermissionsRequest request,
        InvocationContext context,
        SideUsers users,
        PermissionCatalog catalog,
        PermissionChangeType change,
        Func<SysUser, string, IReadOnlyList<string>, CancellationToken, ValueTask<IReadOnlyList<PermissionOutcome>>> apply,
        Func<SysUser, string, IReadOnlyList<string>, CancellationToken, Task<IReadOnlyList<(string Permission, Error Reason)>>>? screen = null)
    {
        var owner = RoleService.OwnerOf(context);
        var found = await FindUserAsync(users, request.UserId, owner, context.CancellationToken).ConfigureAwait(false);
        if (!found.TryGetValue(out var user))
        {
            return found.Error!;
        }

        var platformSide = RoleService.IsPlatform(owner);
        var accepted = new List<string>();
        var failed = new List<PermissionFailure>();
        foreach (var code in request.Permissions.Select(static p => p.Trim()).Distinct(StringComparer.Ordinal))
        {
            var descriptor = catalog.Find(code);
            if (descriptor is null)
            {
                failed.Add(new PermissionFailure(code, AccessErrors.PermissionUnknown, AccessMessages.PermissionUnknown.L(("code", code))));
            }
            else if (!PermissionCatalog.AllowsSide(descriptor, platformSide))
            {
                failed.Add(new PermissionFailure(code, AccessErrors.PermissionScopeMismatch, AccessMessages.PermissionScopeMismatch.L(("code", code), ("side", platformSide ? "platform" : "tenant"))));
            }
            else
            {
                accepted.Add(code);
            }
        }

        if (screen is not null && accepted.Count > 0)
        {
            var blocked = await screen(user, owner, accepted, context.CancellationToken).ConfigureAwait(false);
            failed.AddRange(blocked.Select(static item => PermissionFailure.From(item.Permission, item.Reason)));
            accepted.RemoveAll(code => blocked.Any(item => string.Equals(item.Permission, code, StringComparison.Ordinal)));
        }

        var succeeded = new List<string>(accepted.Count);
        if (accepted.Count > 0)
        {
            foreach (var outcome in await apply(user, owner, accepted, context.CancellationToken).ConfigureAwait(false))
            {
                if (outcome.Outcome.IsSuccess)
                {
                    succeeded.Add(outcome.Grant.Permission);
                }
                else
                {
                    failed.Add(PermissionFailure.From(outcome.Grant.Permission, outcome.Outcome.Error!));
                }
            }
        }

        if (succeeded.Count > 0)
        {
            context.AddAuditLog(new PermissionChangeLogEntry(change) { TargetUserId = user.Id, TargetUserName = user.UserName, PermissionCodes = succeeded });
        }

        return new PermissionBatchResult(succeeded, failed);
    }
}
