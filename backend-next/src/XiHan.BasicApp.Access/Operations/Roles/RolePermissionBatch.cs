// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Access.Domain.Entities;
using XiHan.BasicApp.Access.Domain.Services;
using XiHan.BasicApp.Contracts.Auditing;
using XiHan.Framework.Primitives;
using XiHan.Framework.Security.Database.Grants;

namespace XiHan.BasicApp.Access.Operations.Roles;

/// <summary>
/// 授予 / 撤销的共同骨架：找角色、逐项校验权限码（在目录里、作用侧相符），通过的一次交给框架授予存储，逐项结果合并；真正生效的权限码登记一条权限变更日志。
/// </summary>
internal static class RolePermissionBatch
{
    /// <summary>
    /// 执行一批。
    /// </summary>
    public static async ValueTask<Result<PermissionBatchResult>> RunAsync(
        RolePermissionsRequest request,
        InvocationContext context,
        RoleService service,
        PermissionCatalog catalog,
        PermissionChangeType change,
        Func<SysRole, IReadOnlyList<string>, CancellationToken, ValueTask<IReadOnlyList<PermissionGrantResult>>> apply)
    {
        var owner = RoleService.OwnerOf(context);
        if (!(await service.FindAsync(request.Id, owner, context.CancellationToken).ConfigureAwait(false)).TryGetValue(out var role))
        {
            return Error.NotFound(AccessErrors.RoleNotFound, AccessMessages.RoleNotFound, MessageArguments.Of(("id", request.Id)));
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

        var succeeded = new List<string>(accepted.Count);
        if (accepted.Count > 0)
        {
            foreach (var outcome in await apply(role, accepted, context.CancellationToken).ConfigureAwait(false))
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
            context.AddAuditLog(new PermissionChangeLogEntry(change) { RoleId = role.Id, RoleName = role.Name, PermissionCodes = succeeded });
        }

        return new PermissionBatchResult(succeeded, failed);
    }
}
