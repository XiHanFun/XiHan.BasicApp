// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Access.Domain.Services;
using XiHan.BasicApp.Access.Operations.Roles;
using XiHan.BasicApp.Contracts.Auditing;
using XiHan.BasicApp.Identity.Domain.Entities;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;
using XiHan.Framework.Security.Database.Grants;

namespace XiHan.BasicApp.Access.Operations.UserGrants;

/// <summary>
/// 用户直接授权的共同骨架。
/// </summary>
/// <remarks>
/// 用户须在这一侧（注册地相同），权限码须在目录里且作用侧相符，通过的交给写入委托，逐项结果合并；真正生效的权限码登记一条权限变更日志。
/// </remarks>
internal static class UserGrantBatch
{
    /// <summary>
    /// 找到这一侧的用户。
    /// </summary>
    public static async Task<Result<SysUser>> FindUserAsync(IRepository<SysUser, Guid> users, Guid userId, string owner, CancellationToken cancellationToken)
        => (await users.FindAsync(userId, cancellationToken).ConfigureAwait(false)).TryGetValue(out var user) && string.Equals(user.HomeTenantId, owner, StringComparison.Ordinal)
            ? user
            : Error.NotFound(AccessErrors.MemberNotFound, AccessMessages.MemberNotFound, MessageArguments.Of(("id", userId)));

    /// <summary>
    /// 执行一批。
    /// </summary>
    public static async ValueTask<Result<PermissionBatchResult>> RunAsync(
        UserPermissionsRequest request,
        InvocationContext context,
        IRepository<SysUser, Guid> users,
        PermissionCatalog catalog,
        PermissionChangeType change,
        Func<SysUser, string, IReadOnlyList<string>, CancellationToken, ValueTask<IReadOnlyList<PermissionGrantResult>>> apply,
        Func<SysUser, string, IReadOnlyList<string>, CancellationToken, Task<IReadOnlyList<string>>>? screen = null)
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
                failed.Add(new PermissionFailure(code, AccessErrors.PermissionUnknown));
            }
            else if (!PermissionCatalog.AllowsSide(descriptor, platformSide))
            {
                failed.Add(new PermissionFailure(code, AccessErrors.PermissionScopeMismatch));
            }
            else
            {
                accepted.Add(code);
            }
        }

        if (screen is not null && accepted.Count > 0)
        {
            var blocked = await screen(user, owner, accepted, context.CancellationToken).ConfigureAwait(false);
            failed.AddRange(blocked.Select(static code => new PermissionFailure(code, AccessErrors.ConstraintViolated)));
            accepted.RemoveAll(code => blocked.Contains(code, StringComparer.Ordinal));
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
                    failed.Add(new PermissionFailure(outcome.Grant.Permission, outcome.Outcome.Error!.Code));
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
