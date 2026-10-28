// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Access.Domain.Entities;
using XiHan.BasicApp.Access.Domain.Services;
using XiHan.BasicApp.Access.Infrastructure;
using XiHan.BasicApp.Contracts.Auditing;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Access.Operations.Roles;

/// <summary>
/// 给角色授予权限：<c>POST /api/roles/{id}:grant</c>。权限码须在目录里且作用侧相符；已有的授予算成功。
/// 带有效期或原因时为这些权限码写 Grant 规则（原来的 Deny 一并改成授予），不带即普通授予（删掉原有的规则行）。
/// </summary>
/// <param name="service">角色域服务。</param>
/// <param name="catalog">权限目录。</param>
/// <param name="grants">角色授予。</param>
/// <param name="rules">授予规则。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = AccessPermissions.RolesGrant)]
public sealed class GrantRole(RoleService service, PermissionCatalog catalog, RoleGrants grants, PermissionRuleWriter rules) : IOperation<RolePermissionsRequest, PermissionBatchResult>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "roles.grant";

    /// <inheritdoc />
    public async ValueTask<Result<PermissionBatchResult>> HandleAsync(RolePermissionsRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var window = GrantWindow.Validate(request.EffectiveAt, request.ExpiresAt);
        if (window.IsFailure)
        {
            return window.Error!;
        }

        var result = await RolePermissionBatch.RunAsync(request, context, service, catalog, PermissionChangeType.RoleGrant, grants.GrantAsync).ConfigureAwait(false);
        if (!result.TryGetValue(out var batch))
        {
            return result;
        }

        var written = await rules.SetAsync(RoleService.OwnerOf(context), GrantSubjectKind.Role, request.Id, batch.Succeeded, PermissionAction.Grant, request.EffectiveAt, request.ExpiresAt, request.Reason, context.CancellationToken).ConfigureAwait(false);
        return written.IsFailure ? written.Error! : batch;
    }
}
